using NexAccess.Application.Exceptions;
using NexAccess.Application.Services;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;
using NexAccess.UnitTests.Support;
using NSubstitute;

namespace NexAccess.UnitTests.Services;

public class MyProfileServiceTests
{
    private readonly ServiceFactory _factory = new();

    private MyProfileService CreateService() => new(
    _factory.CurrentUser,
    _factory.Users,
    _factory.Employees,
    _factory.Credentials,
    _factory.Clock,
    null!,
    _factory.MobileSessions);

    [Fact]
    public async Task Cannot_download_digital_credential_from_another_installation()
    {
        var employee = TestData.Employee();
        var installationA = Guid.NewGuid();
        var installationB = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _factory.CurrentUser.UserId = 1;
        _factory.CurrentUser.SessionId = sessionId;

        _factory.Users
            .GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = 1,
                EmployeeId = employee.Id,
                IsActive = true,
                MobileAccessEnabled = true
            });

        _factory.Employees
            .GetByIdAsync(employee.Id, Arg.Any<CancellationToken>())
            .Returns(employee);

        _factory.MobileSessions
            .GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new MobileSession
            {
                Id = sessionId,
                UserId = 1,
                InstallationId = installationA,
                IsActive = true
            });

        var credential = TestData.Credential(employee);
        credential.Type = CredentialType.Digital;
        credential.InstallationId = installationB;

        _factory.Credentials
            .GetByIdAsync(credential.Id, Arg.Any<CancellationToken>())
            .Returns(credential);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().GetNfcCredentialAsync(credential.Id));
    }

[Fact]
public async Task Can_download_digital_credential_from_own_installation()
{
    var employee = TestData.Employee();
    var installationId = Guid.NewGuid();
    var sessionId = Guid.NewGuid();

    _factory.CurrentUser.UserId = 1;
    _factory.CurrentUser.SessionId = sessionId;

    _factory.Users
        .GetByIdAsync(1, Arg.Any<CancellationToken>())
        .Returns(new User
        {
            Id = 1,
            EmployeeId = employee.Id,
            IsActive = true,
            MobileAccessEnabled = true
        });

    _factory.Employees
        .GetByIdAsync(employee.Id, Arg.Any<CancellationToken>())
        .Returns(employee);

    _factory.MobileSessions
        .GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
        .Returns(new MobileSession
        {
            Id = sessionId,
            UserId = 1,
            InstallationId = installationId,
            IsActive = true
        });

    var credential = TestData.Credential(employee);
    credential.Type = CredentialType.Digital;
    credential.InstallationId = installationId;

    _factory.Credentials
        .GetByIdAsync(credential.Id, Arg.Any<CancellationToken>())
        .Returns(credential);

    var response = await CreateService()
        .GetNfcCredentialAsync(credential.Id);

    Assert.Equal(credential.Id, response.CredentialId);
    Assert.Equal(credential.CredentialCode, response.Credential);
}

[Fact]
public async Task Cannot_download_digital_credential_with_revoked_session()
{
    var employee = TestData.Employee();
    var installationId = Guid.NewGuid();
    var sessionId = Guid.NewGuid();

    _factory.CurrentUser.UserId = 1;
    _factory.CurrentUser.SessionId = sessionId;

    _factory.Users
        .GetByIdAsync(1, Arg.Any<CancellationToken>())
        .Returns(new User
        {
            Id = 1,
            EmployeeId = employee.Id,
            IsActive = true,
            MobileAccessEnabled = true
        });

    _factory.Employees
        .GetByIdAsync(employee.Id, Arg.Any<CancellationToken>())
        .Returns(employee);

    _factory.MobileSessions
        .GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
        .Returns(new MobileSession
        {
            Id = sessionId,
            UserId = 1,
            InstallationId = installationId,
            IsActive = false,
            RevokedAt = TestData.MondayNoonUtc
        });

    var credential = TestData.Credential(employee);
    credential.Type = CredentialType.Digital;
    credential.InstallationId = installationId;

    _factory.Credentials
        .GetByIdAsync(credential.Id, Arg.Any<CancellationToken>())
        .Returns(credential);

    await Assert.ThrowsAsync<NotFoundException>(() =>
        CreateService().GetNfcCredentialAsync(credential.Id));
}



}
