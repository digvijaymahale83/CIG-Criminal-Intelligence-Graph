namespace Domain.Common;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    string? UpdatedBy { get; set; }
}

public interface IInvestigationScopedEntity
{
    Guid InvestigationId { get; set; }
}
