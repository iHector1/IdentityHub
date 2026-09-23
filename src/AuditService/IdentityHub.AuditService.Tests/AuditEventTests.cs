using IdentityHub.AuditService.Domain.Entities;

namespace IdentityHub.AuditService.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void CreateAuditEvent_WithValidData_ShouldPreserveBusinessData()
    {
        var occurredAt = new DateTime(2026, 9, 23, 12, 30, 0, DateTimeKind.Utc);
        var metadata = new Dictionary<string, object> { ["source"] = "test" };

        var auditEvent = new AuditEvent(
            "UserCreated",
            "UserService",
            userId: "user-1",
            entityId: "entity-1",
            occurredAt: occurredAt,
            metadata: metadata);

        Assert.False(string.IsNullOrWhiteSpace(auditEvent.Id));
        Assert.Equal("UserCreated", auditEvent.EventType);
        Assert.Equal("UserService", auditEvent.ServiceName);
        Assert.Equal("user-1", auditEvent.UserId);
        Assert.Equal("entity-1", auditEvent.EntityId);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
        Assert.Same(metadata, auditEvent.Metadata);
    }

    [Theory]
    [InlineData("", "UserService")]
    [InlineData("UserCreated", "")]
    public void CreateAuditEvent_WithRequiredValueMissing_ShouldThrowArgumentException(
        string eventType,
        string serviceName)
    {
        Assert.Throws<ArgumentException>(() => new AuditEvent(eventType, serviceName));
    }

    [Fact]
    public void CreateAuditEvent_WithoutOccurredAt_ShouldSetUtcTimestamp()
    {
        var before = DateTime.UtcNow;

        var auditEvent = new AuditEvent("UserCreated", "UserService");

        var after = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, auditEvent.OccurredAt.Kind);
        Assert.InRange(auditEvent.OccurredAt, before, after);
    }
}
