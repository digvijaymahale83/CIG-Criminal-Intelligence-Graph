namespace Domain.Enums;

public enum ServiceType
{
    PostgreSql,
    Neo4j,
    Redis,
    RabbitMq,
    AiService,
    ObjectStorage
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public enum SystemReadinessStatus
{
    Ready,
    Degraded,
    Initializing,
    Maintenance
}

public enum InvestigationStatus
{
    Active,
    Suspended,
    Closed,
    Archived
}

public enum EntityType
{
    Person,
    Organization,
    Location,
    Phone,
    Vehicle,
    BankAccount,
    DigitalAccount,
    Weapon,
    Document
}

public enum RelationshipType
{
    AssociateOf,
    FamilyOf,
    CommunicatedWith,
    TransactedWith,
    LocatedAt,
    Owns,
    EmployedBy,
    MemberOf,
    CoOccurredWith
}

public enum ExtractionStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
