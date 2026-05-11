namespace CloudRestaurent.Application.Common.Exceptions;

public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string entity, object key)
    : AppException($"{entity} with key '{key}' was not found.")
{
    public string Entity { get; } = entity;
    public object Key { get; } = key;
}

public sealed class BusinessRuleException(string message) : AppException(message);

public sealed class ForbiddenException(string message = "Access denied.") : AppException(message);

public sealed class UnauthorizedException(string message = "Authentication required.") : AppException(message);

public sealed class ConflictException(string message) : AppException(message);

public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : this(failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()))
    {
    }
}
