using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Modules;
using CloudRestaurent.Modules.Accounting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Accounting;

public sealed class AccountingModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Accounting",
        Name: "Accounting",
        Version: "1.0.0",
        Description: "Chart of accounts, journal entries, double-entry ledger");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILedgerPoster, LedgerPoster>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountingModule).Assembly);
    }
}
