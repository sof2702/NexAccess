using System.Net;
using System.Net.Http.Headers;
using NexAccess.IntegrationTests.Support;

namespace NexAccess.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthenticationTests(NexAccessApiFactory factory)
{
    [Fact]
    public async Task Login_with_valid_credentials_returns_jwt_and_me_works()
    {
        var client = factory.CreateClient();
        var token = await ApiClient.LoginAsync(client, factory.AdminUsername, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetAsync("/api/v1/auth/me").ExpectAsync(HttpStatusCode.OK);

        Assert.Equal(factory.AdminUsername, me["username"]!.GetValue<string>());
        Assert.Equal("ADMIN", me["role"]!.GetValue<string>());
        Assert.Null(me["passwordHash"]);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var problem = await factory.CreateClient()
            .PostAsync("/api/v1/auth/login", new { username = factory.AdminUsername, password = "incorrecta-123" })
            .ExpectAsync(HttpStatusCode.Unauthorized);

        Assert.Equal("INVALID_CREDENTIALS", problem["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invalid_login_payload_is_bad_request()
    {
        var problem = await factory.CreateClient().PostAsync("/api/v1/auth/login", new { username = "" }).ExpectAsync(HttpStatusCode.BadRequest);

        Assert.Equal("VALIDATION_ERROR", problem["code"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("/api/v1/employees")]
    [InlineData("/api/v1/devices")]
    [InlineData("/api/v1/attendance")]
    [InlineData("/api/v1/access-events")]
    [InlineData("/api/v1/audit")]
    public async Task Protected_endpoints_require_token(string url)
    {
        await factory.CreateClient().GetAsync(url).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tampered_token_is_rejected()
    {
        var client = factory.CreateClient();
        var token = await ApiClient.LoginAsync(client, factory.AdminUsername, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        await client.GetAsync("/api/v1/auth/me").ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Rrhh_role_is_limited_to_its_endpoints()
    {
        var admin = await ApiClient.AdminAsync(factory);
        var username = ApiClient.Unique("rrhh").ToLowerInvariant();
        const string password = "Rrhh-Prueba-12345";
        await admin.PostAsync("/api/v1/users", new { username, password, role = "RRHH" }).ExpectAsync(HttpStatusCode.Created);

        var rrhh = factory.CreateClient();
        rrhh.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiClient.LoginAsync(rrhh, username, password));

        await rrhh.GetAsync("/api/v1/attendance").ExpectAsync(HttpStatusCode.OK);
        await rrhh.GetAsync("/api/v1/employees").ExpectAsync(HttpStatusCode.OK);
        await rrhh.GetAsync("/api/v1/access-events").ExpectAsync(HttpStatusCode.Forbidden);
        await rrhh.GetAsync("/api/v1/users").ExpectAsync(HttpStatusCode.Forbidden);
        await rrhh.PostAsync("/api/v1/devices", new { code = "X1", name = "x", type = "MARCACION" }).ExpectAsync(HttpStatusCode.Forbidden);
        await rrhh.PostAsync("/api/v1/access-permissions", new { roleId = 1, areaId = 1 }).ExpectAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Token_of_deactivated_user_stops_working_immediately()
    {
        var admin = await ApiClient.AdminAsync(factory);
        var username = ApiClient.Unique("seg").ToLowerInvariant();
        const string password = "Seguridad-Prueba-123";
        var user = await admin.PostAsync("/api/v1/users", new { username, password, role = "SEGURIDAD" }).ExpectAsync(HttpStatusCode.Created);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ApiClient.LoginAsync(client, username, password));
        await client.GetAsync("/api/v1/devices").ExpectAsync(HttpStatusCode.OK);

        await admin.PatchAsync($"/api/v1/users/{user["id"]}/status", new { isActive = false }).ExpectAsync(HttpStatusCode.OK);

        await client.GetAsync("/api/v1/devices").ExpectAsync(HttpStatusCode.Unauthorized);
        await client.PostAsync("/api/v1/auth/login", new { username, password }).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Device_key_cannot_use_admin_endpoints_and_jwt_cannot_use_device_endpoints()
    {
        var scenario = await Scenario.CreateAsync(factory);

        await scenario.DeviceClient().GetAsync("/api/v1/employees").ExpectAsync(HttpStatusCode.Unauthorized);
        await scenario.DeviceClient().PostAsync("/api/v1/employees", new { documentNumber = "999", firstName = "x", lastName = "y" }).ExpectAsync(HttpStatusCode.Unauthorized);
        await scenario.Admin.PostAsync("/api/v1/access/verify", new { deviceCode = scenario.DeviceCode, credential = "04A1B2C3" }).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_or_missing_device_key_is_unauthorized()
    {
        var scenario = await Scenario.CreateAsync(factory);

        var problem = await ApiClient.Device(factory, "nxd_invalida").GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("DEVICE_UNAUTHORIZED", problem["code"]!.GetValue<string>());
        await factory.CreateClient().GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivated_device_is_rejected()
    {
        var scenario = await Scenario.CreateAsync(factory);
        await scenario.Admin.PatchAsync($"/api/v1/devices/{scenario.DeviceId}/status", new { isActive = false }).ExpectAsync(HttpStatusCode.OK);

        await scenario.DeviceClient().GetAsync($"/api/v1/devices/{scenario.DeviceCode}/commands").ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Device_cannot_poll_commands_of_another_device()
    {
        var first = await Scenario.CreateAsync(factory);
        var second = await Scenario.CreateAsync(factory);

        await first.DeviceClient().GetAsync($"/api/v1/devices/{second.DeviceCode}/commands").ExpectAsync(HttpStatusCode.Forbidden);
    }
}
