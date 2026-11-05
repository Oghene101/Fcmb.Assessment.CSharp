using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Users.UnitTests.Users;

public sealed class UserTests : BaseTest
{
    private static readonly DateOnly AdultDob = new(1990, 5, 17);

    [Fact]
    public void Create_ShouldReturnUser_WithTheGivenProfile()
    {
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();
        string email = Faker.Internet.Email();
        string phoneNumber = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        string identityId = NewIdentityId();

        Result<User> result = User.Create(
            firstName,
            lastName,
            email,
            phoneNumber,
            AdultDob,
            UserType.Corporate,
            identityId);

        result.IsSuccess.ShouldBeTrue();

        User user = result.Value;

        user.FirstName.ShouldBe(firstName);
        user.LastName.ShouldBe(lastName);
        user.Dob.ShouldBe(AdultDob);
        user.UserType.ShouldBe(UserType.Corporate);
        user.IdentityId.ShouldBe(identityId);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheUserItself()
    {
        User user = CreateUser(UserType.Individual);

        string userId = user.Id.ToString();

        user.Id.ShouldNotBe(Guid.Empty);
        user.CreatedBy.ShouldBe(userId);
        user.LastUpdatedBy.ShouldBe(userId);
        user.LastUpdatedAt.ShouldBe(user.CreatedAt);
        user.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldGiveEveryUser_ADistinctId()
    {
        User firstUser = CreateUser(UserType.Individual);
        User secondUser = CreateUser(UserType.Individual);

        firstUser.Id.ShouldNotBe(secondUser.Id);
    }

    [Theory]
    [InlineData(UserType.Individual)]
    [InlineData(UserType.Corporate)]
    public void Create_ShouldRaise_UserSignedUpDomainEvent_CarryingTheSignUpDetails(UserType userType)
    {
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();
        string email = Faker.Internet.Email();
        string phoneNumber = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        string identityId = NewIdentityId();

        User user = User.Create(
            firstName,
            lastName,
            email,
            phoneNumber,
            AdultDob,
            userType,
            identityId);

        UserSignedUpDomainEvent domainEvent = AssertDomainEventWasPublished<UserSignedUpDomainEvent>(user);

        domainEvent.UserId.ShouldBe(user.Id);
        domainEvent.FirstName.ShouldBe(firstName);
        domainEvent.LastName.ShouldBe(lastName);
        domainEvent.Email.ShouldBe(email);
        domainEvent.PhoneNumber.ShouldBe(phoneNumber);
        domainEvent.Dob.ShouldBe(AdultDob);
        domainEvent.UserType.ShouldBe(userType);
    }

    [Fact]
    public void Create_ShouldRaiseTheEvent_WithItsOwnId_AtTheMomentTheUserWasCreated()
    {
        User user = CreateUser(UserType.Individual);

        UserSignedUpDomainEvent domainEvent = AssertDomainEventWasPublished<UserSignedUpDomainEvent>(user);

        domainEvent.Id.ShouldNotBe(Guid.Empty);
        domainEvent.Id.ShouldNotBe(user.Id);
        domainEvent.OccurredOn.ShouldBe(user.CreatedAt);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemove_TheSignedUpEvent()
    {
        User user = CreateUser(UserType.Individual);

        user.ClearDomainEvents();

        user.DomainEvents.ShouldBeEmpty();
    }

    private static User CreateUser(UserType userType)
    {
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();
        string email = Faker.Internet.Email();
        string phoneNumber = Faker.Phone.PhoneNumber(PhoneNumberFormat);
        string identityId = NewIdentityId();

        return User.Create(
            firstName,
            lastName,
            email,
            phoneNumber,
            AdultDob,
            userType,
            identityId);
    }

    private static string NewIdentityId()
    {
        var identityId = Guid.NewGuid();

        return identityId.ToString();
    }
}
