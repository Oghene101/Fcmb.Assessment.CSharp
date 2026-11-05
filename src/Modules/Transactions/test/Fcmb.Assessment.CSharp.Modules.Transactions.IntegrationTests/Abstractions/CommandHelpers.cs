using System.Net;
using System.Net.Http.Json;
using Bogus;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using static Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions;
using SignUpRequest = Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpRequest;
using SignUpUserType = Fcmb.Assessment.CSharp.Modules.Users.Domain.Users.UserType;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Abstractions;

internal static class CommandHelpers
{
    private const string PhoneNumberFormat = "+234##########";
    private const string StrongPasswordPrefix = "Aa1!";
    private const string AccountNumberFormat = "##########";

    internal static SignUpRequest NewSignUpRequest()
    {
        var faker = new Faker();

        string firstName = faker.Name.FirstName();
        string lastName = faker.Name.LastName();
        string phoneNumber = faker.Phone.PhoneNumber(PhoneNumberFormat);
        DateTime nineteenYearsAgo = DateTime.Today.AddYears(-19);
        var latestAdultDob = DateOnly.FromDateTime(nineteenYearsAgo);
        DateOnly dob = faker.Date.PastDateOnly(40, latestAdultDob);
        string uniqueSuffix = faker.Random.AlphaNumeric(8);
        string email = faker.Internet.Email(firstName, lastName, uniqueSuffix: uniqueSuffix);
        string password = faker.Internet.Password(12, prefix: StrongPasswordPrefix);

        return new SignUpRequest(firstName, lastName, phoneNumber, dob, SignUpUserType.Individual, email, password);
    }

    internal static async Task SignUpAsync(
        this HttpClient httpClient,
        SignUpRequest request,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await httpClient.PostAsJsonAsync("users/sign-up", request, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    internal static UserSignedUpIntegrationEvent NewUserSignedUpIntegrationEvent(UserType userType)
    {
        var faker = new Faker();

        var eventId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        string firstName = faker.Name.FirstName();
        string lastName = faker.Name.LastName();
        string uniqueSuffix = faker.Random.AlphaNumeric(8);
        string email = faker.Internet.Email(firstName, lastName, uniqueSuffix: uniqueSuffix);
        string phoneNumber = faker.Phone.PhoneNumber(PhoneNumberFormat);
        DateTime nineteenYearsAgo = DateTime.Today.AddYears(-19);
        var latestAdultDob = DateOnly.FromDateTime(nineteenYearsAgo);
        DateOnly dob = faker.Date.PastDateOnly(40, latestAdultDob);

        return new UserSignedUpIntegrationEvent(
            eventId,
            DateTimeOffset.UtcNow,
            userId,
            firstName,
            lastName,
            email,
            phoneNumber,
            dob,
            userType);
    }

    internal static TransferRequest NewTransferRequest(TransferType transferType = TransferType.IntraBank)
    {
        var faker = new Faker();

        string sourceAccount = faker.Random.ReplaceNumbers(AccountNumberFormat);
        string destinationAccount = faker.Random.ReplaceNumbers(AccountNumberFormat);
        decimal amount = faker.Finance.Amount(1_000m, 500_000m);

        return new TransferRequest(transferType, sourceAccount, destinationAccount, amount);
    }
}
