namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

internal sealed class FixedTimeProvider(
    DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
