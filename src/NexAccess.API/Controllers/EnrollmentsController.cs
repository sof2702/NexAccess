using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.API.Configuration;
using NexAccess.Application.DTOs.Enrollments;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController(EnrollmentService enrollments) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public async Task<ActionResult<EnrollmentResponse>> Create(EnrollmentCreateRequest request, CancellationToken cancellationToken)
    {
        var enrollment = await enrollments.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = enrollment.Id }, enrollment);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<EnrollmentResponse> Get(long id, CancellationToken cancellationToken) =>
        enrollments.GetAsync(id, cancellationToken);

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = SystemRoles.AdminOrSeguridad)]
    public Task<EnrollmentResponse> Cancel(long id, CancellationToken cancellationToken) =>
        enrollments.CancelAsync(id, cancellationToken);

    [HttpPost("{id:long}/card")]
    [Authorize(AuthenticationSchemes = DeviceKeyAuthenticationHandler.SchemeName)]
    public Task<EnrollmentCardResponse> SubmitCard(long id, EnrollmentCardRequest request, CancellationToken cancellationToken) =>
        enrollments.SubmitCardAsync(User.GetDevice(), id, request, cancellationToken);
}
