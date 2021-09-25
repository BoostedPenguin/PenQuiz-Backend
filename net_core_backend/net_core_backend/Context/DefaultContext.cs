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

                entity.Property(e => e.Admin).HasColumnName("admin");

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
                entity.HasKey(e => new { e.ThisTer, e.BordersTer })
                    .HasName("pk_myConstraint");

                entity.Property(e => e.ThisTer).HasColumnName("thisTER");

                entity.Property(e => e.BordersTer).HasColumnName("bordersTER");

                entity.HasOne(d => d.BordersTerNavigation)
                    .WithMany(p => p.BordersBordersTerNavigation)
                    .HasForeignKey(d => d.BordersTer)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Borders_MapTerritory1");

                entity.HasOne(d => d.ThisTerNavigation)
                    .WithMany(p => p.BordersThisTerNavigation)
                    .HasForeignKey(d => d.ThisTer)
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
