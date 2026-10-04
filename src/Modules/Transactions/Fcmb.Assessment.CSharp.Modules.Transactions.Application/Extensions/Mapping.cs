using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Transactions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using TransferRequest =
    Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions.TransferRequest;
using TransferType =
    Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos.Transactions.TransferType;


namespace Fcmb.Assessment.CSharp.Modules.Transactions.Application.Extensions;

public static class MappingExtensions
{
    #region To Command

#pragma warning disable S3928
#pragma warning disable CA2208
    public static TransferUseCase.Command ToCommand(this TransferRequest dto)
    {
        TransactionType transactionType = dto.TransferType switch
        {
            TransferType.IntraBank => TransactionType.IntraBank,
            TransferType.InterBank => TransactionType.InterBank,
            _ => throw new ArgumentOutOfRangeException(nameof(dto.TransferType), dto.TransferType,
                "Unsupported transfer type.")
        };
        return new(transactionType, dto.SourceAccount, dto.DestinationAccount, dto.Amount);
    }
#pragma warning restore CA2208
#pragma warning restore S3928

    #endregion
}
