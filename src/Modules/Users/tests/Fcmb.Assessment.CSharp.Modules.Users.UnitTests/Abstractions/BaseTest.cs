using Bogus;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Abstractions;

public abstract class BaseTest
{
    protected const string PhoneNumberFormat = "+234##########";

    protected static readonly Faker Faker = new();

    protected static T AssertDomainEventWasPublished<T>(Entity entity)
        where T : IDomainEvent
    {
        T? domainEvent = entity.DomainEvents
            .OfType<T>()
            .SingleOrDefault();

        return domainEvent ?? throw new InvalidOperationException($"{typeof(T).Name} was not published");
    }
}
