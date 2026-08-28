using System.Diagnostics.CodeAnalysis;
using Fcmb.Assessment.CSharp.Common.Application.Extensions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using FluentValidation;
using SerilogTimings;
using SignUpResponse =
    Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpResponse;


namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Users;

public static class SignUpUseCase
{
    public sealed record Command(
        string FirstName,
        string LastName,
        string PhoneNumber,
        DateOnly Dob,
        UserType UserType,
        string Email,
        string Password) : ICommand<SignUpResponse>;

    internal sealed class Handler(
        IKeyCloakClient keyCloak,
        IUnitOfWork uOw) : ICommandHandler<Command, SignUpResponse>
    {
        private static readonly string HandlerName = typeof(Handler).GetOuterAndInnerName();

        public async Task<Result<SignUpResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            using var op = Operation.Begin(
                "{HandlerName} with Name: {FirstName} {LastName} and Email: {Email}",
                HandlerName, request.FirstName, request.LastName, request.Email);

            KeyCloakCreateUserRequest createUser = request.ToRequest();

            HttpResponseMessage response = await keyCloak.CreateUserAsync(createUser);
            response.EnsureSuccessStatusCode();
            string identityId = ExtractIdentityIdFromLocationHeader(response);

            Result<User> user = User.Create(
                request.FirstName,
                request.LastName,
                request.Dob,
                request.UserType,
                identityId);

            await uOw.UsersWriteRepository.AddAsync(user, cancellationToken);

            op.Complete();
            return new SignUpResponse(user.Value.Id);
        }

        private static string ExtractIdentityIdFromLocationHeader(
            HttpResponseMessage httpResponseMessage)
        {
            const string usersSegmentName = "users/";

            string locationHeader = httpResponseMessage.Headers.Location?.PathAndQuery ??
                                    throw new InvalidOperationException("Location header is null");

            int userSegmentValueIndex = locationHeader.IndexOf(
                usersSegmentName,
                StringComparison.InvariantCultureIgnoreCase);

            string identityId = locationHeader.Substring(userSegmentValueIndex + usersSegmentName.Length);

            return identityId;
        }
    }

    [SuppressMessage("SonarLint", "S1144", Justification = "Picked up by reflection")]
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .MaximumLength(20)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("A valid international phone number is required.");

            RuleFor(x => x.Dob)
                .NotEmpty()
                .Must(dob => dob <= DateOnly.FromDateTime(DateTime.Today.AddYears(-18)))
                .WithMessage("User must be at least 18 years old.");

            RuleFor(x => x.UserType)
                .IsInEnum()
                .WithMessage("A valid user type must be specified.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long.");
        }
    }
}
