using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Messaging;

public sealed class LaneWakeUpsTests
{
    private const string Lane = MessageLanes.Default;

    private static readonly TimeSpan LongWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShortWait = TimeSpan.FromMilliseconds(50);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AWakeUp_EndsTheLanesWait()
    {
        var wakeUps = new OutboxWakeUps<TestUnitOfWork>([Lane]);
        Task<bool> waiting = wakeUps.WaitAsync(Lane, LongWait, TimeProvider.System, Token);

        wakeUps.WakeUp(Lane);

        bool wokenUp = await waiting;
        wokenUp.ShouldBeTrue();
    }

    [Fact]
    public async Task WakeUps_WhileTheLaneIsBusy_EndOnlyItsNextWait()
    {
        var wakeUps = new OutboxWakeUps<TestUnitOfWork>([Lane]);
        wakeUps.WakeUp(Lane);
        wakeUps.WakeUp(Lane);

        bool firstWaitWokenUp = await wakeUps.WaitAsync(Lane, LongWait, TimeProvider.System, Token);
        bool secondWaitWokenUp = await wakeUps.WaitAsync(Lane, ShortWait, TimeProvider.System, Token);

        firstWaitWokenUp.ShouldBeTrue();
        secondWaitWokenUp.ShouldBeFalse();
    }

    [Fact]
    public async Task AWaitWithoutAWakeUp_RunsOut()
    {
        var wakeUps = new OutboxWakeUps<TestUnitOfWork>([Lane]);

        bool wokenUp = await wakeUps.WaitAsync(Lane, ShortWait, TimeProvider.System, Token);

        wokenUp.ShouldBeFalse();
    }

    [Fact]
    public async Task Stopping_EndsTheWait_AsNotWokenUp()
    {
        var wakeUps = new OutboxWakeUps<TestUnitOfWork>([Lane]);
        using var stopping = new CancellationTokenSource();
        Task<bool> waiting = wakeUps.WaitAsync(Lane, LongWait, TimeProvider.System, stopping.Token);

        await stopping.CancelAsync();

        bool wokenUp = await waiting;
        wokenUp.ShouldBeFalse();
    }

    [Fact]
    public async Task AWakeUpForAnUnknownLane_WakesNoOtherLane()
    {
        var wakeUps = new OutboxWakeUps<TestUnitOfWork>([Lane]);

        wakeUps.WakeUp("Unknown");

        bool wokenUp = await wakeUps.WaitAsync(Lane, ShortWait, TimeProvider.System, Token);
        wokenUp.ShouldBeFalse();
    }
}
