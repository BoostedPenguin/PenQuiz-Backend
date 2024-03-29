﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using GameService.Data;

namespace GameService.SqlServerMigrations.Migrations
{
    [DbContext(typeof(DefaultContext))]
    [Migration("20210930085027_attackedby-territory")]
    partial class attackedbyterritory
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GameService.Models.Answers", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Answer")
                        .HasColumnName("answer")
                        .HasColumnType("nvarchar(255)")
                        .HasMaxLength(255);

                    b.Property<bool>("Correct")
                        .HasColumnName("correct")
                        .HasColumnType("bit");

                    b.Property<int>("QuestionId")
                        .HasColumnName("questionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("QuestionId");

                    b.ToTable("Answers");
                });

            modelBuilder.Entity("GameService.Models.Borders", b =>
                {
                    b.Property<int>("ThisTerritory")
                        .HasColumnType("int");

                    b.Property<int>("NextToTerritory")
                        .HasColumnType("int");

                    b.HasKey("ThisTerritory", "NextToTerritory")
                        .HasName("pk_myConstraint");

                    b.HasIndex("NextToTerritory");

                    b.ToTable("Borders");
                });

            modelBuilder.Entity("GameService.Models.GameInstance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("EndTime")
                        .HasColumnName("end_time")
                        .HasColumnType("datetime");

                    b.Property<bool>("InProgress")
                        .HasColumnName("inProgress")
                        .HasColumnType("bit");

                    b.Property<int>("Mapid")
                        .HasColumnType("int");

                    b.Property<int>("ParticipantsId")
                        .HasColumnType("int");

                    b.Property<int>("QuestionTimerSeconds")
                        .HasColumnName("questionTimerSeconds")
                        .HasColumnType("int");

                    b.Property<int>("ResultId")
                        .HasColumnName("resultId")
                        .HasColumnType("int");

                    b.Property<DateTime>("StartTime")
                        .HasColumnName("start_time")
                        .HasColumnType("datetime");

                    b.HasKey("Id");

                    b.HasIndex("Mapid");

                    b.HasIndex("ParticipantsId");

                    b.HasIndex("ResultId");

                    b.ToTable("GameInstance");
                });

            modelBuilder.Entity("GameService.Models.GameResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("nvarchar(255)")
                        .HasMaxLength(255);

                    b.HasKey("Id");

                    b.ToTable("GameResult");
                });

            modelBuilder.Entity("GameService.Models.MapTerritory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("MapId")
                        .HasColumnName("mapId")
                        .HasColumnType("int");

                    b.Property<string>("TerritoryName")
                        .IsRequired()
                        .HasColumnName("territoryName")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.HasIndex("MapId");

                    b.ToTable("MapTerritory");
                });

            modelBuilder.Entity("GameService.Models.Maps", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnName("name")
                        .HasColumnType("varchar(50)")
                        .HasMaxLength(50)
                        .IsUnicode(false);

                    b.HasKey("Id");

                    b.ToTable("Maps");
                });

            modelBuilder.Entity("GameService.Models.ObjectTerritory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("AttackedBy")
                        .HasColumnName("attackedBy")
                        .HasColumnType("int");

                    b.Property<int>("MapObjectId")
                        .HasColumnName("mapObjectId")
                        .HasColumnType("int");

                    b.Property<int>("MapTerritoryId")
                        .HasColumnName("mapTerritoryId")
                        .HasColumnType("int");

                    b.Property<int?>("TakenBy")
                        .HasColumnName("takenBy")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("MapObjectId");

                    b.HasIndex("MapTerritoryId");

                    b.ToTable("ObjectTerritory");
                });

            modelBuilder.Entity("GameService.Models.Participants", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("GameId")
                        .HasColumnName("gameId")
                        .HasColumnType("int");

                    b.Property<int>("PlayerId")
                        .HasColumnName("playerId")
                        .HasColumnType("int");

                    b.Property<int>("Score")
                        .HasColumnName("score")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("PlayerId");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("GameService.Models.Questions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("IsNumberQuestion")
                        .HasColumnName("isNumberQuestion")
                        .HasColumnType("bit");

                    b.Property<string>("Question")
                        .HasColumnName("question")
                        .HasColumnType("nvarchar(255)")
                        .HasMaxLength(255);

                    b.HasKey("Id");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("GameService.Models.RefreshToken", b =>
                {
                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime2");

                    b.Property<string>("CreatedByIp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime2");

                    b.Property<string>("ReplacedByToken")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("Revoked")
                        .HasColumnType("datetime2");

                    b.Property<string>("RevokedByIp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Token")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UsersId", "Id");

                    b.ToTable("RefreshToken");
                });

            modelBuilder.Entity("GameService.Models.RoundQuestion", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("QuestionId")
                        .HasColumnName("questionId")
                        .HasColumnType("int");

                    b.Property<int>("RoundId")
                        .HasColumnName("roundId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("QuestionId");

                    b.HasIndex("RoundId");

                    b.ToTable("RoundQuestion");
                });

            modelBuilder.Entity("GameService.Models.Rounds", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("TotalRounds")
                        .HasColumnName("totalRounds")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Rounds");
                });

            modelBuilder.Entity("GameService.Models.RoundsHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AttackerId")
                        .HasColumnName("attackerId")
                        .HasColumnType("int");

                    b.Property<int>("DefenderId")
                        .HasColumnName("defenderId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("nvarchar(255)")
                        .HasMaxLength(255);

                    b.Property<int>("GameInstanceId")
                        .HasColumnName("gameInstanceId")
                        .HasColumnType("int");

                    b.Property<int>("RoundId")
                        .HasColumnName("roundID")
                        .HasColumnType("int");

                    b.Property<int>("RoundWinnerId")
                        .HasColumnName("roundWinnerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GameInstanceId");

                    b.HasIndex("RoundId");

                    b.ToTable("RoundsHistory");
                });

            modelBuilder.Entity("GameService.Models.Users", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnName("email")
                        .HasColumnType("nvarchar(100)")
                        .HasMaxLength(100);

                    b.Property<bool>("IsAdmin")
                        .HasColumnName("isAdmin")
                        .HasColumnType("bit");

                    b.Property<bool>("IsBanned")
                        .HasColumnName("isBanned")
                        .HasColumnType("bit");

                    b.Property<bool>("IsOnline")
                        .HasColumnName("isOnline")
                        .HasColumnType("bit");

                    b.Property<bool>("Provider")
                        .HasColumnName("provider")
                        .HasColumnType("bit");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnName("username")
                        .HasColumnType("nvarchar(50)")
                        .HasMaxLength(50);

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("GameService.Models.Answers", b =>
                {
                    b.HasOne("GameService.Models.Questions", "Question")
                        .WithMany("Answers")
                        .HasForeignKey("QuestionId")
                        .HasConstraintName("FK__Answers__questio__5DCAEF64")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.Borders", b =>
                {
                    b.HasOne("GameService.Models.MapTerritory", "NextToTerritoryNavigation")
                        .WithMany("BordersNextToTerritoryNavigation")
                        .HasForeignKey("NextToTerritory")
                        .HasConstraintName("FK_Borders_MapTerritory1")
                        .IsRequired();

                    b.HasOne("GameService.Models.MapTerritory", "ThisTerritoryNavigation")
                        .WithMany("BordersThisTerritoryNavigation")
                        .HasForeignKey("ThisTerritory")
                        .HasConstraintName("FK_Borders_MapTerritory")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.GameInstance", b =>
                {
                    b.HasOne("GameService.Models.Maps", "Map")
                        .WithMany("GameInstance")
                        .HasForeignKey("Mapid")
                        .HasConstraintName("FK_GameInstance_Maps")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameService.Models.Participants", "Participants")
                        .WithMany("GameInstance")
                        .HasForeignKey("ParticipantsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameService.Models.GameResult", "Result")
                        .WithMany("GameInstance")
                        .HasForeignKey("ResultId")
                        .HasConstraintName("FK__GameInsta__resul__59063A47")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.MapTerritory", b =>
                {
                    b.HasOne("GameService.Models.Maps", "Map")
                        .WithMany("MapTerritory")
                        .HasForeignKey("MapId")
                        .HasConstraintName("FK_MapTerritory_Maps")
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.ObjectTerritory", b =>
                {
                    b.HasOne("GameService.Models.GameInstance", "MapObject")
                        .WithMany("ObjectTerritory")
                        .HasForeignKey("MapObjectId")
                        .HasConstraintName("FK__ObjectTer__mapOb__5AEE82B9")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameService.Models.MapTerritory", "MapTerritory")
                        .WithMany("ObjectTerritory")
                        .HasForeignKey("MapTerritoryId")
                        .HasConstraintName("FK__ObjectTer__mapTe__59FA5E80")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.Participants", b =>
                {
                    b.HasOne("GameService.Models.GameInstance", "Game")
                        .WithMany("ParticipantsNavigation")
                        .HasForeignKey("GameId")
                        .HasConstraintName("FK__Participa__gameI__5812160E")
                        .IsRequired();

                    b.HasOne("GameService.Models.Users", "Player")
                        .WithMany("Participants")
                        .HasForeignKey("PlayerId")
                        .HasConstraintName("FK__Participa__playe__571DF1D5")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.RefreshToken", b =>
                {
                    b.HasOne("GameService.Models.Users", "Users")
                        .WithMany("RefreshToken")
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.RoundQuestion", b =>
                {
                    b.HasOne("GameService.Models.Questions", "Question")
                        .WithMany("RoundQuestion")
                        .HasForeignKey("QuestionId")
                        .HasConstraintName("FK__RoundQues__quest__5FB337D6")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameService.Models.RoundsHistory", "Round")
                        .WithMany("RoundQuestion")
                        .HasForeignKey("RoundId")
                        .HasConstraintName("FK__RoundQues__round__5EBF139D")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GameService.Models.RoundsHistory", b =>
                {
                    b.HasOne("GameService.Models.GameInstance", "GameInstance")
                        .WithMany("RoundsHistory")
                        .HasForeignKey("GameInstanceId")
                        .HasConstraintName("FK__RoundsHis__gameI__5CD6CB2B")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GameService.Models.Rounds", "Round")
                        .WithMany("RoundsHistory")
                        .HasForeignKey("RoundId")
                        .HasConstraintName("FK__RoundsHis__round__5BE2A6F2")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
