﻿using Maliwan.Domain.Core.Data;
using Maliwan.Domain.IdentityContext.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace Maliwan.Infra.Data.Contexts.IdentityDb.Mappings;

public class RefreshTokenMapping : EntityMap<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("RefreshTokens");

        builder.HasIndex(e => e.Token)
            .IsUnique();

        builder.Property(e => e.UserId)
            .HasColumnOrder(2);

        builder.Property(e => e.Token)
            .HasColumnOrder(3)
            .IsRequired();

        builder.Property(e => e.ValidUntil)
            .HasColumnOrder(4);

        builder.Property(e => e.CreatedAt)
            .HasColumnOrder(5);

        builder.Property(e => e.UpdatedAt)
            .HasColumnOrder(6);

        builder.Property(e => e.DeletedAt)
            .HasColumnOrder(7);

        #region Relationships

        builder.HasOne(e => e.User)
            .WithMany(e => e.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        #endregion
    }
}