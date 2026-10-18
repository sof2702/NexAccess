using NexAccess.Application.DTOs.Common;
using NexAccess.Application.DTOs.Credentials;
using NexAccess.Application.Exceptions;
using NexAccess.Domain.Entities;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class CredentialServiceTests
{
    private readonly ServiceFactory _factory = new();
    private readonly Employee _employee = TestData.Employee();

    public CredentialServiceTests()
    {
        _factory.Employees.GetByIdAsync(_employee.Id, Arg.Any<CancellationToken>()).Returns(_employee);
    }

    [Fact]
    public async Task Register_stores_normalized_code_as_codigo_credencial()
    {
        var response = await _factory.CredentialService().RegisterAsync(new CredentialRegisterRequest { EmployeeId = _employee.Id, CredentialCode = "04-a1-b2-c3" });

        Assert.Equal("04A1B2C3", response.CredentialCode);
        Assert.Equal("ACTIVA", response.Status);
        _factory.AuditRepository.Received(1).Add(Arg.Is<AuditEntry>(a => a.Action == "CREATE" && !a.Detail!.Contains("04A1B2C3")));
    }

    [Fact]
    public async Task Register_rejects_duplicate_code()
    {
        _factory.Credentials.CodeExistsAsync("04A1B2C3", Arg.Any<CancellationToken>()).Returns(true);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            _factory.CredentialService().RegisterAsync(new CredentialRegisterRequest { EmployeeId = _employee.Id, CredentialCode = "04A1B2C3" }));

        Assert.Equal("CREDENTIAL_ALREADY_REGISTERED", error.Code);
    }

    [Fact]
    public async Task Register_rejects_past_expiration()
    {
        await Assert.ThrowsAsync<AppValidationException>(() =>
            _factory.CredentialService().RegisterAsync(new CredentialRegisterRequest
            {
                EmployeeId = _employee.Id,
                CredentialCode = "04A1B2C3",
                ExpiresAt = TestData.MondayNoonUtc.AddDays(-1)
            }));
    }

    [Fact]
    public async Task Register_for_inactive_employee_is_rejected()
    {
        _employee.IsActive = false;

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _factory.CredentialService().RegisterAsync(new CredentialRegisterRequest { EmployeeId = _employee.Id, CredentialCode = "04A1B2C3" }));
    }

    [Fact]
    public async Task Revoke_deactivates_and_records_reason()
    {
        var credential = TestData.Credential(_employee);
        _factory.Credentials.GetByIdAsync(credential.Id, Arg.Any<CancellationToken>()).Returns(credential);

        var response = await _factory.CredentialService().RevokeAsync(credential.Id, new RevokeCredentialRequest { Reason = "Extravío" });

        Assert.Equal("REVOCADA", response.Status);
        Assert.False(credential.IsActive);
        Assert.Equal("Extravío", credential.RevocationReason);
        Assert.Equal(TestData.MondayNoonUtc, credential.RevokedAt);
    }

    [Fact]
    public async Task Revoked_credential_cannot_be_reactivated()
    {
        var credential = TestData.Credential(_employee);
        credential.Revoke("Extravío", TestData.MondayNoonUtc);
        _factory.Credentials.GetByIdAsync(credential.Id, Arg.Any<CancellationToken>()).Returns(credential);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _factory.CredentialService().SetStatusAsync(credential.Id, new StatusRequest { IsActive = true }));

        Assert.Equal("CREDENTIAL_REVOKED", error.Code);
        Assert.False(credential.IsActive);
    }

    [Fact]
    public async Task Revoking_twice_is_conflict()
    {
        var credential = TestData.Credential(_employee);
        credential.Revoke("Extravío", TestData.MondayNoonUtc);
        _factory.Credentials.GetByIdAsync(credential.Id, Arg.Any<CancellationToken>()).Returns(credential);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _factory.CredentialService().RevokeAsync(credential.Id, new RevokeCredentialRequest { Reason = "Otra vez" }));
    }
}
