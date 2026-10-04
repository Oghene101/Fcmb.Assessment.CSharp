namespace Fcmb.Assessment.CSharp.Modules.Transactions.Application.Contracts.Dtos;

public static class Transactions
{
    public enum TransferType
    {
        IntraBank,
        InterBank,
    }

    public sealed record TransferRequest(
        TransferType TransferType,
        string SourceAccount,
        string DestinationAccount,
        decimal Amount);
}
