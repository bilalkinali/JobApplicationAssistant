using System;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace JobApplicationAssistant.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.AiRun", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<int>("AttemptCount").HasColumnType("integer");
                b.Property<DateTimeOffset?>("CompletedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("ErrorCode").HasMaxLength(120).HasColumnType("character varying(120)");
                b.Property<string>("ErrorMessage").HasMaxLength(4000).HasColumnType("character varying(4000)");
                b.Property<string>("InputSummary").HasColumnType("jsonb");
                b.Property<Guid?>("JobApplicationId").HasColumnType("uuid");
                b.Property<string>("Model").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");
                b.Property<string>("OutputSummary").HasColumnType("jsonb");
                b.Property<string>("Provider").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<DateTimeOffset>("StartedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("Status").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<string>("Step").IsRequired().HasMaxLength(120).HasColumnType("character varying(120)");

                b.HasKey("Id");
                b.HasIndex("JobApplicationId");
                b.ToTable("ai_runs");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.GeneratedDraft", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<DateTimeOffset?>("AuditUpdatedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("ClaimAudit").IsRequired().HasColumnType("jsonb");
                b.Property<string>("CoverLetterText").IsRequired().HasColumnType("text");
                b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<DateTimeOffset>("GeneratedAt").HasColumnType("timestamp with time zone");
                b.Property<bool>("IsClaimAuditStale")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
                b.Property<Guid>("JobApplicationId").HasColumnType("uuid");
                b.Property<DateTimeOffset?>("LastEditedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("ShortMotivationText").IsRequired().HasColumnType("text");
                b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");

                b.HasKey("Id");
                b.HasIndex("JobApplicationId").IsUnique();
                b.ToTable("generated_drafts");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.JobApplication", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<string>("ApplicationUrl").HasMaxLength(500).HasColumnType("character varying(500)");
                b.Property<string>("ApprovedEvidence").IsRequired().HasColumnType("jsonb");
                b.Property<string>("CompanyName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("CustomFacts").IsRequired().HasColumnType("jsonb");
                b.Property<DateOnly?>("Deadline").HasColumnType("date");
                b.Property<string>("DetectedLanguage").HasMaxLength(40).HasColumnType("character varying(40)");
                b.Property<string>("EvidenceMatches").IsRequired().HasColumnType("jsonb");
                b.Property<string>("JobPostingText").IsRequired().HasColumnType("text");
                b.Property<string>("JobSignals").IsRequired().HasColumnType("jsonb");
                b.Property<DateTimeOffset?>("LastPreparedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("PreparationStatus").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<string>("RoleTitle").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("SelectedLanguage").HasMaxLength(40).HasColumnType("character varying(40)");
                b.Property<string>("Status").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<string>("UnmatchedRequirements").IsRequired().HasColumnType("jsonb");
                b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");

                b.HasKey("Id");
                b.ToTable("job_applications");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.Profile", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("DanishTone").HasMaxLength(2000).HasColumnType("character varying(2000)");
                b.Property<string>("DefaultLanguage").IsRequired().HasMaxLength(40).HasColumnType("character varying(40)");
                b.Property<string>("Email").IsRequired().HasMaxLength(320).HasColumnType("character varying(320)");
                b.Property<string>("EnglishTone").HasMaxLength(2000).HasColumnType("character varying(2000)");
                b.Property<string>("FullName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("GitHubUrl").HasMaxLength(500).HasColumnType("character varying(500)");
                b.Property<string>("LinkedInUrl").HasMaxLength(500).HasColumnType("character varying(500)");
                b.Property<string>("Location").HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("Phone").HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<string>("PortfolioUrl").HasMaxLength(500).HasColumnType("character varying(500)");
                b.Property<int>("SingletonKey").ValueGeneratedOnAdd().HasColumnType("integer").HasDefaultValue(1);
                b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");

                b.HasKey("Id");
                b.HasIndex("SingletonKey").IsUnique();
                b.ToTable("profiles", t =>
                {
                    t.HasCheckConstraint("CK_profiles_singleton_key", "\"SingletonKey\" = 1");
                });
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.ProfileFact", b =>
            {
                b.Property<Guid>("Id").HasColumnType("uuid");
                b.Property<string>("AllowedClaims").IsRequired().HasColumnType("jsonb");
                b.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone");
                b.Property<string>("FactItems").IsRequired().HasColumnType("jsonb");
                b.Property<string>("ForbiddenClaims").IsRequired().HasColumnType("jsonb");
                b.Property<bool>("ManuallyEdited").HasColumnType("boolean");
                b.Property<string>("OriginalImportedSnapshot").HasColumnType("jsonb");
                b.Property<string>("SourceDocumentIds").IsRequired().HasColumnType("jsonb");
                b.Property<ProfileFactStatus>("Status").HasConversion<string>().HasMaxLength(32).HasColumnType("character varying(32)");
                b.Property<string>("Summary").IsRequired().HasMaxLength(4000).HasColumnType("character varying(4000)");
                b.Property<string>("Technologies").IsRequired().HasColumnType("jsonb");
                b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
                b.Property<string>("Type").IsRequired().HasMaxLength(80).HasColumnType("character varying(80)");
                b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("timestamp with time zone");

                b.HasKey("Id");
                b.ToTable("profile_facts");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.AiRun", b =>
            {
                b.HasOne("JobApplicationAssistant.Api.Domain.JobApplication", "JobApplication")
                    .WithMany("AiRuns")
                    .HasForeignKey("JobApplicationId")
                    .OnDelete(DeleteBehavior.SetNull);

                b.Navigation("JobApplication");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.GeneratedDraft", b =>
            {
                b.HasOne("JobApplicationAssistant.Api.Domain.JobApplication", "JobApplication")
                    .WithOne("GeneratedDraft")
                    .HasForeignKey("JobApplicationAssistant.Api.Domain.GeneratedDraft", "JobApplicationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("JobApplication");
            });

            modelBuilder.Entity("JobApplicationAssistant.Api.Domain.JobApplication", b =>
            {
                b.Navigation("AiRuns");
                b.Navigation("GeneratedDraft");
            });
        }
    }
}
