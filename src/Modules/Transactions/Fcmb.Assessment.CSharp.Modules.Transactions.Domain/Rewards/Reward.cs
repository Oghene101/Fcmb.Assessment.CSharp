using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Rewards;

public sealed class Reward : Entity
{
    private const decimal CorporateTransferThreshold = 120_000m;
    private const int CorporateBasePoints = 3;
    private const int CorporateCashbackPointsThreshold = 90;
    private const decimal CorporateCashbackAmount = 7_500m;

    private const decimal IndividualTransferThreshold = 25_000m;
    private const int IndividualBasePoints = 2;
    private const int IndividualAirtimeTransferThreshold = 7;
    private const decimal IndividualAirtimeAmount = 1_500m;

    private Reward()
    {
    }

    public int Points { get; private set; }
    public int QualifyingTransfers { get; private set; }
    public DateTimeOffset? LastCashBackDate { get; private set; }
    public DateTimeOffset LastResetDate { get; private set; }
    public Guid CustomerId { get; private set; }

    public static Result<Reward> Create(Guid customerId)
    {
        var reward = new Reward
        {
            CustomerId = customerId,
            Points = 0,
            QualifyingTransfers = 0,
            LastResetDate = DateTimeOffset.UtcNow
        };

        reward.InitializeAudit(
            Guid.CreateVersion7(),
            customerId.ToString());

        return reward;
    }

    public Result<RewardProcessingResponse> ProcessTransfer(
        CustomerType customerType,
        decimal amount,
        DateTimeOffset customerRegistrationDate,
        DateTimeOffset transactionTime)
    {
        ResetIfNewMonth(transactionTime);

        if (!IsQualifyingTransfer(customerType, amount))
        {
            return RewardProcessingResponse.None();
        }

        QualifyingTransfers++;

        // Calculate tenure multiplier (Double points for first 4 qualifying transfers if tenure > 4 years)
        bool hasTenureBonus = IsTenureEligible(customerRegistrationDate, transactionTime) && QualifyingTransfers <= 4;
        int multiplier = hasTenureBonus ? 2 : 1;

        int basePoints = customerType == CustomerType.Corporate ? CorporateBasePoints : IndividualBasePoints;
        int pointsEarned = basePoints * multiplier;
        Points += pointsEarned;

        // Evaluate rewards
        if (customerType == CustomerType.Corporate)
        {
            bool alreadyClaimedThisMonth = LastCashBackDate.HasValue &&
                                           LastCashBackDate.Value.Year == transactionTime.Year &&
                                           LastCashBackDate.Value.Month == transactionTime.Month;

            if (Points >= CorporateCashbackPointsThreshold && !alreadyClaimedThisMonth)
            {
                LastCashBackDate = transactionTime;
                return RewardProcessingResponse.Cashback(CorporateCashbackAmount, pointsEarned);
            }
        }

        // Triggered on the next qualifying transfer after 7 transfers (i.e., the 8th)
        if (customerType == CustomerType.Individual &&
            QualifyingTransfers == IndividualAirtimeTransferThreshold + 1)
        {
            return RewardProcessingResponse.Airtime(IndividualAirtimeAmount, pointsEarned);
        }

        return RewardProcessingResponse.ForPoints(pointsEarned);
    }

    private void ResetIfNewMonth(DateTimeOffset transactionTime)
    {
        // Convert dates into an absolute month integer (e.g., March 2026 = 2026 * 12 + 3)
        int transactionPeriod = transactionTime.Year * 12 + transactionTime.Month;
        int lastResetPeriod = LastResetDate.Year * 12 + LastResetDate.Month;

        // Only reset if moving forward into a strictly newer month
        if (transactionPeriod > lastResetPeriod)
        {
            Points = 0;
            QualifyingTransfers = 0;
            LastResetDate = transactionTime;
        }
    }

    private static bool IsQualifyingTransfer(
        CustomerType customerType, decimal amount) => customerType switch
    {
        CustomerType.Corporate => amount > CorporateTransferThreshold,
        CustomerType.Individual => amount > IndividualTransferThreshold,
        _ => false
    };

    private static bool IsTenureEligible(
        DateTimeOffset registrationDate,
        DateTimeOffset transactionTime)
    {
        // Step 1: Get an initial guess of customer tenure in years
        int years = transactionTime.Year - registrationDate.Year;

        // Step 2: Check if the anniversary date has NOT occurred yet this year
        // Rewind the transaction date by our initial year guess to compare calendar dates
        if (registrationDate.Date > transactionTime.AddYears(-years).Date)
        {
            // Example: Registered Dec 15, 2020. Transaction on Jan 10, 2024.
            // 2024 - 2020 = 4 years, but Dec 15 hasn't happened yet in 2024.
            // So we reduce the tenure count to 3.
            years--;
        }

        // Step 3: Return true if the customer has been registered for 4 or more full years
        return years >= 4;
    }
}

public sealed record RewardProcessingResponse(
    int PointsEarned,
    decimal CashbackAmount,
    decimal AirtimeAmount,
    bool EarnedCashback,
    bool EarnedAirtime)
{
    public static RewardProcessingResponse None() => new(0, 0, 0, false, false);
    public static RewardProcessingResponse ForPoints(int points) => new(points, 0, 0, false, false);
    public static RewardProcessingResponse Cashback(decimal amount, int points) => new(points, amount, 0, true, false);
    public static RewardProcessingResponse Airtime(decimal amount, int points) => new(points, 0, amount, false, true);
}
