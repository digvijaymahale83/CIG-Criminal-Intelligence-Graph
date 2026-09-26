using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Tests.Unit;

public class TestEntity : Entity<Guid>, IAuditableEntity, IInvestigationScopedEntity
{
    public TestEntity(Guid id, Guid investigationId, string createdBy)
    {
        Id = id;
        InvestigationId = investigationId;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid InvestigationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}

public class DomainEntitiesTests
{
    [Fact]
    public void Entities_WithSameId_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id, Guid.NewGuid(), "investigator-1");
        var entity2 = new TestEntity(id, Guid.NewGuid(), "investigator-2");

        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_ShouldNotBeEqual()
    {
        var entity1 = new TestEntity(Guid.NewGuid(), Guid.NewGuid(), "investigator-1");
        var entity2 = new TestEntity(Guid.NewGuid(), Guid.NewGuid(), "investigator-2");

        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void EntityNotFoundException_ShouldContainEntityNameAndKey()
    {
        var id = Guid.NewGuid();
        var ex = new EntityNotFoundException("PersonOfInterest", id);

        ex.Message.Should().Contain("PersonOfInterest");
        ex.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void SystemEnums_ShouldContainExpectedServiceTypes()
    {
        Enum.GetNames<ServiceType>().Should().Contain(new[]
        {
            nameof(ServiceType.PostgreSql),
            nameof(ServiceType.Neo4j),
            nameof(ServiceType.Redis),
            nameof(ServiceType.RabbitMq),
            nameof(ServiceType.AiService)
        });
    }
}
