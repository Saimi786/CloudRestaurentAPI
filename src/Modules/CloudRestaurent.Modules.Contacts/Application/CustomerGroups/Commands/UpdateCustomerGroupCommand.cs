using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Commands;

public sealed record UpdateCustomerGroupCommand(
    Guid Id,
    string Name,
    decimal DiscountPercent,
    string? Description) : IRequest<CustomerGroupDto>;

public sealed class UpdateCustomerGroupValidator : AbstractValidator<UpdateCustomerGroupCommand>
{
    public UpdateCustomerGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class UpdateCustomerGroupHandler(IAppDbContext db)
    : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
{
    public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<CustomerGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("CustomerGroup", request.Id);

        if (await db.Set<CustomerGroup>().AnyAsync(g => g.Id != request.Id && g.Name == request.Name, ct))
            throw new ConflictException($"A customer group named '{request.Name}' already exists.");

        group.Update(request.Name, request.DiscountPercent, request.Description);
        await db.SaveChangesAsync(ct);

        return new CustomerGroupDto(group.Id, group.Name, group.DiscountPercent, group.Description, group.IsActive);
    }
}
