using Microsoft.EntityFrameworkCore;
using NexAccess.Application.Exceptions;
using NexAccess.Application.Interfaces;
using Npgsql;

namespace NexAccess.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly NexAccessDbContext _context;

    public UnitOfWork(NexAccessDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PersistenceException("El registro fue modificado o eliminado por otra operación.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgres)
        {
            throw Translate(postgres, exception);
        }
        catch (DbUpdateException exception)
        {
            throw new PersistenceException("No se pudieron guardar los cambios.", exception);
        }
    }

    public static AppException Translate(PostgresException postgres, Exception original) => postgres.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => new ConflictException("DUPLICATE_VALUE", $"Ya existe un registro con esos datos ({postgres.ConstraintName})."),
        PostgresErrorCodes.ForeignKeyViolation => new ConflictException("REFERENCE_VIOLATION", $"El registro hace referencia a datos inexistentes o en uso ({postgres.ConstraintName})."),
        PostgresErrorCodes.CheckViolation => new ConflictException("CONSTRAINT_VIOLATION", $"Los datos no cumplen una restricción de la base ({postgres.ConstraintName})."),
        PostgresErrorCodes.NotNullViolation => new ConflictException("CONSTRAINT_VIOLATION", $"Falta un dato obligatorio ({postgres.ColumnName})."),
        _ => new PersistenceException("No se pudieron guardar los cambios.", original)
    };
}
