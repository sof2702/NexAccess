using System.ComponentModel.DataAnnotations;
using NexAccess.Application.Exceptions;

namespace NexAccess.Application.Validators;

public static class ModelValidator
{
    public static void Validate(object model)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            return;
        }

        var errors = results
            .SelectMany(result => (result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty })
                .Select(member => (member, message: result.ErrorMessage ?? "Valor inválido.")))
            .GroupBy(item => item.member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.message).ToArray());

        throw new AppValidationException(errors);
    }
}
