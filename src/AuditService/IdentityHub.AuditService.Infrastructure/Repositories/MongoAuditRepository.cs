using IdentityHub.AuditService.Application.Abstractions;
using IdentityHub.AuditService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace IdentityHub.AuditService.Infrastructure.Repositories;

public sealed class MongoAuditRepository : IAuditRepository
{
    private readonly IMongoCollection<AuditEventDocument> _collection;

    public MongoAuditRepository(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDB:ConnectionString"]
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "identityhub_audit";
        var database = new MongoClient(connectionString).GetDatabase(databaseName);
        _collection = database.GetCollection<AuditEventDocument>("audit_events");
    }

    public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        _collection.InsertOneAsync(ToDocument(auditEvent), cancellationToken: cancellationToken);

    public async Task<AuditEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyCollection<AuditEvent>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var documents = await _collection.Find(FilterDefinition<AuditEventDocument>.Empty)
            .SortByDescending(item => item.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<AuditEvent>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var documents = await _collection.Find(item => item.UserId == userId)
            .SortByDescending(item => item.OccurredAt)
            .ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    public async Task<IReadOnlyCollection<AuditEvent>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var documents = await _collection.Find(item => item.EventType == eventType)
            .SortByDescending(item => item.OccurredAt)
            .ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    private static AuditEventDocument ToDocument(AuditEvent auditEvent) => new()
    {
        Id = auditEvent.Id,
        EventType = auditEvent.EventType,
        ServiceName = auditEvent.ServiceName,
        UserId = auditEvent.UserId,
        ActorId = auditEvent.ActorId,
        EntityId = auditEvent.EntityId,
        EntityType = auditEvent.EntityType,
        OccurredAt = auditEvent.OccurredAt,
        CorrelationId = auditEvent.CorrelationId,
        IpAddress = auditEvent.IpAddress,
        Description = auditEvent.Description,
        Metadata = ToBsonDocument(auditEvent.Metadata)
    };

    private static AuditEvent ToDomain(AuditEventDocument document) => new(
        document.EventType,
        document.ServiceName,
        document.UserId,
        document.ActorId,
        document.EntityId,
        document.EntityType,
        document.OccurredAt,
        document.CorrelationId,
        document.IpAddress,
        document.Description,
        FromBsonDocument(document.Metadata));

    private static BsonDocument? ToBsonDocument(Dictionary<string, object>? metadata)
    {
        if (metadata is null) return null;

        var document = new BsonDocument();
        foreach (var pair in metadata)
            document[pair.Key] = ToBsonValue(pair.Value);
        return document;
    }

    private static BsonValue ToBsonValue(object? value) => value switch
    {
        null => BsonNull.Value,
        string text => text,
        bool boolean => boolean,
        int integer => integer,
        long longValue => longValue,
        double doubleValue => doubleValue,
        decimal decimalValue => decimalValue,
        Guid guid => guid.ToString(),
        DateTime dateTime => dateTime,
        _ => value.ToString() ?? string.Empty
    };

    private static Dictionary<string, object>? FromBsonDocument(BsonDocument? document)
    {
        if (document is null) return null;
        return document.Elements.ToDictionary(element => element.Name, element => FromBsonValue(element.Value));
    }

    private static object FromBsonValue(BsonValue value) => value.BsonType switch
    {
        BsonType.Null => null!,
        BsonType.Boolean => value.AsBoolean,
        BsonType.Int32 => value.AsInt32,
        BsonType.Int64 => value.AsInt64,
        BsonType.Double => value.AsDouble,
        BsonType.DateTime => value.ToUniversalTime(),
        BsonType.Document => value.AsBsonDocument.ToString(),
        _ => value.ToString() ?? string.Empty
    };
}

internal sealed class AuditEventDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ActorId { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public BsonDocument? Metadata { get; set; }
}
