using Microsoft.Extensions.Options;
using NexAccess.Application.DTOs.Devices;
using NexAccess.Application.DTOs.Enrollments;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using NexAccess.Application.Interfaces.Repositories;
using NexAccess.Application.Options;
using NexAccess.Application.Validators;
using NexAccess.Domain.Entities;
using NexAccess.Domain.Enums;

namespace NexAccess.Application.Services;

public class EnrollmentService
{
    private readonly IEnrollmentRepository _enrollments;
    private readonly IEmployeeRepository _employees;
    private readonly IDeviceRepository _devices;
    private readonly IDeviceCommandRepository _commands;
    private readonly CredentialService _credentials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuditService _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly EnrollmentOptions _options;

    public EnrollmentService(
        IEnrollmentRepository enrollments,
        IEmployeeRepository employees,
        IDeviceRepository devices,
        IDeviceCommandRepository commands,
        CredentialService credentials,
        IUnitOfWork unitOfWork,
        AuditService audit,
        ICurrentUser currentUser,
        IClock clock,
        IOptions<EnrollmentOptions> options)
    {
        _enrollments = enrollments;
        _employees = employees;
        _devices = devices;
        _commands = commands;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<EnrollmentResponse> CreateAsync(EnrollmentCreateRequest request, CancellationToken cancellationToken = default)
    {
        ModelValidator.Validate(request);
        var minutes = request.ExpiresInMinutes ?? _options.DefaultExpirationMinutes;
        if (minutes > _options.MaxExpirationMinutes)
        {
            throw new AppValidationException("expiresInMinutes", $"La vigencia máxima es de {_options.MaxExpirationMinutes} minutos.");
        }

        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken) ?? throw new NotFoundException("Empleado", request.EmployeeId);
        if (!employee.IsActive)
        {
            throw new BusinessRuleException("EMPLOYEE_INACTIVE", "No se puede enrolar una credencial para un empleado inactivo.");
        }

        var device = await _devices.GetByIdAsync(request.DeviceId, cancellationToken) ?? throw new NotFoundException("Dispositivo", request.DeviceId);
        if (!device.IsActive)
        {
            throw new BusinessRuleException("DEVICE_INACTIVE", "El dispositivo está inactivo.");
        }

        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(minutes);
        var enrollment = new Enrollment
        {
            EmployeeId = employee.Id,
            Employee = employee,
            DeviceId = device.Id,
            Device = device,
            CredentialType = request.CredentialType,
            CredentialName = string.IsNullOrWhiteSpace(request.CredentialName) ? null : request.CredentialName.Trim(),
            Status = EnrollmentStatus.Pendiente,
            ExpiresAt = expiresAt,
            RequestedByUserId = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _enrollments.Add(enrollment);
        _commands.Add(new DeviceCommand
        {
            DeviceId = device.Id,
            Type = DeviceCommandType.EnrollCard,
            Enrollment = enrollment,
            Status = DeviceCommandStatus.Pendiente,
            ExpiresAt = expiresAt,
            CreatedAt = now
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("CREATE", "enrolamiento", enrollment.Id, $"empleado={employee.Id};dispositivo={device.Code}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EnrollmentResponse.From(enrollment);
    }

    public async Task<EnrollmentResponse> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var enrollment = await FindAsync(id, cancellationToken);
        if (enrollment.Status == EnrollmentStatus.Pendiente && enrollment.IsExpiredAt(_clock.UtcNow))
        {
            await ExpireAsync(enrollment, cancellationToken);
        }

        return EnrollmentResponse.From(enrollment);
    }

    public async Task<EnrollmentResponse> CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var enrollment = await FindAsync(id, cancellationToken);
        if (enrollment.Status != EnrollmentStatus.Pendiente)
        {
            throw new ConflictException("ENROLLMENT_NOT_PENDING", "Solo se pueden cancelar enrolamientos pendientes.");
        }

        var now = _clock.UtcNow;
        enrollment.Status = EnrollmentStatus.Cancelado;
        enrollment.UpdatedAt = now;
        foreach (var command in await _commands.GetOpenForEnrollmentAsync(enrollment.Id, cancellationToken))
        {
            command.Status = DeviceCommandStatus.Cancelado;
        }

        _audit.Record("CANCEL", "enrolamiento", enrollment.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return EnrollmentResponse.From(enrollment);
    }

    public async Task<EnrollmentCardResponse> SubmitCardAsync(
        AuthenticatedDevice device,
        long enrollmentId,
        EnrollmentCardRequest request,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await FindAsync(enrollmentId, cancellationToken);
        if (enrollment.DeviceId != device.Id)
        {
            throw new ForbiddenException("ENROLLMENT_DEVICE_MISMATCH", "El enrolamiento no fue asignado a este dispositivo.");
        }

        switch (enrollment.Status)
        {
            case EnrollmentStatus.Completado:
                throw new ConflictException("ENROLLMENT_ALREADY_COMPLETED", "El enrolamiento ya fue completado.");
            case EnrollmentStatus.Cancelado:
                throw new ConflictException("ENROLLMENT_CANCELLED", "El enrolamiento fue cancelado.");
            case EnrollmentStatus.Vencido:
                throw new ConflictException("ENROLLMENT_EXPIRED", "El enrolamiento está vencido.");
        }

        var now = _clock.UtcNow;
        if (enrollment.IsExpiredAt(now))
        {
            await ExpireAsync(enrollment, cancellationToken);
            throw new ConflictException("ENROLLMENT_EXPIRED", "El enrolamiento está vencido.");
        }

        ModelValidator.Validate(request);
        var employee = enrollment.Employee
            ?? await _employees.GetByIdAsync(enrollment.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Empleado", enrollment.EmployeeId);

        var credential = await _credentials.AddNewAsync(employee, request.CredentialCode, enrollment.CredentialType, enrollment.CredentialName, null, cancellationToken);
        enrollment.Credential = credential;
        enrollment.Status = EnrollmentStatus.Completado;
        enrollment.CompletedAt = now;
        enrollment.UpdatedAt = now;
        foreach (var command in await _commands.GetOpenForEnrollmentAsync(enrollment.Id, cancellationToken))
        {
            command.Status = DeviceCommandStatus.Completado;
            command.CompletedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Record("COMPLETE", "enrolamiento", enrollment.Id, $"credencial={credential.Id};codigo={CredentialCode.Mask(credential.CredentialCode)}");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new EnrollmentCardResponse(enrollment.Id, enrollment.Status, credential.Id);
    }

    private async Task ExpireAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        enrollment.Status = EnrollmentStatus.Vencido;
        enrollment.UpdatedAt = now;
        foreach (var command in await _commands.GetOpenForEnrollmentAsync(enrollment.Id, cancellationToken))
        {
            command.Status = DeviceCommandStatus.Vencido;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Enrollment> FindAsync(long id, CancellationToken cancellationToken) =>
        await _enrollments.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Enrolamiento", id);
}
