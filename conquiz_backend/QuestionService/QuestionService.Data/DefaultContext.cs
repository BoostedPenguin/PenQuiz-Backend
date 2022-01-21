using Microsoft.EntityFrameworkCore;
using QuestionService.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuestionService.Data
{
    public class DefaultContext : DbContext
    {
        public DefaultContext(DbContextOptions<DefaultContext> options)
            : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

        }

        public virtual DbSet<Answers> Answers { get; set; }
        public virtual DbSet<Questions> Questions { get; set; }
        public virtual DbSet<GameInstance> GameInstances { get; set; }
        public virtual DbSet<GameSessionQuestions> GameSessionQuestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Answers>(entity =>
            {
                entity.HasIndex(e => e.QuestionId);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Answer)
                    .IsRequired()
                    .HasColumnName("answer")
                    .HasMaxLength(255);

                entity.Property(e => e.Correct)
                    .IsRequired()
                    .HasColumnName("correct");

                entity.Property(e => e.QuestionId).HasColumnName("questionId");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.Answers)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__Answers__questio__5DCAEF64");
            });

            modelBuilder.Entity<Questions>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IsVerified).HasColumnName("isVerified");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasMaxLength(255);

                entity.Property(e => e.Difficulty)
                    .HasColumnName("difficulty")
                    .HasMaxLength(255);

                entity.Property(e => e.Category)
                    .HasColumnName("category")
                    .HasMaxLength(255);

                entity.Property(e => e.VerifiedAt)
                    .HasColumnName("verifiedAt");

                entity.Property(e => e.SubmittedByUsername)
                    .HasColumnName("submittedByUsername")
                    .HasMaxLength(255);

                entity.Property(e => e.SubmittedAt)
                    .HasColumnName("submittedAt");

                entity.Property(e => e.Question)
                    .IsRequired()
                    .HasColumnName("question")
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<GameInstance>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ExternalGlobalId).IsRequired().HasColumnName("externalGlobalId");

                entity.Property(e => e.OpentDbSessionToken)
                    .IsRequired()
                    .HasColumnName("opentDbSessionToken")
                    .HasMaxLength(255);
            });


            modelBuilder.Entity<GameSessionQuestions>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.HasIndex(e => e.QuestionId);

                entity.HasIndex(e => e.GameInstanceId);

                entity.Property(e => e.QuestionId).HasColumnName("questionId");

                entity.Property(e => e.GameInstanceId).HasColumnName("gameInstanceId");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.GameSessionQuestions)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__GameSessQues__quest__5FB337D6");

                entity.HasOne(d => d.GameInstance)
                    .WithMany(p => p.GameSessionQuestions)
                    .HasForeignKey(d => d.GameInstanceId)
                    .HasConstraintName("FK__GameSessQues__game__5EBF139D");
            });
        }
    }
}
