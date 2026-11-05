namespace Fcmb.Assessment.CSharp.UnitTests.TestSupport;

internal sealed class FixedTimeProvider(
    DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
