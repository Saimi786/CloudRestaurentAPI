using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;

namespace CloudRestaurent.Modules.Identity.Application.Users.Common;

internal static class IdentityErrorMapper
{
    /// <summary>
    /// Translates Identity-layer failures into the project's typed exceptions
    /// so the global handler maps them to consistent ProblemDetails responses.
    /// </summary>
    public static Exception ToAppException(IdentityOperationException ex)
    {
        var msg = ex.Message;

        if (msg.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return new NotFoundException("User", msg);

        if (msg.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("DuplicateEmail", StringComparison.OrdinalIgnoreCase))
            return new ConflictException(msg);

        if (msg.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            ex.Errors?.Keys.Any(k => k.StartsWith("Password", StringComparison.OrdinalIgnoreCase)) == true)
        {
            return new ValidationException(new Dictionary<string, string[]>
            {
                ["password"] = ex.Errors?.Values.SelectMany(v => v).ToArray() ?? [msg]
            });
        }

        if (msg.Contains("role", StringComparison.OrdinalIgnoreCase))
            return new ValidationException(new Dictionary<string, string[]>
            {
                ["roles"] = [msg]
            });

        return new BusinessRuleException(msg);
    }
}
