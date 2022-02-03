using AccountService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountService.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        public virtual DbSet<RefreshToken> RefreshToken { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => new { e.UsersId, e.Id });

                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(d => d.Users)
                    .WithMany(p => p.RefreshToken)
                    .HasForeignKey(d => d.UsersId);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasDefaultValue("user");

                entity.Property(e => e.IsBanned).HasColumnName("isBanned");

                entity.Property(e => e.IsInGame).HasColumnName("isInGame").HasDefaultValue(false);

                entity.Property(e => e.UserGlobalIdentifier).IsRequired().HasColumnName("userGlobalIdentifier");

                entity.Property(e => e.IsOnline).HasColumnName("isOnline");
                
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");

                entity.Property(e => e.LastLoggedAt).HasColumnName("lastLoggedAt");

                entity.Property(e => e.Provider).HasColumnName("provider");

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}
