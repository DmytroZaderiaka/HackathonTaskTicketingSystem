using HackathonTaskTicketingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HackathonTaskTicketingSystem.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body)
            .IsRequired();

        builder.HasOne(c => c.Ticket)
            .WithMany()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting a ticket deletes its comments.

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comments are listed oldest-first per ticket.
        builder.HasIndex(c => new { c.TicketId, c.CreatedAt });
    }
}
