using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data.Configurations;

public class AIWorkflowConfiguration : IEntityTypeConfiguration<AIWorkflow>
{
    public void Configure(EntityTypeBuilder<AIWorkflow> builder)
    {
        builder.ToTable("ai_workflows");
        builder.HasKey(ai => ai.Id);

        builder.HasIndex(ai => ai.IssueId)
            .IsUnique();

        builder.Property(ai => ai.WorkflowType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ai => ai.CurrentStep)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ai => ai.ModelIdentifier)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ai => ai.DiagnosisSummary)
            .HasMaxLength(3000);

        builder.Property(ai => ai.RecommendedAction)
            .HasMaxLength(3000);

        // Structured JSON payload column
        builder.Property(ai => ai.StructuredOutputPayloadJson)
            .HasColumnType("jsonb");

        builder.HasIndex(ai => ai.Status);
        builder.HasIndex(ai => ai.WorkflowType);
        builder.HasIndex(ai => ai.CreatedAt);

        builder.HasOne(ai => ai.FacilityIssue)
            .WithOne(fi => fi.AIWorkflow)
            .HasForeignKey<AIWorkflow>(ai => ai.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ai => ai.Steps)
            .WithOne(s => s.AIWorkflow)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ai => ai.ValidationResults)
            .WithOne(v => v.AIWorkflow)
            .HasForeignKey(v => v.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AIWorkflowStepConfiguration : IEntityTypeConfiguration<AIWorkflowStep>
{
    public void Configure(EntityTypeBuilder<AIWorkflowStep> builder)
    {
        builder.ToTable("ai_workflow_steps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Summary)
            .HasMaxLength(2000);

        builder.HasIndex(s => new { s.WorkflowId, s.StepOrder });
        builder.HasIndex(s => s.ExecutedAt);

        builder.HasOne(s => s.AIWorkflow)
            .WithMany(w => w.Steps)
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.ToolExecutions)
            .WithOne(t => t.WorkflowStep)
            .HasForeignKey(t => t.WorkflowStepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AIToolExecutionConfiguration : IEntityTypeConfiguration<AIToolExecution>
{
    public void Configure(EntityTypeBuilder<AIToolExecution> builder)
    {
        builder.ToTable("ai_tool_executions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ToolName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.InputParametersJson)
            .HasColumnType("jsonb");

        builder.Property(t => t.OutputResultJson)
            .HasColumnType("jsonb");

        builder.HasIndex(t => t.WorkflowStepId);
        builder.HasIndex(t => t.ToolName);
        builder.HasIndex(t => t.ExecutedAt);

        builder.HasOne(t => t.WorkflowStep)
            .WithMany(s => s.ToolExecutions)
            .HasForeignKey(t => t.WorkflowStepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AIValidationResultConfiguration : IEntityTypeConfiguration<AIValidationResult>
{
    public void Configure(EntityTypeBuilder<AIValidationResult> builder)
    {
        builder.ToTable("ai_validation_results");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.RuleName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(v => v.ValidationMessage)
            .HasMaxLength(1000);

        builder.HasIndex(v => v.WorkflowId);
        builder.HasIndex(v => v.Passed);

        builder.HasOne(v => v.AIWorkflow)
            .WithMany(w => w.ValidationResults)
            .HasForeignKey(v => v.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
