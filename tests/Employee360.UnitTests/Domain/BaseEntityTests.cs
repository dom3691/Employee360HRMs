using Employee360.Domain.Common;
using Employee360.Domain.Events;
using FluentAssertions;

namespace Employee360.UnitTests.Domain;

/// <summary>Tests for <see cref="BaseEntity"/> identity and domain-event collection.</summary>
public class BaseEntityTests
{
    private sealed class TestEntity : BaseEntity;

    private sealed record TestEvent : DomainEvent;

    [Fact]
    public void NewEntity_ShouldHaveNonEmptyId()
    {
        var entity = new TestEntity();

        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AddDomainEvent_ShouldQueueEvent()
    {
        var entity = new TestEntity();
        var domainEvent = new TestEvent();

        entity.AddDomainEvent(domainEvent);

        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_ShouldDequeueEvent()
    {
        var entity = new TestEntity();
        var domainEvent = new TestEvent();
        entity.AddDomainEvent(domainEvent);

        entity.RemoveDomainEvent(domainEvent);

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        var entity = new TestEntity();
        entity.AddDomainEvent(new TestEvent());
        entity.AddDomainEvent(new TestEvent());

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvent_ShouldStampOccurredOnUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var domainEvent = new TestEvent();

        domainEvent.OccurredOnUtc.Should().BeOnOrAfter(before)
            .And.BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
