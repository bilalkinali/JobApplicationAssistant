using JobApplicationAssistant.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationAssistant.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<ProfileFact> ProfileFacts => Set<ProfileFact>();

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    public DbSet<GeneratedDraft> GeneratedDrafts => Set<GeneratedDraft>();

    public DbSet<AiRun> AiRuns => Set<AiRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles", table =>
                table.HasCheckConstraint("CK_profiles_singleton_key", "\"SingletonKey\" = 1"));
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.SingletonKey).IsUnique();

            entity.Property(profile => profile.SingletonKey).HasDefaultValue(1);
            entity.Property(profile => profile.FullName).HasMaxLength(200);
            entity.Property(profile => profile.Email).HasMaxLength(320);
            entity.Property(profile => profile.Phone).HasMaxLength(80);
            entity.Property(profile => profile.Location).HasMaxLength(200);
            entity.Property(profile => profile.LinkedInUrl).HasMaxLength(500);
            entity.Property(profile => profile.GitHubUrl).HasMaxLength(500);
            entity.Property(profile => profile.PortfolioUrl).HasMaxLength(500);
            entity.Property(profile => profile.DefaultLanguage).HasMaxLength(40);
            entity.Property(profile => profile.DanishTone).HasMaxLength(2000);
            entity.Property(profile => profile.EnglishTone).HasMaxLength(2000);
        });

        modelBuilder.Entity<ProfileFact>(entity =>
        {
            entity.ToTable("profile_facts");
            entity.HasKey(fact => fact.Id);

            entity.Property(fact => fact.Type).HasMaxLength(80);
            entity.Property(fact => fact.Title).HasMaxLength(200);
            entity.Property(fact => fact.Summary).HasMaxLength(4000);
            entity.Property(fact => fact.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(fact => fact.FactItems).HasColumnType("jsonb");
            entity.Property(fact => fact.Technologies).HasColumnType("jsonb");
            entity.Property(fact => fact.AllowedClaims).HasColumnType("jsonb");
            entity.Property(fact => fact.ForbiddenClaims).HasColumnType("jsonb");
            entity.Property(fact => fact.SourceDocumentIds).HasColumnType("jsonb");
            entity.Property(fact => fact.OriginalImportedSnapshot).HasColumnType("jsonb");
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("job_applications");
            entity.HasKey(application => application.Id);

            entity.Property(application => application.CompanyName).HasMaxLength(200);
            entity.Property(application => application.RoleTitle).HasMaxLength(200);
            entity.Property(application => application.ApplicationUrl).HasMaxLength(500);
            entity.Property(application => application.Status).HasMaxLength(80);
            entity.Property(application => application.DetectedLanguage).HasMaxLength(40);
            entity.Property(application => application.SelectedLanguage).HasMaxLength(40);
            entity.Property(application => application.JobSignals).HasColumnType("jsonb");
            entity.Property(application => application.EvidenceMatches).HasColumnType("jsonb");
            entity.Property(application => application.UnmatchedRequirements).HasColumnType("jsonb");
            entity.Property(application => application.ApprovedEvidence).HasColumnType("jsonb");
            entity.Property(application => application.CustomFacts).HasColumnType("jsonb");
            entity.Property(application => application.PreparationStatus).HasMaxLength(80);
        });

        modelBuilder.Entity<GeneratedDraft>(entity =>
        {
            entity.ToTable("generated_drafts");
            entity.HasKey(draft => draft.Id);

            entity.Property(draft => draft.CoverLetterText).HasColumnType("text");
            entity.Property(draft => draft.ShortMotivationText).HasColumnType("text");
            entity.Property(draft => draft.ClaimAudit).HasColumnType("jsonb");
            entity.Property(draft => draft.IsClaimAuditStale).HasDefaultValue(false);

            entity.HasOne(draft => draft.JobApplication)
                .WithOne(application => application.GeneratedDraft)
                .HasForeignKey<GeneratedDraft>(draft => draft.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiRun>(entity =>
        {
            entity.ToTable("ai_runs");
            entity.HasKey(run => run.Id);

            entity.Property(run => run.Step).HasMaxLength(120);
            entity.Property(run => run.Provider).HasMaxLength(80);
            entity.Property(run => run.Model).HasMaxLength(120);
            entity.Property(run => run.Status).HasMaxLength(80);
            entity.Property(run => run.ErrorCode).HasMaxLength(120);
            entity.Property(run => run.ErrorMessage).HasMaxLength(4000);
            entity.Property(run => run.InputSummary).HasColumnType("jsonb");
            entity.Property(run => run.OutputSummary).HasColumnType("jsonb");

            entity.HasOne(run => run.JobApplication)
                .WithMany(application => application.AiRuns)
                .HasForeignKey(run => run.JobApplicationId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
