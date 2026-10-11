using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Talabat.Core.Models.Identity;

namespace Talabat.Repository.Identity;

public class AppIdentityDbContext : IdentityDbContext<AppUser>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options):base(options)
    {
        
    }

    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Address>().ToTable("Addresses"); 
        builder.Entity<UserRefreshToken>(token =>
        {
            token.HasKey(entity => entity.Id);
            token.HasIndex(entity => entity.TokenHash).IsUnique();
            token.Property(entity => entity.TokenHash).HasMaxLength(64).IsRequired();
            token.Property(entity => entity.FamilyId).HasMaxLength(32).IsRequired();
            token.Property(entity => entity.UserSecurityStamp).HasMaxLength(256).IsRequired();
            token.HasIndex(entity => entity.FamilyId);
            token.Property(entity => entity.UserId).HasMaxLength(450).IsRequired();
            token.Property(entity => entity.RowVersion).IsRowVersion();
            token.HasOne(entity => entity.User)
                .WithMany()
                .HasForeignKey(entity => entity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
} 
