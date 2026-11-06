using BuildingBlocks.Domain;
using FluentValidation;
using MassTransit;
using MediatR;

namespace Architecture.Tests;

public class ConventionTests
{
    // ---- MediatR request handlers ------------------------------------------------------

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Request_handlers_are_named_Handler_and_are_not_public(string module)
    {
        var handlers = Modules.Types(module).That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Or().ImplementInterface(typeof(IRequestHandler<>));
        RuleAssert.NotEmpty(handlers, $"request handlers in {module}");

        RuleAssert.Passes(
            handlers.Should().HaveNameEndingWith("Handler").And().NotBePublic(),
            "Request handlers are resolved by MediatR only; keep them internal and suffixed 'Handler'.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Request_handlers_live_in_Features(string module)
    {
        RuleAssert.Passes(
            Modules.Types(module).That()
                .ImplementInterface(typeof(IRequestHandler<,>))
                .Or().ImplementInterface(typeof(IRequestHandler<>))
                .Should().ResideInNamespace($"{module}.Features"),
            "Request handlers belong to a feature slice.");
    }

    // ---- Validators --------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Validators_are_named_Validator_sealed_and_not_public(string module)
    {
        var validators = Modules.Types(module).That().Inherit(typeof(AbstractValidator<>));
        RuleAssert.NotEmpty(validators, $"validators in {module}");

        RuleAssert.Passes(
            validators.Should().HaveNameEndingWith("Validator").And().BeSealed().And().NotBePublic(),
            "Validators are discovered by assembly scanning; keep them internal sealed and suffixed 'Validator'.");
    }

    // ---- Domain ------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Entities_inherit_BaseEntity(string module)
    {
        var entities = Modules.Types(module).That()
            .ResideInNamespace($"{module}.Domain.Entities")
            .And().AreClasses();
        RuleAssert.NotEmpty(entities, $"entities in {module}.Domain.Entities");

        RuleAssert.Passes(
            entities.Should().Inherit(typeof(BaseEntity)),
            "Entities must inherit BaseEntity so they get an Id, timestamps and domain events.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_events_implement_IDomainEvent_are_sealed_and_named_DomainEvent(string module)
    {
        var events = Modules.Types(module).That().ResideInNamespace($"{module}.Domain.Events");

        // Not every module raises domain events, so an empty selection is fine here.
        RuleAssert.Passes(
            events.Should().ImplementInterface(typeof(IDomainEvent))
                .And().BeSealed()
                .And().HaveNameEndingWith("DomainEvent"),
            "Domain events must implement IDomainEvent (so MediatR dispatches them), be sealed, and end with 'DomainEvent'.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_events_live_in_Domain_Events(string module)
    {
        RuleAssert.Passes(
            Modules.Types(module).That().ImplementInterface(typeof(IDomainEvent))
                .Should().ResideInNamespace($"{module}.Domain.Events"),
            "Domain events belong in Domain.Events.");
    }

    // ---- Messaging ---------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Domain_event_handlers_are_named_Handler(string module)
    {
        RuleAssert.Passes(
            Modules.Types(module).That().ImplementInterface(typeof(INotificationHandler<>))
                .Should().HaveNameEndingWith("Handler"),
            "Notification handlers must be suffixed 'Handler'.");
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Integration_event_consumers_are_named_Consumer_and_live_in_Features(string module)
    {
        RuleAssert.Passes(
            Modules.Types(module).That().ImplementInterface(typeof(IConsumer<>))
                .Should().HaveNameEndingWith("Consumer")
                .And().ResideInNamespace($"{module}.Features"),
            "MassTransit consumers must be suffixed 'Consumer' and belong to a feature slice.");
    }

    // ---- Endpoints ---------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Endpoint_classes_are_static_and_live_in_Features(string module)
    {
        var endpoints = Modules.Types(module).That().HaveNameEndingWith("Endpoints");
        RuleAssert.NotEmpty(endpoints, $"endpoint classes in {module}");

        RuleAssert.Passes(
            endpoints.Should().BeStatic().And().ResideInNamespace($"{module}.Features"),
            "Endpoint classes are static Map* extension holders inside Features.");
    }
}
