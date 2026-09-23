using IdentityHub.AuditService.Domain.Entities;

namespace IdentityHub.AuditService.Application.Abstractions;

public interface IAuditRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<AuditEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditEvent>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditEvent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditEvent>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
}
