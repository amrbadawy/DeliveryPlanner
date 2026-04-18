using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Infrastructure.Data.Configurations;

public sealed class TaskNoteConfiguration : IEntityTypeConfiguration<TaskNote>
{
    public void Configure(EntityTypeBuilder<TaskNote> builder)
    {
        builder.ToTable("TaskNotes", "task");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.TaskId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(n => n.NoteText)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.Author)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(n => n.TaskId);
    }
}
