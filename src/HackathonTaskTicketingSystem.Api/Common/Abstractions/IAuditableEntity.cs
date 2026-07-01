namespace HackathonTaskTicketingSystem.Common.Abstractions;

/// <summary>
/// Marks an entity whose created/modified timestamps are maintained automatically by
/// <c>AuditableEntitySaveChangesInterceptor</c>. Because EF only flags an entity as
/// Modified when a value actually changes, ModifiedAt is not advanced on no-op saves
/// or when a related entity (e.g. a comment) is added.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }

    DateTime ModifiedAt { get; set; }
}
