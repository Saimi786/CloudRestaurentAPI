using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Commands;

public sealed record SetTableStatusCommand(Guid Id, TableStatus Status) : IRequest;

public sealed class SetTableStatusValidator : AbstractValidator<SetTableStatusCommand>
{
    public SetTableStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class SetTableStatusHandler(IAppDbContext db) : IRequestHandler<SetTableStatusCommand>
{
    public async Task Handle(SetTableStatusCommand request, CancellationToken ct)
    {
        var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("Table", request.Id);

        if (!table.IsActive)
            throw new BusinessRuleException("Cannot change status of a deactivated table.");

        table.SetStatus(request.Status);
        await db.SaveChangesAsync(ct);
    }
}
