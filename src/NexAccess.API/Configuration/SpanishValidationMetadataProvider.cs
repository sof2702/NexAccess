using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace NexAccess.API.Configuration;

public class SpanishValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
            {
                continue;
            }

            attribute.ErrorMessage = attribute switch
            {
                RequiredAttribute => "El campo {0} es obligatorio.",
                StringLengthAttribute { MinimumLength: > 0 } => "El campo {0} debe tener entre {2} y {1} caracteres.",
                StringLengthAttribute => "El campo {0} no puede superar {1} caracteres.",
                RangeAttribute => "El campo {0} debe estar entre {1} y {2}.",
                EmailAddressAttribute => "El campo {0} no es un correo electrónico válido.",
                RegularExpressionAttribute => "El campo {0} no tiene un formato válido.",
                _ => attribute.ErrorMessage
            };
        }
    }
}
