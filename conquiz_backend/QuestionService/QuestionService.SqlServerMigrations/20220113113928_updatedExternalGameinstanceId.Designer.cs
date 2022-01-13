﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuestionService.Data;

namespace QuestionService.SqlServerMigrations.Migrations
{
    [DbContext(typeof(DefaultContext))]
    [Migration("20220113113928_updatedExternalGameinstanceId")]
    partial class updatedExternalGameinstanceId
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.11")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("QuestionService.Data.Models.Answers", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Answer")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("answer");

                    b.Property<bool>("Correct")
                        .HasColumnType("bit")
                        .HasColumnName("correct");

                    b.Property<int>("QuestionId")
                        .HasColumnType("int")
                        .HasColumnName("questionId");

                    b.HasKey("Id");

                    b.HasIndex("QuestionId");

                    b.ToTable("Answers");
                });

            modelBuilder.Entity("QuestionService.Data.Models.GameInstance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("externalId");

                    b.Property<string>("OpentDbSessionToken")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("opentDbSessionToken");

                    b.HasKey("Id");

                    b.ToTable("GameInstances");
                });

            modelBuilder.Entity("QuestionService.Data.Models.GameSessionQuestions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("GameInstanceId")
                        .HasColumnType("int")
                        .HasColumnName("gameInstanceId");

                    b.Property<int>("QuestionId")
                        .HasColumnType("int")
                        .HasColumnName("questionId");

                    b.HasKey("Id");

                    b.HasIndex("GameInstanceId");

                    b.HasIndex("QuestionId");

                    b.ToTable("GameSessionQuestions");
                });

            modelBuilder.Entity("QuestionService.Data.Models.Questions", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Category")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("category");

                    b.Property<string>("Difficulty")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("difficulty");

                    b.Property<string>("Question")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("question");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.ToTable("Questions");
                });

            modelBuilder.Entity("QuestionService.Data.Models.Answers", b =>
                {
                    b.HasOne("QuestionService.Data.Models.Questions", "Question")
                        .WithMany("Answers")
                        .HasForeignKey("QuestionId")
                        .HasConstraintName("FK__Answers__questio__5DCAEF64")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Question");
                });

            modelBuilder.Entity("QuestionService.Data.Models.GameSessionQuestions", b =>
                {
                    b.HasOne("QuestionService.Data.Models.GameInstance", "GameInstance")
                        .WithMany("GameSessionQuestions")
                        .HasForeignKey("GameInstanceId")
                        .HasConstraintName("FK__GameSessQues__game__5EBF139D")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("QuestionService.Data.Models.Questions", "Question")
                        .WithMany("GameSessionQuestions")
                        .HasForeignKey("QuestionId")
                        .HasConstraintName("FK__GameSessQues__quest__5FB337D6")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GameInstance");

                    b.Navigation("Question");
                });

            modelBuilder.Entity("QuestionService.Data.Models.GameInstance", b =>
                {
                    b.Navigation("GameSessionQuestions");
                });

            modelBuilder.Entity("QuestionService.Data.Models.Questions", b =>
                {
                    b.Navigation("Answers");

                    b.Navigation("GameSessionQuestions");
                });
#pragma warning restore 612, 618
        }
    }
}
