using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Emails;

public sealed class EmailTests : BaseTest
{
    [Fact]
    public void Create_ShouldReturnEmail_OwnedByTheGivenUser()
    {
        string address = Faker.Internet.Email();
        var userId = Guid.NewGuid();

        Result<Email> result = Email.Create(address, userId);

        result.IsSuccess.ShouldBeTrue();

        Email email = result.Value;

        email.Address.ShouldBe(address);
        email.UserId.ShouldBe(userId);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheOwningUser()
    {
        string address = Faker.Internet.Email();
        var userId = Guid.NewGuid();

        Email email = Email.Create(address, userId);

        string owningUserId = userId.ToString();

        email.Id.ShouldNotBe(Guid.Empty);
        email.Id.ShouldNotBe(userId);
        email.CreatedBy.ShouldBe(owningUserId);
        email.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldNotRaise_AnyDomainEvent()
    {
        string address = Faker.Internet.Email();
        var userId = Guid.NewGuid();

        Email email = Email.Create(address, userId);

        email.DomainEvents.ShouldBeEmpty();
    }
}
