using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Users.UnitTests.PhoneNumbers;

public sealed class PhoneNumberTests : BaseTest
{
    [Fact]
    public void Create_ShouldReturnPhoneNumber_OwnedByTheGivenUser()
    {
        string number = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        var userId = Guid.NewGuid();

        Result<PhoneNumber> result = PhoneNumber.Create(number, userId);

        result.IsSuccess.ShouldBeTrue();

        PhoneNumber phoneNumber = result.Value;

        phoneNumber.Number.ShouldBe(number);
        phoneNumber.UserId.ShouldBe(userId);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheOwningUser()
    {
        string number = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        var userId = Guid.NewGuid();

        PhoneNumber phoneNumber = PhoneNumber.Create(number, userId);

        string owningUserId = userId.ToString();

        phoneNumber.Id.ShouldNotBe(Guid.Empty);
        phoneNumber.Id.ShouldNotBe(userId);
        phoneNumber.CreatedBy.ShouldBe(owningUserId);
        phoneNumber.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldNotRaise_AnyDomainEvent()
    {
        string number = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        var userId = Guid.NewGuid();

        PhoneNumber phoneNumber = PhoneNumber.Create(number, userId);

        phoneNumber.DomainEvents.ShouldBeEmpty();
    }
}
