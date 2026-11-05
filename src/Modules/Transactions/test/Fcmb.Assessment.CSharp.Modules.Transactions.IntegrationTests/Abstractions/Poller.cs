namespace Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Abstractions;

internal static class Poller
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    // Background lanes deliver eventually; probe returns null until the expected state has arrived.
    internal static async Task<T?> WaitAsync<T>(
        TimeSpan timeout,
        Func<Task<T?>> probe,
        CancellationToken cancellationToken)
        where T : class
    {
        using var timer = new PeriodicTimer(Interval);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            T? arrived = await probe();

            if (arrived is null)
            {
                await timer.WaitForNextTickAsync(cancellationToken);

                continue;
            }

            return arrived;
        }

        return null;
    }
}
