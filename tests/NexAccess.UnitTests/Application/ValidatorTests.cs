using NexAccess.Application.DTOs.Users;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Services.Reports;
using NexAccess.Application.Validators;
using NexAccess.Domain.Enums;

namespace NexAccess.UnitTests.Application;

public class ValidatorTests
{
    [Theory]
    [InlineData("04:A1:B2:C3", "04A1B2C3", true)]
    [InlineData(" 04 a1 b2 c3 ", "04A1B2C3", true)]
    [InlineData("04-a1-b2", "04A1B2", true)]
    [InlineData("ab", "AB", false)]
    [InlineData("04A1;DROP", "04A1;DROP", false)]
    [InlineData(null, "", false)]
    public void Credential_code_is_normalized_and_validated(string? raw, string normalized, bool valid)
    {
        Assert.Equal(valid, CredentialCode.TryNormalize(raw, out var result));
        Assert.Equal(normalized, result);
    }

    [Fact]
    public void Credential_code_mask_shows_only_last_four()
    {
        Assert.Equal("******C3D4", CredentialCode.Mask("04A1B2C3D4"));
        Assert.Equal("***", CredentialCode.Mask("ABC"));
    }

    [Fact]
    public void Model_validator_reports_all_invalid_fields()
    {
        var error = Assert.Throws<AppValidationException>(() =>
            ModelValidator.Validate(new CreateUserRequest { Username = "a", Password = "corta" }));

        Assert.Contains(nameof(CreateUserRequest.Username), error.Errors.Keys);
        Assert.Contains(nameof(CreateUserRequest.Password), error.Errors.Keys);
        Assert.Contains(nameof(CreateUserRequest.Role), error.Errors.Keys);
    }

    [Fact]
    public void Model_validator_accepts_valid_dto()
    {
        ModelValidator.Validate(new CreateUserRequest { Username = "rrhh.ana", Password = "Clave-Segura-123", Role = SystemRole.Rrhh });
    }

    [Fact]
    public void Inverted_date_range_is_rejected()
    {
        Assert.Throws<AppValidationException>(() => DateRange.Ensure(new DateTime(2026, 10, 2), new DateTime(2026, 10, 1)));
    }

    [Fact]
    public void Csv_escapes_separators_quotes_and_formula_injection()
    {
        var csv = CsvBuilder.Build(new[] { "a", "b", "c" }, new[] { new object?[] { "x,y", "di \"hola\"", "=CMD()" } });

        Assert.Equal("a,b,c\r\n\"x,y\",\"di \"\"hola\"\"\",'=CMD()\r\n", csv);
    }
}
