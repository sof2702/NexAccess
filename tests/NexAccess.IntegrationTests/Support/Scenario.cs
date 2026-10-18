using System.Net;
using System.Text.Json.Nodes;

namespace NexAccess.IntegrationTests.Support;

public sealed class Scenario
{
    private Scenario(HttpClient admin, NexAccessApiFactory factory)
    {
        Admin = admin;
        Factory = factory;
    }

    public NexAccessApiFactory Factory { get; }
    public HttpClient Admin { get; }
    public long AreaId { get; private set; }
    public long RoleId { get; private set; }
    public long EmployeeId { get; private set; }
    public long DeviceId { get; private set; }
    public string DeviceCode { get; private set; } = string.Empty;
    public string DeviceKey { get; private set; } = string.Empty;

    public static async Task<Scenario> CreateAsync(
        NexAccessApiFactory factory,
        string deviceType = "ACCESO",
        string? startTime = null,
        string? endTime = null)
    {
        var scenario = new Scenario(await ApiClient.AdminAsync(factory), factory);
        var admin = scenario.Admin;

        scenario.AreaId = (await admin.PostAsync("/api/v1/areas", new { code = ApiClient.Unique("AREA-"), name = "Área de prueba", isRestricted = true })
            .ExpectAsync(HttpStatusCode.Created))["id"]!.GetValue<long>();

        scenario.RoleId = (await admin.PostAsync("/api/v1/roles", new { code = ApiClient.Unique("ROL_").Replace("-", "_"), name = "Rol de prueba" })
            .ExpectAsync(HttpStatusCode.Created))["id"]!.GetValue<long>();

        await admin.PostAsync("/api/v1/access-permissions", new
        {
            roleId = scenario.RoleId,
            areaId = scenario.AreaId,
            startTime,
            endTime,
            monday = true,
            tuesday = true,
            wednesday = true,
            thursday = true,
            friday = true,
            saturday = true,
            sunday = true
        }).ExpectAsync(HttpStatusCode.Created);

        scenario.EmployeeId = (await admin.PostAsync("/api/v1/employees", new { documentNumber = ApiClient.Unique("C"), firstName = "Ana María", lastName = "Pérez Ñandutí" })
            .ExpectAsync(HttpStatusCode.Created))["id"]!.GetValue<long>();

        await admin.PostAsync($"/api/v1/employees/{scenario.EmployeeId}/roles/{scenario.RoleId}", null).ExpectAsync(HttpStatusCode.OK);

        var device = await admin.PostAsync("/api/v1/devices", new { code = ApiClient.Unique("PUERTA-"), name = "Puerta de prueba", type = deviceType, areaId = scenario.AreaId })
            .ExpectAsync(HttpStatusCode.Created);
        scenario.DeviceId = device["device"]!["id"]!.GetValue<long>();
        scenario.DeviceCode = device["device"]!["code"]!.GetValue<string>();
        scenario.DeviceKey = device["deviceKey"]!.GetValue<string>();
        return scenario;
    }

    public HttpClient DeviceClient() => ApiClient.Device(Factory, DeviceKey);

    public async Task<string> EnrollCardAsync()
    {
        var card = ApiClient.Unique("04A1");
        var enrollment = await Admin.PostAsync("/api/v1/enrollments", new { employeeId = EmployeeId, deviceId = DeviceId }).ExpectAsync(HttpStatusCode.Created);
        var id = enrollment["id"]!.GetValue<long>();
        await DeviceClient().PostAsync($"/api/v1/enrollments/{id}/card", new JsonObject { ["codigo_credencial"] = card }).ExpectAsync(HttpStatusCode.OK);
        return card;
    }

    public Task<JsonNode> VerifyAsync(string credential) =>
        DeviceClient().PostAsync("/api/v1/access/verify", new { deviceCode = DeviceCode, credential }).ExpectAsync(HttpStatusCode.OK);
}
