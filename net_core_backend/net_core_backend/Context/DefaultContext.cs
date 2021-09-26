using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace net_core_backend.Models
{
    public partial class DefaultContext : DbContext
    {
        public DefaultContext()
        {
        }

        public DefaultContext(DbContextOptions<DefaultContext> options)
            : base(options)
        {

        }

        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Borders> Borders { get; set; }
        public virtual DbSet<MapTerritory> MapTerritory { get; set; }
        public virtual DbSet<Maps> Maps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IsAdmin).HasColumnName("admin");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasMaxLength(100);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Borders>(entity =>
            {
                entity.HasKey(e => new { e.ThisTerritory, e.NextToTerritory })
                    .HasName("pk_myConstraint");

                entity.Property(e => e.ThisTerritory).HasColumnName("ThisTerritory");

                entity.Property(e => e.NextToTerritory).HasColumnName("NextToTerritory");

                entity.HasOne(d => d.ThisTerritoryReference)
                    .WithMany(p => p.BordersNextToTerritoryReference)
                    .HasForeignKey(d => d.NextToTerritory)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Borders_MapTerritory1");

                entity.HasOne(d => d.NextToTerritoryReference)
                    .WithMany(p => p.BordersThisTerritoryReference)
                    .HasForeignKey(d => d.ThisTerritory)
                    .HasConstraintName("FK_Borders_MapTerritory");
            });

            modelBuilder.Entity<MapTerritory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.MapId).HasColumnName("mapId");

                entity.Property(e => e.TerritoryName)
                    .IsRequired()
                    .HasColumnName("territoryName")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.Map)
                    .WithMany(p => p.MapTerritory)
                    .HasForeignKey(d => d.MapId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MapTerritory_Maps");
            });

            modelBuilder.Entity<Maps>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
