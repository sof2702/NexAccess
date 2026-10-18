using System.Net;
using System.Text.Json.Nodes;
using NexAccess.IntegrationTests.Support;

namespace NexAccess.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AccessFlowTests(NexAccessApiFactory factory)
{
    [Fact]
    public async Task Enrollment_flow_from_command_to_authorized_access()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var device = scenario.DeviceClient();

        var none = await device.GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("""{"command":"NONE"}""", none.ToJsonString());

        var enrollment = await scenario.Admin.PostAsync("/api/v1/enrollments", new { employeeId = scenario.EmployeeId, deviceId = scenario.DeviceId }).ExpectAsync(HttpStatusCode.Created);
        var enrollmentId = enrollment["id"]!.GetValue<long>();

        var command = await device.GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("ENROLL_CARD", command["command"]!.GetValue<string>());
        Assert.Equal(enrollmentId, command["enrollmentId"]!.GetValue<long>());

        var card = ApiClient.Unique("04A1");
        var submitted = await device.PostAsync($"/api/v1/enrollments/{enrollmentId}/card", new JsonObject { ["codigo_credencial"] = card }).ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("COMPLETADO", submitted["status"]!.GetValue<string>());

        var after = await device.GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("NONE", after["command"]!.GetValue<string>());

        var verify = await scenario.VerifyAsync(card);
        Assert.True(verify["authorized"]!.GetValue<bool>());
        Assert.Equal("OPEN", verify["action"]!.GetValue<string>());
        Assert.Equal(3000, verify["durationMs"]!.GetValue<int>());
        Assert.Equal("AUTHORIZED", verify["reason"]!.GetValue<string>());

        var stored = await DatabaseScripts.ScalarAsync<string>(factory.ConnectionString,
            "SELECT codigo_credencial FROM credencial WHERE id = @id", ("id", submitted["credentialId"]!.GetValue<long>()));
        Assert.Equal(card, stored);
    }

    [Fact]
    public async Task Unknown_credential_is_denied_with_stable_contract_and_event_is_persisted()
    {
        var scenario = await Scenario.CreateAsync(factory);

        var verify = await scenario.VerifyAsync("FFFF0000AAAA");

        Assert.Equal("""{"authorized":false,"action":"DENY","reason":"CREDENTIAL_NOT_FOUND"}""", verify.ToJsonString());
        var events = await DatabaseScripts.ScalarAsync<long>(factory.ConnectionString,
            "SELECT COUNT(*) FROM evento_acceso WHERE id_dispositivo = @id AND resultado = 'RECHAZADO' AND motivo = 'CREDENTIAL_NOT_FOUND'", ("id", scenario.DeviceId));
        Assert.Equal(1, events);
    }

    [Fact]
    public async Task Revoked_inactive_and_employee_inactive_credentials_are_denied()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var card = await scenario.EnrollCardAsync();
        var credentials = await scenario.Admin.GetAsync($"/api/v1/credentials?employeeId={scenario.EmployeeId}").ExpectAsync(HttpStatusCode.OK);
        var credentialId = credentials["items"]![0]!["id"]!.GetValue<long>();

        await scenario.Admin.PatchAsync($"/api/v1/credentials/{credentialId}/status", new { isActive = false }).ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("CREDENTIAL_INACTIVE", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());

        await scenario.Admin.PatchAsync($"/api/v1/credentials/{credentialId}/status", new { isActive = true }).ExpectAsync(HttpStatusCode.OK);
        await scenario.Admin.PatchAsync($"/api/v1/employees/{scenario.EmployeeId}/status", new { isActive = false }).ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("EMPLOYEE_INACTIVE", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());

        await scenario.Admin.PatchAsync($"/api/v1/employees/{scenario.EmployeeId}/status", new { isActive = true }).ExpectAsync(HttpStatusCode.OK);
        await scenario.Admin.PostAsync($"/api/v1/credentials/{credentialId}/revoke", new { reason = "Extravío reportado" }).ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("CREDENTIAL_REVOKED", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());

        await scenario.Admin.PatchAsync($"/api/v1/credentials/{credentialId}/status", new { isActive = true }).ExpectAsync(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Expired_credential_is_denied()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var card = ApiClient.Unique("0E");
        await scenario.Admin.PostAsync("/api/v1/credentials", new JsonObject
        {
            ["employeeId"] = scenario.EmployeeId,
            ["codigo_credencial"] = card,
            ["expiresAt"] = DateTime.UtcNow.AddMinutes(5)
        }).ExpectAsync(HttpStatusCode.Created);

        await DatabaseScripts.ScalarAsync<long>(factory.ConnectionString,
            "UPDATE credencial SET fecha_expiracion = NOW() - INTERVAL '1 minute' WHERE codigo_credencial = @code RETURNING id", ("code", card));

        Assert.Equal("CREDENTIAL_EXPIRED", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());
    }

    [Fact]
    public async Task Employee_without_role_permission_is_denied()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var card = await scenario.EnrollCardAsync();
        await scenario.Admin.DeleteAsync($"/api/v1/employees/{scenario.EmployeeId}/roles/{scenario.RoleId}").ExpectAsync(HttpStatusCode.OK);

        Assert.Equal("NO_PERMISSION", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());
    }

    [Fact]
    public async Task Access_outside_schedule_is_denied()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Asuncion"));
        var start = TimeOnly.FromDateTime(now).AddHours(2);
        var end = start.AddHours(1);
        var scenario = await Scenario.CreateAsync(factory, startTime: start.ToString("HH:mm:ss"), endTime: end.ToString("HH:mm:ss"));
        var card = await scenario.EnrollCardAsync();

        Assert.Equal("OUTSIDE_SCHEDULE", (await scenario.VerifyAsync(card))["reason"]!.GetValue<string>());
    }

    [Fact]
    public async Task Same_card_cannot_be_enrolled_twice()
    {
        var first = await Scenario.CreateAsync(factory);
        var card = await first.EnrollCardAsync();
        var second = await Scenario.CreateAsync(factory);
        var enrollment = await second.Admin.PostAsync("/api/v1/enrollments", new { employeeId = second.EmployeeId, deviceId = second.DeviceId }).ExpectAsync(HttpStatusCode.Created);

        var problem = await second.DeviceClient()
            .PostAsync($"/api/v1/enrollments/{enrollment["id"]}/card", new JsonObject { ["codigo_credencial"] = card.ToLowerInvariant() })
            .ExpectAsync(HttpStatusCode.Conflict);

        Assert.Equal("CREDENTIAL_ALREADY_REGISTERED", problem["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Expired_enrollment_is_closed_and_rejects_card()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var enrollment = await scenario.Admin.PostAsync("/api/v1/enrollments", new { employeeId = scenario.EmployeeId, deviceId = scenario.DeviceId }).ExpectAsync(HttpStatusCode.Created);
        var id = enrollment["id"]!.GetValue<long>();
        await DatabaseScripts.ScalarAsync<long>(factory.ConnectionString,
            "UPDATE comando_dispositivo SET fecha_expiracion = NOW() - INTERVAL '1 minute' WHERE id_enrolamiento = @id; UPDATE enrolamiento SET fecha_expiracion = NOW() - INTERVAL '1 minute' WHERE id = @id RETURNING id", ("id", id));

        var command = await scenario.DeviceClient().GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("NONE", command["command"]!.GetValue<string>());

        var problem = await scenario.DeviceClient().PostAsync($"/api/v1/enrollments/{id}/card", new JsonObject { ["codigo_credencial"] = "04A1B2C3D4E5" }).ExpectAsync(HttpStatusCode.Conflict);
        Assert.Equal("ENROLLMENT_EXPIRED", problem["code"]!.GetValue<string>());

        var status = await scenario.Admin.GetAsync($"/api/v1/enrollments/{id}").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("VENCIDO", status["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Mixed_device_registers_attendance_without_duplicates()
    {
        var scenario = await Scenario.CreateAsync(factory, deviceType: "MIXTO");
        var card = await scenario.EnrollCardAsync();

        var first = await scenario.VerifyAsync(card);
        var repeated = await scenario.VerifyAsync(card);

        Assert.Equal("ENTRADA", first["attendance"]!["type"]!.GetValue<string>());
        Assert.False(first["attendance"]!["duplicate"]!.GetValue<bool>());
        Assert.True(repeated["attendance"]!["duplicate"]!.GetValue<bool>());

        var marks = await scenario.Admin.GetAsync($"/api/v1/attendance?employeeId={scenario.EmployeeId}").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal(1, marks["totalItems"]!.GetValue<int>());
        Assert.Equal("Ana María Pérez Ñandutí", marks["items"]![0]!["employeeName"]!.GetValue<string>());

        var events = await scenario.Admin.GetAsync($"/api/v1/devices/{scenario.DeviceId}/events").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal(2, events["totalItems"]!.GetValue<int>());
    }

    [Fact]
    public async Task Manual_correction_report_and_csv_export()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var occurredAt = DateTime.UtcNow.AddHours(-2);
        var mark = await scenario.Admin.PostAsync("/api/v1/attendance", new { employeeId = scenario.EmployeeId, type = "ENTRADA", occurredAt, note = "Olvidó la tarjeta" })
            .ExpectAsync(HttpStatusCode.Created);
        Assert.Equal("MANUAL", mark["source"]!.GetValue<string>());

        await scenario.Admin.PostAsync("/api/v1/attendance", new { employeeId = scenario.EmployeeId, type = "SALIDA", occurredAt = occurredAt.AddSeconds(10), note = "Duplicada" })
            .ExpectAsync(HttpStatusCode.Conflict);

        var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(DateTime.UtcNow.AddDays(1).ToString("O"));
        var report = await scenario.Admin.GetAsync($"/api/v1/attendance/report?employeeId={scenario.EmployeeId}&from={from}&to={to}").ExpectAsync(HttpStatusCode.OK);
        Assert.Single(report.AsArray());
        Assert.True(report[0]!["incomplete"]!.GetValue<bool>());

        var csv = await scenario.Admin.GetAsync($"/api/v1/attendance/export?employeeId={scenario.EmployeeId}&from={from}&to={to}");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var text = await csv.Content.ReadAsStringAsync();
        Assert.Contains("Ana María Pérez Ñandutí", text);
        Assert.Contains("Olvidó la tarjeta", text);

        await scenario.Admin.GetAsync("/api/v1/attendance/export").ExpectAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_crud_validations_and_duplicates()
    {
        var admin = await ApiClient.AdminAsync(factory);
        var document = ApiClient.Unique("D");
        var created = await admin.PostAsync("/api/v1/employees", new { documentNumber = document, firstName = "José", lastName = "Güemes", email = $"{document.ToLowerInvariant()}@example.com" })
            .ExpectAsync(HttpStatusCode.Created);
        var id = created["id"]!.GetValue<long>();
        Assert.Equal("Güemes", created["lastName"]!.GetValue<string>());

        await admin.PostAsync("/api/v1/employees", new { documentNumber = document, firstName = "Otro", lastName = "Duplicado" }).ExpectAsync(HttpStatusCode.Conflict);
        var invalid = await admin.PostAsync("/api/v1/employees", new { documentNumber = "", firstName = "" }).ExpectAsync(HttpStatusCode.BadRequest);
        Assert.Contains("obligatorio", invalid["errors"]!.ToJsonString());

        var updated = await admin.PutAsync($"/api/v1/employees/{id}", new { documentNumber = document, firstName = "José Luis", lastName = "Güemes" }).ExpectAsync(HttpStatusCode.OK);
        Assert.Equal("José Luis", updated["firstName"]!.GetValue<string>());

        await admin.GetAsync("/api/v1/employees/999999999").ExpectAsync(HttpStatusCode.NotFound);
        var page = await admin.GetAsync($"/api/v1/employees?search={document}&pageSize=5").ExpectAsync(HttpStatusCode.OK);
        Assert.Equal(1, page["totalItems"]!.GetValue<int>());
        await admin.GetAsync("/api/v1/employees?pageSize=500").ExpectAsync(HttpStatusCode.BadRequest);

        var audit = await admin.GetAsync($"/api/v1/audit?entity=empleado&pageSize=50").ExpectAsync(HttpStatusCode.OK);
        Assert.Contains(audit["items"]!.AsArray(), item => item!["entityId"]?.GetValue<long>() == id && item["action"]!.GetValue<string>() == "UPDATE");
    }

    [Fact]
    public async Task Rotated_key_invalidates_previous_key()
    {
        var scenario = await Scenario.CreateAsync(factory);
        var rotated = await scenario.Admin.PostAsync($"/api/v1/devices/{scenario.DeviceId}/rotate-key", null).ExpectAsync(HttpStatusCode.OK);
        var newKey = rotated["deviceKey"]!.GetValue<string>();

        await scenario.DeviceClient().GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.Unauthorized);
        await ApiClient.Device(factory, newKey).GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.OK);

        var keyHash = await DatabaseScripts.ScalarAsync<string>(factory.ConnectionString, "SELECT clave_hash FROM dispositivo WHERE id = @id", ("id", scenario.DeviceId));
        Assert.NotEqual(newKey, keyHash);
        Assert.Equal(64, keyHash!.Length);
    }
}
