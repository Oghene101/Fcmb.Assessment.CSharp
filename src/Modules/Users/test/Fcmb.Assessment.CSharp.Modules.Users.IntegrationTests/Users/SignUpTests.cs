using System.Net;
using System.Net.Http.Json;
using Fcmb.Assessment.CSharp.Common.Application.Exceptions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using static Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users;
using static Fcmb.Assessment.CSharp.Modules.Users.Application.Users.SignUpUseCase;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Users;

public sealed class SignUpTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string SignUpRoute = "users/sign-up";

    public static readonly TheoryData<string, string> InvalidFieldValues = new()
    {
        { nameof(SignUpRequest.FirstName), "" },
        { nameof(SignUpRequest.FirstName), new string('a', 51) },
        { nameof(SignUpRequest.LastName), "" },
        { nameof(SignUpRequest.LastName), new string('a', 51) },
        { nameof(SignUpRequest.PhoneNumber), "" },
        { nameof(SignUpRequest.PhoneNumber), "08012345678" },
        { nameof(SignUpRequest.PhoneNumber), "+234-801-234" },
        { nameof(SignUpRequest.Email), "" },
        { nameof(SignUpRequest.Email), "not-an-email" },
        { nameof(SignUpRequest.Password), "" },
        { nameof(SignUpRequest.Password), "Aa1!aaa" },
        { nameof(SignUpRequest.Password), "aa1!aaaa" },
        { nameof(SignUpRequest.Password), "AA1!AAAA" },
        { nameof(SignUpRequest.Password), "Aa!aaaaa" },
        { nameof(SignUpRequest.Password), "Aa1aaaaa" }
    };

    [Theory]
    [MemberData(nameof(InvalidFieldValues))]
    public async Task Should_ReturnBadRequest_WhenAFieldIsInvalid(
        string invalidField,
        string invalidValue)
    {
        SignUpRequest validRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest request = WithInvalidField(validRequest, invalidField, invalidValue);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(SignUpRoute, request, CancellationToken);

        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey(invalidField);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTheUserIsYoungerThanEighteen()
    {
        DateTime eighteenYearsAgo = DateTime.Today.AddYears(-18);
        var eighteenYearsAgoDob = DateOnly.FromDateTime(eighteenYearsAgo);
        DateOnly underageDob = eighteenYearsAgoDob.AddDays(1);
        SignUpRequest validRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest request = validRequest with { Dob = underageDob };

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(SignUpRoute, request, CancellationToken);

        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey(nameof(SignUpRequest.Dob));
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTheUserTypeIsUnknown()
    {
        SignUpRequest validRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest request = validRequest with { UserType = (UserType)7 };

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(SignUpRoute, request, CancellationToken);

        HttpValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey(nameof(SignUpRequest.UserType));
    }

    [Fact]
    public async Task Should_ReturnCreated_WithTheLocationOfTheNewUser_WhenTheRequestIsValid()
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(SignUpRoute, request, CancellationToken);

        ApiEnvelope<SignUpResponse>? signedUp =
            await response.Content.ReadFromJsonAsync<ApiEnvelope<SignUpResponse>>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        signedUp.ShouldNotBeNull();
        signedUp.IsSuccess.ShouldBeTrue();
        signedUp.Data.UserId.ShouldNotBe(Guid.Empty);

        Uri? location = response.Headers.Location;

        location.ShouldNotBeNull();
        location.AbsolutePath.ShouldBe($"/api/v1/users/{signedUp.Data.UserId}");
    }

    [Theory]
    [InlineData(UserType.Individual)]
    [InlineData(UserType.Corporate)]
    public async Task Should_PersistTheUser_WithItsEmailAndPhoneNumber(UserType userType)
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest(userType);

        Guid userId = await HttpClient.SignUpAsync(request, CancellationToken);

        User? user = await DbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(storedUser => storedUser.Id == userId, CancellationToken);

        Email? email = await DbContext.Emails
            .AsNoTracking()
            .SingleOrDefaultAsync(storedEmail => storedEmail.UserId == userId, CancellationToken);

        PhoneNumber? phoneNumber = await DbContext.PhoneNumbers
            .AsNoTracking()
            .SingleOrDefaultAsync(storedPhoneNumber => storedPhoneNumber.UserId == userId, CancellationToken);

        user.ShouldNotBeNull();
        user.FirstName.ShouldBe(request.FirstName);
        user.LastName.ShouldBe(request.LastName);
        user.Dob.ShouldBe(request.Dob);
        user.UserType.ShouldBe(userType);
        email.ShouldNotBeNull();
        email.Address.ShouldBe(request.Email);
        phoneNumber.ShouldNotBeNull();
        phoneNumber.Number.ShouldBe(request.PhoneNumber);
    }

    [Fact]
    public async Task Should_LinkTheUser_ToTheIdentityAccountItSignsInWith()
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();

        Guid userId = await HttpClient.SignUpAsync(request, CancellationToken);

        string accessToken = await GetAccessTokenAsync(request.Email, request.Password);

        User? user = await DbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(storedUser => storedUser.Id == userId, CancellationToken);

        var signedInIdentity = new JsonWebToken(accessToken);

        user.ShouldNotBeNull();
        user.IdentityId.ShouldBe(signedInIdentity.Subject);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenTheEmailIsAlreadyRegistered()
    {
        SignUpRequest firstRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest otherRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest sameEmailRequest = otherRequest with { Email = firstRequest.Email };

        await HttpClient.SignUpAsync(firstRequest, CancellationToken);

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(SignUpRoute, sameEmailRequest, CancellationToken);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.ShouldNotBeNull();
        problem.ErrorCode().ShouldBe(EmailErrors.AlreadyExists.Code);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenThePhoneNumberIsAlreadyRegistered()
    {
        SignUpRequest firstRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest otherRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest samePhoneNumberRequest = otherRequest with { PhoneNumber = firstRequest.PhoneNumber };

        await HttpClient.SignUpAsync(firstRequest, CancellationToken);

        using HttpResponseMessage response =
            await HttpClient.PostAsJsonAsync(SignUpRoute, samePhoneNumberRequest, CancellationToken);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.ShouldNotBeNull();
        problem.ErrorCode().ShouldBe(PhoneNumberErrors.AlreadyExists.Code);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenTheIdentityProviderAlreadyHoldsTheEmail()
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();

        Guid userId = await HttpClient.SignUpAsync(request, CancellationToken);

        await ForgetApplicationUserAsync(userId);

        using HttpResponseMessage response = await HttpClient.PostAsJsonAsync(SignUpRoute, request, CancellationToken);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.ShouldNotBeNull();
        problem.ErrorCode().ShouldBe("Identity.EmailAlreadyExists");
    }

    [Fact]
    public async Task Should_ReturnTheUserId_WhenTheSignUpCommandIsHandled()
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();
        var command = request.ToCommand();
        IRequestHandler<Command, SignUpResponse> handler =
            ServiceProvider.GetRequiredService<IRequestHandler<Command, SignUpResponse>>();

        Result<SignUpResponse> result = await handler.Handle(command, CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        bool userExists = await DbContext.Users.AnyAsync(user => user.Id == result.Value.UserId, CancellationToken);

        userExists.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_RejectTheSignUpCommand_BeforeCreatingAnyIdentity_WhenItIsInvalid()
    {
        SignUpRequest validRequest = CommandHelpers.NewSignUpRequest();
        SignUpRequest request = validRequest with { Email = "not-an-email" };
        var command = request.ToCommand();
        IRequestHandler<Command, SignUpResponse> handler =
            ServiceProvider.GetRequiredService<IRequestHandler<Command, SignUpResponse>>();

        ValidationException exception =
            await Should.ThrowAsync<ValidationException>(() => handler.Handle(command, CancellationToken));

        exception.Errors.ShouldContainKey(nameof(Command.Email));

        bool phoneNumberTaken = await DbContext.PhoneNumbers
            .AnyAsync(phoneNumber => phoneNumber.Number == request.PhoneNumber, CancellationToken);

        phoneNumberTaken.ShouldBeFalse();
    }

    private async Task ForgetApplicationUserAsync(Guid userId)
    {
        await DbContext.Emails
            .Where(email => email.UserId == userId)
            .ExecuteDeleteAsync(CancellationToken);

        await DbContext.PhoneNumbers
            .Where(phoneNumber => phoneNumber.UserId == userId)
            .ExecuteDeleteAsync(CancellationToken);

        await DbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteDeleteAsync(CancellationToken);
    }

    private static SignUpRequest WithInvalidField(
        SignUpRequest validRequest,
        string invalidField,
        string invalidValue) =>
        invalidField switch
        {
            nameof(SignUpRequest.FirstName) => validRequest with { FirstName = invalidValue },
            nameof(SignUpRequest.LastName) => validRequest with { LastName = invalidValue },
            nameof(SignUpRequest.PhoneNumber) => validRequest with { PhoneNumber = invalidValue },
            nameof(SignUpRequest.Email) => validRequest with { Email = invalidValue },
            nameof(SignUpRequest.Password) => validRequest with { Password = invalidValue },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidField), invalidField, "Not a sign-up field.")
        };
}
