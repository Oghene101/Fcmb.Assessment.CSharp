using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using FluentValidation;
using FluentValidation.Results;

namespace Fcmb.Assessment.CSharp.Common.Application.Behaviors;

internal static class ValidationDecorator
{
    internal sealed class RequestHandler<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> innerHandler,
        IEnumerable<IValidator<TRequest>> validators)
        : IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(request, validators, cancellationToken);

            if (validationFailures.Length == 0)
            {
                return await innerHandler.Handle(request, cancellationToken);
            }

            throw new ValidationException(validationFailures);
        }
    }

    internal sealed class RequestHandler<TRequest>(
        IRequestHandler<TRequest> innerHandler,
        IEnumerable<IValidator<TRequest>> validators)
        : IRequestHandler<TRequest>
        where TRequest : IRequest
    {
        public async Task<Result> Handle(TRequest request, CancellationToken cancellationToken)
        {
            ValidationFailure[] validationFailures = await ValidateAsync(request, validators, cancellationToken);

            if (validationFailures.Length == 0)
            {
                return await innerHandler.Handle(request, cancellationToken);
            }

            throw new ValidationException(validationFailures);
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TCommand>(command);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        ValidationFailure[] validationFailures = validationResults
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .ToArray();

        return validationFailures;
    }
}
