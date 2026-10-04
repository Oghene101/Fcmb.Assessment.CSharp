using System.Diagnostics.CodeAnalysis;
using Fcmb.Assessment.CSharp.Common.Application.Extensions;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using FluentValidation;
using SerilogTimings;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Application.Transactions;

public static class TransferUseCase
{
    public sealed record Command(
        TransactionType TransactionType,
        string SourceAccount,
        string DestinationAccount,
        decimal Amount) : ICommand;

    internal sealed class Handler(
        IUnitOfWork uOw) : ICommandHandler<Command>
    {
        private static readonly string HandlerName = typeof(Handler).GetOuterAndInnerName();

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            using var op = Operation.Begin(
                "{HandlerName} with SourceAccount: {SourceAccount} and DestinationAccount: {DestinationAccount}",
                HandlerName, request.SourceAccount, request.DestinationAccount);

            Result<Transaction> tr = Transaction.Create(
                request.TransactionType,
                request.SourceAccount,
                request.Amount,
                default,
                default,
                Guid.Empty);

            await uOw.TransactionsWriteRepository.AddAsync(tr, cancellationToken);

            op.Complete();
            return Result.Success();
        }
    }

    [SuppressMessage("SonarLint", "S1144", Justification = "Picked up by reflection")]
    internal sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionType)
                .IsInEnum()
                .WithMessage("A valid transaction type must be specified.");

            RuleFor(x => x.SourceAccount)
                .NotEmpty()
                .Length(10)
                .Must(acc => acc.All(char.IsDigit))
                .WithMessage("Source account must be a valid 10-digit account number.");

            RuleFor(x => x.DestinationAccount)
                .NotEmpty()
                .Length(10)
                .Must(acc => acc.All(char.IsDigit))
                .WithMessage("Destination account must be a valid 10-digit account number.")
                .NotEqual(x => x.SourceAccount)
                .WithMessage("Destination account cannot be the same as the source account.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Transfer amount must be greater than zero.");
        }
    }
}
