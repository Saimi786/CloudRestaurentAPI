using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudRestaurent.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.BatchYield).HasPrecision(18, 4).HasDefaultValue(1m);

        builder.HasIndex(r => new { r.TenantId, r.ProductId }).IsUnique();

        builder.HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        // No AutoInclude — read paths query child tables explicitly, write paths
        // bulk-delete via ExecuteDelete to avoid change-tracker conflicts.
    }
}

public sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("RecipeSteps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Instruction).HasMaxLength(1000).IsRequired();

        builder.HasIndex(s => s.RecipeId);
        builder.HasIndex(s => new { s.RecipeId, s.StepNumber }).IsUnique();
    }
}

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasPrecision(18, 6).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(500);

        builder.HasIndex(i => i.RecipeId);
        builder.HasIndex(i => i.IngredientProductId);
        builder.HasIndex(i => new { i.RecipeId, i.IngredientProductId }).IsUnique();
    }
}
