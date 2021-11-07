﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using GameService.Models;

namespace GameService.Context
{
    public partial class DefaultContext : DbContext
    {
        public DefaultContext(DbContextOptions<DefaultContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        public virtual DbSet<Answers> Answers { get; set; }
        public virtual DbSet<Borders> Borders { get; set; }
        public virtual DbSet<GameInstance> GameInstance { get; set; }
        public virtual DbSet<MapTerritory> MapTerritory { get; set; }
        public virtual DbSet<Maps> Maps { get; set; }
        public virtual DbSet<ObjectTerritory> ObjectTerritory { get; set; }
        public virtual DbSet<Participants> Participants { get; set; }
        public virtual DbSet<Questions> Questions { get; set; }
        public virtual DbSet<Round> Round { get; set; }
        public virtual DbSet<PvpRound> PvpRounds { get; set; }
        public virtual DbSet<PvpRoundAnswers> PvpRoundAnswers { get; set; }
        public virtual DbSet<NeutralRound> NeutralRound { get; set; }
        public virtual DbSet<AttackingNeutralTerritory> AttackingNeutralTerritory { get; set; }
        public virtual DbSet<Users> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Answers>(entity =>
            {
                entity.HasIndex(e => e.QuestionId);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Answer)
                    .HasColumnName("answer")
                    .HasMaxLength(255);

                entity.Property(e => e.Correct).HasColumnName("correct");

                entity.Property(e => e.QuestionId).HasColumnName("questionId");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.Answers)
                    .HasForeignKey(d => d.QuestionId)
                    .HasConstraintName("FK__Answers__questio__5DCAEF64");
            });

            modelBuilder.Entity<Borders>(entity =>
            {
                entity.HasKey(e => new { e.ThisTerritory, e.NextToTerritory })
                    .HasName("pk_myConstraint");

                entity.HasIndex(e => e.NextToTerritory);

                entity.HasOne(d => d.NextToTerritoryNavigation)
                    .WithMany(p => p.BordersNextToTerritoryNavigation)
                    .HasForeignKey(d => d.NextToTerritory)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Borders_MapTerritory1");

                entity.HasOne(d => d.ThisTerritoryNavigation)
                    .WithMany(p => p.BordersThisTerritoryNavigation)
                    .HasForeignKey(d => d.ThisTerritory)
                    .HasConstraintName("FK_Borders_MapTerritory");
            });

            modelBuilder.Entity<GameInstance>(entity =>
            {
                entity.HasIndex(e => e.Mapid);

                entity.HasIndex(e => e.ParticipantsId);

                entity.HasIndex(e => e.ResultId);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.GameRoundNumber).HasColumnName("gameRoundNumber");

                entity.Property(e => e.EndTime)
                    .HasColumnName("end_time")
                    .HasColumnType("datetime");

                entity.Property(e => e.GameState).HasConversion<string>().HasDefaultValue(GameState.IN_LOBBY);

                entity.Property(e => e.InvitationLink)
                    .HasColumnName("invitationLink")
                    .HasMaxLength(1500);

                entity.Property(e => e.GameCreatorId).HasColumnName("gameCreatorId");

                entity.Property(e => e.QuestionTimerSeconds).HasColumnName("questionTimerSeconds");

                entity.Property(e => e.ResultId).HasColumnName("resultId");

                entity.Property(e => e.StartTime)
                    .HasColumnName("start_time")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Map)
                    .WithMany(p => p.GameInstance)
                    .HasForeignKey(d => d.Mapid)
                    .HasConstraintName("FK_GameInstance_Maps");

            });

            modelBuilder.Entity<MapTerritory>(entity =>
            {
                entity.HasIndex(e => e.MapId);

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

            modelBuilder.Entity<ObjectTerritory>(entity =>
            {
                entity.HasIndex(e => e.GameInstanceId);

                entity.HasIndex(e => e.MapTerritoryId);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.GameInstanceId).HasColumnName("gameInstanceId");

                entity.Property(e => e.MapTerritoryId).HasColumnName("mapTerritoryId");

                entity.Property(e => e.TakenBy).HasColumnName("takenBy");

                entity.Property(e => e.IsCapital).HasColumnName("isCapital").HasDefaultValue(false);
                
                entity.Property(e => e.TerritoryScore).HasColumnName("territoryScore").HasDefaultValue(0);

                entity.Property(e => e.IsAttacked).HasColumnName("isAttacked").HasDefaultValue(false);

                entity.HasOne(d => d.GameInstance)
                    .WithMany(p => p.ObjectTerritory)
                    .HasForeignKey(d => d.GameInstanceId)
                    .HasConstraintName("FK__ObjectTer__gameIn__5AEE82B9");

                entity.HasOne(d => d.MapTerritory)
                    .WithMany(p => p.ObjectTerritory)
                    .HasForeignKey(d => d.MapTerritoryId)
                    .HasConstraintName("FK__ObjectTer__mapTe__59FA5E80");
            });

            modelBuilder.Entity<Participants>(entity =>
            {
                entity.HasIndex(e => e.GameId);

                entity.HasIndex(e => e.PlayerId);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.GameId).HasColumnName("gameId");

                entity.Property(e => e.PlayerId).HasColumnName("playerId");

                entity.Property(e => e.IsBot).HasColumnName("isBot").HasDefaultValue(false);

                entity.Property(e => e.Score).HasColumnName("score");

                entity.Property(e => e.AvatarName)
                    .IsRequired()
                    .HasColumnName("avatarName")
                    .HasDefaultValue("penguinAvatar.svg")
                    .HasMaxLength(50);

                entity.HasOne(d => d.Game)
                    .WithMany(p => p.Participants)
                    .HasForeignKey(d => d.GameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Participa__gameI__5812160E");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Participants)
                    .HasForeignKey(d => d.PlayerId)
                    .HasConstraintName("FK__Participa__playe__571DF1D5");
            });

            modelBuilder.Entity<Questions>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Question)
                    .HasColumnName("question")
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<Round>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.HasIndex(e => e.GameInstanceId);
                entity.Property(e => e.GameInstanceId).HasColumnName("gameInstanceId");

                entity.Property(e => e.AttackStage)
                    .HasConversion<string>()
                    .HasDefaultValue(AttackStage.MULTIPLE_NEUTRAL);

                entity.Property(e => e.RoundStage)
                    .HasConversion<string>()
                    .HasDefaultValue(RoundStage.NOT_STARTED);

                entity.Property(e => e.GameRoundNumber)
                    .HasColumnName("gameRoundNumber");

                entity.Property(e => e.IsQuestionVotingOpen)
                    .HasColumnName("isQuestionVotingOpen")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsTerritoryVotingOpen)
                    .HasColumnName("isTerritoryVotingOpen")
                    .HasDefaultValue(false);

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(255);

                entity.HasOne(d => d.GameInstance)
                    .WithMany(p => p.Rounds)
                    .HasForeignKey(d => d.GameInstanceId)
                    .HasConstraintName("FK__Round__gameI__5CD6CB2B");

                entity.HasOne(d => d.NeutralRound)
                    .WithOne(x => x.Round)
                    .HasForeignKey<NeutralRound>(e => e.RoundId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.PvpRound)
                    .WithOne(x => x.Round)
                    .HasForeignKey<PvpRound>(e => e.RoundId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Question)
                    .WithOne(x => x.Round)
                    .HasForeignKey<Questions>(e => e.RoundId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<NeutralRound>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
            });

            modelBuilder.Entity<AttackingNeutralTerritory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AttackerId).HasColumnName("attackerId");

                entity.Property(e => e.AttackerWon)
                    .HasColumnName("attackerWon")
                    .IsRequired(false);

                entity.Property(e => e.AttackedTerritoryId)
                    .HasColumnName("attackedTerritoryId")
                    .IsRequired(false);

                entity.Property(e => e.AttackerMChoiceQAnswerId)
                    .HasColumnName("attackerMChoiceQAnswerId")
                    .IsRequired(false);

                entity.Property(e => e.AttackerNumberQAnswer)
                    .HasColumnName("attackerNumberQAnswer")
                    .IsRequired(false);

                entity.HasOne(d => d.NeutralRound)
                    .WithMany(p => p.TerritoryAttackers)
                    .HasForeignKey(d => d.NeutralRoundId)
                    .HasConstraintName("FK__NeuRound__terAtt__8AWDJXCS");

                entity.HasOne(d => d.AttackedTerritory)
                    .WithMany(p => p.NeutralRoundsAttacks)
                    .HasForeignKey(d => d.NeutralRoundId)
                    .HasConstraintName("FK__attTer__neuAtt__8AJAWDSW");
            });

            modelBuilder.Entity<PvpRound>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                
                entity.Property(e => e.AttackerId).HasColumnName("attackerId");

                entity.Property(e => e.DefenderId)
                    .HasColumnName("defenderId")
                    .IsRequired(false);

                entity.Property(e => e.WinnerId)
                    .HasColumnName("winnerId")
                    .IsRequired(false);

                entity.HasOne(d => d.AttackedTerritory)
                    .WithMany(p => p.PvpRounds)
                    .HasForeignKey(d => d.AttackedTerritoryId)
                    .HasConstraintName("FK__attTer__pvpRou__123JKAWD");
            });

            modelBuilder.Entity<PvpRoundAnswers>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.Property(e => e.MChoiceQAnswerId)
                    .HasColumnName("mChoiceQAnswerId")
                    .IsRequired(false);

                entity.Property(e => e.NumberQAnswer)
                    .HasColumnName("numberQAnswer")
                    .IsRequired(false);

                entity.HasOne(d => d.PvpRound)
                    .WithMany(p => p.PvpRoundAnswers)
                    .HasForeignKey(d => d.PvpRoundId)
                    .HasConstraintName("FK__pvpRou__pvpRouAns__A8AWDJBNS");
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ExternalId).HasColumnName("externalId");

                entity.Property(e => e.IsInGame).HasColumnName("isInGame").HasDefaultValue(false);

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
