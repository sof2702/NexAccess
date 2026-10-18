using NexAccess.Application.Exceptions;
using NexAccess.Domain.Enums;
using NexAccess.Infrastructure.Persistence;
using NexAccess.Infrastructure.Persistence.Configurations;
using Npgsql;

namespace NexAccess.UnitTests.Infrastructure;

public class PersistenceTests
{
    [Theory]
    [InlineData(DeviceCommandType.EnrollCard, "ENROLL_CARD")]
    [InlineData(SystemRole.Rrhh, "RRHH")]
    [InlineData(CredentialType.Fisica, "FISICA")]
    [InlineData(DeviceType.Marcacion, "MARCACION")]
    public void Enum_converter_matches_database_values(object value, string expected)
    {
        var method = typeof(UpperSnakeEnumConverter<>).MakeGenericType(value.GetType()).GetMethod(nameof(UpperSnakeEnumConverter<DeviceType>.ToDatabase))!;

        Assert.Equal(expected, method.Invoke(null, new[] { value }));
    }

    [Fact]
    public void Enum_converter_reads_database_values()
    {
        Assert.Equal(DeviceCommandType.EnrollCard, UpperSnakeEnumConverter<DeviceCommandType>.FromDatabase("ENROLL_CARD"));
        Assert.Equal(AttendanceSource.Manual, UpperSnakeEnumConverter<AttendanceSource>.FromDatabase("MANUAL"));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, typeof(ConflictException), "DUPLICATE_VALUE")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, typeof(ConflictException), "REFERENCE_VIOLATION")]
    [InlineData(PostgresErrorCodes.CheckViolation, typeof(ConflictException), "CONSTRAINT_VIOLATION")]
    [InlineData(PostgresErrorCodes.DeadlockDetected, typeof(PersistenceException), "PERSISTENCE_ERROR")]
    public void Postgres_errors_are_translated(string sqlState, Type expectedType, string expectedCode)
    {
        var postgres = new PostgresException("error", "ERROR", "ERROR", sqlState);

        var translated = UnitOfWork.Translate(postgres, postgres);

        Assert.IsType(expectedType, translated);
        Assert.Equal(expectedCode, translated.Code);
    }
}
