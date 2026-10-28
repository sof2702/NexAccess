
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexAccess.Application.DTOs.Empresa;
using NexAccess.Application.Services;
using NexAccess.Domain.Enums;

namespace NexAccess.API.Controllers;

[ApiController]
[Route("api/v1/empresa")]
[Authorize(Roles = SystemRoles.All)]
public class EmpresaController : ControllerBase
{
    private readonly EmpresaService _empresaService;

    public EmpresaController(EmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    // Consultar información institucional
    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var empresa = await _empresaService.GetAsync(
            cancellationToken);

        if (empresa is null)
        {
            return NotFound(new
            {
                code = "EMPRESA_NOT_CONFIGURED",
                message = "Todavía no se configuraron los datos de la empresa."
            });
        }

        return Ok(empresa);
    }

    // Crear o actualizar información institucional
    [HttpPut]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<ActionResult<EmpresaResponse>> Save(
        [FromBody] EmpresaRequest request,
        CancellationToken cancellationToken)
    {
        var empresa = await _empresaService.SaveAsync(
            request,
            cancellationToken);

        return Ok(empresa);
    }
// Subir o reemplazar el logotipo institucional
[HttpPost("logo")]
[Authorize(Roles = SystemRoles.Admin)]
[Consumes("multipart/form-data")]
[RequestSizeLimit(2 * 1024 * 1024 + 65536)]
public async Task<ActionResult<EmpresaResponse>> UploadLogo(
    [FromForm] IFormFile logo,
    CancellationToken cancellationToken)
{
    if (logo is null || logo.Length == 0)
    {
        return BadRequest(new
        {
            code = "LOGO_REQUIRED",
            message = "Debe seleccionar una imagen."
        });
    }

    if (logo.Length > 2 * 1024 * 1024)
    {
        return BadRequest(new
        {
            code = "LOGO_TOO_LARGE",
            message = "El logotipo no puede superar los 2 MB."
        });
    }

    await using var stream = logo.OpenReadStream();

    var empresa = await _empresaService.SaveLogoAsync(
        stream,
        cancellationToken);

    return Ok(empresa);
}


}

