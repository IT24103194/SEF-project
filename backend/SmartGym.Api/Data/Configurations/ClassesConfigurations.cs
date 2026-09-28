using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGym.Api.Entities;

namespace SmartGym.Api.Data.Configurations;

public class ClassCategoryConfiguration : IEntityTypeConfiguration<ClassCategory>
{
    public void Configure(EntityTypeBuilder<ClassCategory> builder)
    {
        builder.ToTable("class_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(500);
    }
}

public class FitnessClassConfiguration : IEntityTypeConfiguration<FitnessClass>
{
    public void Configure(EntityTypeBuilder<FitnessClass> builder)
    {
        builder.ToTable("fitness_classes");
        builder.HasKey(fc => fc.Id);

        builder.Property(fc => fc.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(fc => fc.IntensityLevel)
            .HasMaxLength(50);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_fitness_classes_capacity", "\"DefaultCapacity\" > 0");
            t.HasCheckConstraint("CK_fitness_classes_duration", "\"DurationMinutes\" > 0");
        });

        builder.HasIndex(fc => fc.Name);

        builder.HasOne(fc => fc.Category)
            .WithMany(c => c.FitnessClasses)
            .HasForeignKey(fc => fc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(fc => fc.Schedules)
            .WithOne(s => s.FitnessClass)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClassScheduleConfiguration : IEntityTypeConfiguration<ClassSchedule>
{
    public void Configure(EntityTypeBuilder<ClassSchedule> builder)
    {
        builder.ToTable("class_schedules");
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.Room)
            .HasMaxLength(100);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_class_schedules_capacity", "\"Capacity\" > 0");
            t.HasCheckConstraint("CK_class_schedules_booked_count", "\"BookedCount\" >= 0");
        });

        builder.HasIndex(cs => new { cs.StartTime, cs.EndTime });
        builder.HasIndex(cs => cs.Status);
        builder.HasIndex(cs => cs.ClassId);
        builder.HasIndex(cs => cs.TrainerId);

        builder.HasOne(cs => cs.FitnessClass)
            .WithMany(fc => fc.Schedules)
            .HasForeignKey(cs => cs.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.Trainer)
            .WithMany(u => u.TrainedSchedules)
            .HasForeignKey(cs => cs.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(cs => cs.Bookings)
            .WithOne(b => b.Schedule)
            .HasForeignKey(b => b.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cs => cs.Attendances)
            .WithOne(a => a.Schedule)
            .HasForeignKey(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.HasIndex(b => new { b.ScheduleId, b.MemberId });
        builder.HasIndex(b => b.BookingTime);
        builder.HasIndex(b => b.Status);

        builder.HasOne(b => b.Schedule)
            .WithMany(cs => cs.Bookings)
            .HasForeignKey(b => b.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Member)
            .WithMany(m => m.Bookings)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Attendance)
            .WithOne(a => a.Booking)
            .HasForeignKey<Attendance>(a => a.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("attendances");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.BookingId)
            .IsUnique();

        builder.HasIndex(a => new { a.ScheduleId, a.MemberId });
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.CheckedInAt);

        builder.HasOne(a => a.Booking)
            .WithOne(b => b.Attendance)
            .HasForeignKey<Attendance>(a => a.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Schedule)
            .WithMany(cs => cs.Attendances)
            .HasForeignKey(a => a.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Member)
            .WithMany(m => m.Attendances)
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
