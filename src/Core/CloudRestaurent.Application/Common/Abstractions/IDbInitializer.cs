namespace CloudRestaurent.Application.Common.Abstractions;

public interface IDbInitializer
{
    Task InitializeAsync(CancellationToken ct = default);
}
