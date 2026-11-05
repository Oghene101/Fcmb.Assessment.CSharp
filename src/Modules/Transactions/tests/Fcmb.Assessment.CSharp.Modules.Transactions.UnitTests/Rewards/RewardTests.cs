using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Rewards;
using Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Rewards;

public sealed class RewardTests : BaseTest
{
    private const decimal IndividualThreshold = 25_000m;
    private const decimal CorporateThreshold = 120_000m;
    private const decimal IndividualQualifyingAmount = 25_000.01m;
    private const decimal CorporateQualifyingAmount = 120_000.01m;
    private const decimal AirtimeAmount = 1_500m;
    private const decimal CashbackAmount = 7_500m;

    private static readonly DateTimeOffset TransferTime = new(2026, 1, 10, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RecentRegistration = TransferTime.AddYears(-1);
    private static readonly DateTimeOffset LongTenureRegistration = TransferTime.AddYears(-5);

    public static readonly TheoryData<decimal> NonQualifyingIndividualAmounts = [1_000m, IndividualThreshold];

    public static readonly TheoryData<decimal> NonQualifyingCorporateAmounts =
        [IndividualQualifyingAmount, CorporateThreshold];

    [Fact]
    public void Create_ShouldStartTheReward_WithNoPointsAndNoQualifyingTransfers()
    {
        var customerId = Guid.NewGuid();

        Result<Reward> result = Reward.Create(customerId);

        result.IsSuccess.ShouldBeTrue();

        Reward reward = result.Value;

        reward.CustomerId.ShouldBe(customerId);
        reward.Points.ShouldBe(0);
        reward.QualifyingTransfers.ShouldBe(0);
        reward.LastCashBackDate.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheCustomer()
    {
        var customerId = Guid.NewGuid();

        Reward reward = Reward.Create(customerId);

        string owningCustomerId = customerId.ToString();

        reward.Id.ShouldNotBe(customerId);
        reward.CreatedBy.ShouldBe(owningCustomerId);
    }

    [Theory]
    [MemberData(nameof(NonQualifyingIndividualAmounts))]
    public void ProcessTransfer_ShouldEarnNothing_WhenAnIndividualTransferIsNotAboveTheThreshold(decimal amount)
    {
        Reward reward = NewReward();

        RewardProcessingResponse response = Process(reward, CustomerType.Individual, amount, RecentRegistration);

        var nothingEarned = RewardProcessingResponse.None();

        response.ShouldBe(nothingEarned);
        reward.Points.ShouldBe(0);
        reward.QualifyingTransfers.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(NonQualifyingCorporateAmounts))]
    public void ProcessTransfer_ShouldEarnNothing_WhenACorporateTransferIsNotAboveTheThreshold(decimal amount)
    {
        Reward reward = NewReward();

        RewardProcessingResponse response = Process(reward, CustomerType.Corporate, amount, RecentRegistration);

        var nothingEarned = RewardProcessingResponse.None();

        response.ShouldBe(nothingEarned);
        reward.Points.ShouldBe(0);
        reward.QualifyingTransfers.ShouldBe(0);
    }

    [Fact]
    public void ProcessTransfer_ShouldEarnNothing_WhenTheCustomerTypeIsUnknown()
    {
        Reward reward = NewReward();
        const CustomerType unknownCustomerType = (CustomerType)99;

        RewardProcessingResponse response = Process(reward, unknownCustomerType, 1_000_000m, RecentRegistration);

        var nothingEarned = RewardProcessingResponse.None();

        response.ShouldBe(nothingEarned);
        reward.QualifyingTransfers.ShouldBe(0);
    }

    [Fact]
    public void ProcessTransfer_ShouldEarnTwoPoints_WhenAnIndividualTransferIsAboveTheThreshold()
    {
        Reward reward = NewReward();

        RewardProcessingResponse response =
            Process(reward, CustomerType.Individual, IndividualQualifyingAmount, RecentRegistration);

        var twoPoints = RewardProcessingResponse.ForPoints(2);

        response.ShouldBe(twoPoints);
        reward.Points.ShouldBe(2);
        reward.QualifyingTransfers.ShouldBe(1);
    }

    [Fact]
    public void ProcessTransfer_ShouldEarnThreePoints_WhenACorporateTransferIsAboveTheThreshold()
    {
        Reward reward = NewReward();

        RewardProcessingResponse response =
            Process(reward, CustomerType.Corporate, CorporateQualifyingAmount, RecentRegistration);

        var threePoints = RewardProcessingResponse.ForPoints(3);

        response.ShouldBe(threePoints);
        reward.Points.ShouldBe(3);
        reward.QualifyingTransfers.ShouldBe(1);
    }

    [Fact]
    public void ProcessTransfer_ShouldNotCountTowardsRewards_ATransferThatDoesNotQualify()
    {
        Reward reward = NewReward();

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 2, RecentRegistration, TransferTime);
        Process(reward, CustomerType.Individual, IndividualThreshold, RecentRegistration);

        reward.Points.ShouldBe(4);
        reward.QualifyingTransfers.ShouldBe(2);
    }

    [Fact]
    public void ProcessTransfer_ShouldNotAwardAirtime_BeforeTheEighthQualifyingIndividualTransfer()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Individual, 7, RecentRegistration, TransferTime);

        responses.ShouldAllBe(response => !response.EarnedAirtime);
        reward.Points.ShouldBe(14);
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardAirtime_OnTheEighthQualifyingIndividualTransfer()
    {
        Reward reward = NewReward();

        RewardProcessingResponse eighthResponse =
            ProcessQualifyingTransfers(reward, CustomerType.Individual, 8, RecentRegistration, TransferTime);

        var airtime = RewardProcessingResponse.Airtime(AirtimeAmount, 2);

        eighthResponse.ShouldBe(airtime);
        reward.Points.ShouldBe(16);
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardAirtime_OnlyOnceInAMonth()
    {
        Reward reward = NewReward();

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 8, RecentRegistration, TransferTime);

        List<RewardProcessingResponse> laterResponses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Individual, 10, RecentRegistration, TransferTime);

        laterResponses.ShouldAllBe(response => !response.EarnedAirtime && response.PointsEarned == 2);
    }

    [Fact]
    public void ProcessTransfer_ShouldNeverAwardAirtime_ToACorporate()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Corporate, 9, RecentRegistration, TransferTime);

        responses.ShouldAllBe(response => !response.EarnedAirtime);
    }

    [Fact]
    public void ProcessTransfer_ShouldNotAwardCashback_BeforeACorporateReachesNinetyPoints()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Corporate, 29, RecentRegistration, TransferTime);

        responses.ShouldAllBe(response => !response.EarnedCashback);
        reward.Points.ShouldBe(87);
        reward.LastCashBackDate.ShouldBeNull();
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardCashback_WhenACorporateReachesNinetyPoints()
    {
        Reward reward = NewReward();

        RewardProcessingResponse thirtiethResponse =
            ProcessQualifyingTransfers(reward, CustomerType.Corporate, 30, RecentRegistration, TransferTime);

        var cashback = RewardProcessingResponse.Cashback(CashbackAmount, 3);

        thirtiethResponse.ShouldBe(cashback);
        reward.Points.ShouldBe(90);
        reward.LastCashBackDate.ShouldBe(TransferTime);
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardCashback_OnlyOnceInAMonth()
    {
        Reward reward = NewReward();

        ProcessQualifyingTransfers(reward, CustomerType.Corporate, 30, RecentRegistration, TransferTime);

        List<RewardProcessingResponse> laterResponses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Corporate, 10, RecentRegistration, TransferTime);

        laterResponses.ShouldAllBe(response => !response.EarnedCashback && response.PointsEarned == 3);
        reward.Points.ShouldBe(120);
    }

    [Fact]
    public void ProcessTransfer_ShouldNeverAwardCashback_ToAnIndividual()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Individual, 50, RecentRegistration, TransferTime);

        responses.ShouldAllBe(response => !response.EarnedCashback);
        reward.Points.ShouldBe(100);
    }

    [Fact]
    public void ProcessTransfer_ShouldDoubleThePoints_ForTheFirstFourQualifyingTransfers_WhenTenureIsOverFourYears()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Individual, 6, LongTenureRegistration, TransferTime);

        int[] pointsEarned = [.. responses.Select(response => response.PointsEarned)];

        pointsEarned.ShouldBe([4, 4, 4, 4, 2, 2]);
        reward.Points.ShouldBe(20);
    }

    [Theory]
    [InlineData(2022, 1, 10, true)]
    [InlineData(2022, 1, 11, false)]
    [InlineData(2021, 12, 15, true)]
    [InlineData(2022, 12, 15, false)]
    public void ProcessTransfer_ShouldCountTenureInFullYears_UpToTheRegistrationAnniversary(
        int registrationYear,
        int registrationMonth,
        int registrationDay,
        bool hasTenureBonus)
    {
        Reward reward = NewReward();
        var registeredOn = new DateTimeOffset(registrationYear, registrationMonth, registrationDay, 0, 0, 0,
            TimeSpan.Zero);

        RewardProcessingResponse response =
            Process(reward, CustomerType.Individual, IndividualQualifyingAmount, registeredOn);

        int expectedPoints = hasTenureBonus ? 4 : 2;

        response.PointsEarned.ShouldBe(expectedPoints);
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardCashbackSooner_WhenTheCorporateHasTheTenureBonus()
    {
        Reward reward = NewReward();

        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, CustomerType.Corporate, 26, LongTenureRegistration, TransferTime);

        RewardProcessingResponse twentySixthResponse = responses[^1];

        responses.SkipLast(1).ShouldAllBe(response => !response.EarnedCashback);
        twentySixthResponse.EarnedCashback.ShouldBeTrue();
        reward.Points.ShouldBe(90);
    }

    [Fact]
    public void ProcessTransfer_ShouldResetPointsAndTransfers_WhenATransferFallsInALaterMonth()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset nextMonthTransferTime = thisMonthTransferTime.AddMonths(1);

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 3, RecentRegistration, thisMonthTransferTime);
        ProcessQualifyingTransfers(reward, CustomerType.Individual, 1, RecentRegistration, nextMonthTransferTime);

        reward.Points.ShouldBe(2);
        reward.QualifyingTransfers.ShouldBe(1);
        reward.LastResetDate.ShouldBe(nextMonthTransferTime);
    }

    [Fact]
    public void ProcessTransfer_ShouldReset_EvenWhenTheFirstTransferOfTheNewMonthDoesNotQualify()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset nextMonthTransferTime = thisMonthTransferTime.AddMonths(1);

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 3, RecentRegistration, thisMonthTransferTime);

        Result<RewardProcessingResponse> nonQualifyingTransfer = reward.ProcessTransfer(
            CustomerType.Individual,
            IndividualThreshold,
            RecentRegistration,
            nextMonthTransferTime);

        nonQualifyingTransfer.IsSuccess.ShouldBeTrue();
        reward.Points.ShouldBe(0);
        reward.QualifyingTransfers.ShouldBe(0);
    }

    [Fact]
    public void ProcessTransfer_ShouldNotReset_WhenATransferIsBackdatedToAnEarlierMonth()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset previousMonthTransferTime = thisMonthTransferTime.AddMonths(-1);

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 1, RecentRegistration, thisMonthTransferTime);
        ProcessQualifyingTransfers(reward, CustomerType.Individual, 1, RecentRegistration, previousMonthTransferTime);

        reward.Points.ShouldBe(4);
        reward.QualifyingTransfers.ShouldBe(2);
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardAirtimeAgain_InTheFollowingMonth()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset nextMonthTransferTime = thisMonthTransferTime.AddMonths(1);

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 8, RecentRegistration, thisMonthTransferTime);

        RewardProcessingResponse eighthResponseNextMonth =
            ProcessQualifyingTransfers(reward, CustomerType.Individual, 8, RecentRegistration, nextMonthTransferTime);

        eighthResponseNextMonth.EarnedAirtime.ShouldBeTrue();
    }

    [Fact]
    public void ProcessTransfer_ShouldAwardCashbackAgain_InTheFollowingMonth()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset nextMonthTransferTime = thisMonthTransferTime.AddMonths(1);

        ProcessQualifyingTransfers(reward, CustomerType.Corporate, 30, RecentRegistration, thisMonthTransferTime);

        RewardProcessingResponse thirtiethResponseNextMonth =
            ProcessQualifyingTransfers(reward, CustomerType.Corporate, 30, RecentRegistration, nextMonthTransferTime);

        thirtiethResponseNextMonth.EarnedCashback.ShouldBeTrue();
        reward.LastCashBackDate.ShouldBe(nextMonthTransferTime);
    }

    [Fact]
    public void ProcessTransfer_ShouldRestartTheTenureBonus_InANewMonth()
    {
        Reward reward = NewReward();
        DateTimeOffset thisMonthTransferTime = reward.LastResetDate;
        DateTimeOffset nextMonthTransferTime = thisMonthTransferTime.AddMonths(1);

        ProcessQualifyingTransfers(reward, CustomerType.Individual, 5, LongTenureRegistration, thisMonthTransferTime);

        RewardProcessingResponse firstResponseNextMonth =
            ProcessQualifyingTransfers(reward, CustomerType.Individual, 1, LongTenureRegistration,
                nextMonthTransferTime);

        firstResponseNextMonth.PointsEarned.ShouldBe(4);
    }

    private static Reward NewReward()
    {
        var customerId = Guid.NewGuid();

        return Reward.Create(customerId);
    }

    private static RewardProcessingResponse Process(
        Reward reward,
        CustomerType customerType,
        decimal amount,
        DateTimeOffset registeredOn)
    {
        Result<RewardProcessingResponse> processed =
            reward.ProcessTransfer(customerType, amount, registeredOn, TransferTime);

        return processed.Value;
    }

    private static RewardProcessingResponse ProcessQualifyingTransfers(
        Reward reward,
        CustomerType customerType,
        int transfers,
        DateTimeOffset registeredOn,
        DateTimeOffset transferTime)
    {
        List<RewardProcessingResponse> responses =
            ProcessEachQualifyingTransfer(reward, customerType, transfers, registeredOn, transferTime);

        return responses[^1];
    }

    private static List<RewardProcessingResponse> ProcessEachQualifyingTransfer(
        Reward reward,
        CustomerType customerType,
        int transfers,
        DateTimeOffset registeredOn,
        DateTimeOffset transferTime)
    {
        decimal qualifyingAmount = customerType == CustomerType.Corporate
            ? CorporateQualifyingAmount
            : IndividualQualifyingAmount;

        List<RewardProcessingResponse> responses = [];

        for (int transfer = 0; transfer < transfers; transfer++)
        {
            Result<RewardProcessingResponse> processed =
                reward.ProcessTransfer(customerType, qualifyingAmount, registeredOn, transferTime);

            responses.Add(processed.Value);
        }

        return responses;
    }
}
