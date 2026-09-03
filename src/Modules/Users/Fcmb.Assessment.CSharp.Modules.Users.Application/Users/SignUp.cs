using System.Diagnostics.CodeAnalysis;
using System.Net;
using Fcmb.Assessment.CSharp.Common.Application.Exceptions;
using Fcmb.Assessment.CSharp.Common.Application.Extensions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Extensions;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
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

            await ValidateRequestAsync(request);

            string identityId = await CreateIdentityUserAsync(request);

            Guid userId = await CreateApplicationUserAsync(request, identityId, cancellationToken);

            op.Complete();
            return new SignUpResponse(userId);
        }

        private async Task ValidateRequestAsync(Command request)
        {
            if (await uOw.EmailsReadRepository.EmailExistsAsync(request.Email))
            {
                throw ApiException.Conflict(EmailErrors.AlreadyExists);
            }

            if (await uOw.PhoneNumbersReadRepository.PhoneNumberExistsAsync(request.PhoneNumber))
            {
                throw ApiException.Conflict(PhoneNumberErrors.AlreadyExists);
            }
        }

        private async Task<string> CreateIdentityUserAsync(Command request)
        {
            KeyCloakCreateUserRequest createUser = request.ToRequest();

            HttpResponseMessage response = await keyCloak.CreateUserAsync(createUser);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw ApiException.Conflict(KeyCloakErrors.EmailAlreadyExists);
            }

            response.EnsureSuccessStatusCode();
            return ExtractIdentityIdFromLocationHeader(response);
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

        private async Task<Guid> CreateApplicationUserAsync(
            Command request,
            string identityId,
            CancellationToken cancellationToken)
        {
            User user = User.Create(
                request.FirstName,
                request.LastName,
                request.Email,
                request.PhoneNumber,
                request.Dob,
                request.UserType,
                identityId);

            Email email = Email.Create(request.Email, user.Id);
            PhoneNumber phoneNumber = PhoneNumber.Create(request.PhoneNumber, user.Id);

            await uOw.UsersWriteRepository.AddAsync(user, cancellationToken);
            await uOw.EmailsWriteRepository.AddAsync(email, cancellationToken);
            await uOw.PhoneNumbersWriteRepository.AddAsync(phoneNumber, cancellationToken);

            await uOw.SaveChangesAsync(cancellationToken);
            return user.Id;
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
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
        }
    }
}
