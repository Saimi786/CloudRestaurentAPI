using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Contacts;

public sealed class ContactsModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Contacts",
        Name: "Contacts",
        Version: "1.0.0",
        Description: "Customers, suppliers, contact ledger");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContactsModule).Assembly);
    }
}
