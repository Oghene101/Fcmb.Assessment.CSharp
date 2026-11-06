using Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.IntegrationTests.AuditLog;

public sealed class AuditLogPartitionsTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string PartitionSchemeSql = """
                                              SELECT partitionScheme.name AS Value
                                              FROM sys.indexes clusteredIndex
                                              JOIN sys.partition_schemes partitionScheme
                                                  ON partitionScheme.data_space_id = clusteredIndex.data_space_id
                                              WHERE clusteredIndex.object_id = OBJECT_ID({0})
                                                AND clusteredIndex.index_id = 1
                                              """;

    private const string BoundariesSql = """
                                         SELECT CAST(boundary.value AS datetimeoffset(7)) AS Value
                                         FROM sys.partition_range_values boundary
                                         JOIN sys.partition_functions partitionFunction
                                             ON partitionFunction.function_id = boundary.function_id
                                         WHERE partitionFunction.name = {0}
                                         """;

    private static DateTimeOffset CurrentMonth
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        }
    }

    [Theory]
    [InlineData("users")]
    [InlineData("transactions")]
    public async Task StartUp_ClustersEachModulesAuditLogsOnMonthlyPartitions(string schema)
    {
        List<string> partitionSchemes = await TransactionsDbContext.Database
            .SqlQueryRaw<string>(PartitionSchemeSql, $"{schema}.AuditLogs")
            .ToListAsync(CancellationToken);

        List<DateTimeOffset> boundaries = await TransactionsDbContext.Database
            .SqlQueryRaw<DateTimeOffset>(BoundariesSql, $"pf_{schema}_AuditLogs")
            .ToListAsync(CancellationToken);

        partitionSchemes.ShouldBe([$"ps_{schema}_AuditLogs"]);
        boundaries.ShouldContain(CurrentMonth.AddMonths(3));
    }

    // Empties the current month of the shared database's transactions audit log; no other test reads it.
    [Fact]
    public async Task DroppingMonths_EmptiesOnlyTheMonthsBeforeTheCutoff()
    {
        IUnitOfWork unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.AuditLogPartitionsRepository.CreateMonthsAheadAsync(CurrentMonth, 5);

        Guid droppedEntityId = await InsertAuditLogAsync(CurrentMonth.AddMonths(1).AddDays(3));

        Guid keptEntityId = await InsertAuditLogAsync(CurrentMonth.AddMonths(4).AddDays(3));

        int droppedMonths = await unitOfWork.AuditLogPartitionsRepository
            .DropMonthsBeforeAsync(CurrentMonth.AddMonths(2));

        List<Guid> remainingEntityIds = await TransactionsDbContext.Database
            .SqlQueryRaw<Guid>("SELECT EntityId AS Value FROM transactions.AuditLogs")
            .ToListAsync(CancellationToken);

        List<DateTimeOffset> boundaries = await TransactionsDbContext.Database
            .SqlQueryRaw<DateTimeOffset>(BoundariesSql, "pf_transactions_AuditLogs")
            .ToListAsync(CancellationToken);

        droppedMonths.ShouldBeGreaterThanOrEqualTo(3);
        remainingEntityIds.ShouldNotContain(droppedEntityId);
        remainingEntityIds.ShouldContain(keptEntityId);
        boundaries.Min().ShouldBe(CurrentMonth.AddMonths(3));
    }

    private async Task<Guid> InsertAuditLogAsync(DateTimeOffset occurredOn)
    {
        var entityId = Guid.CreateVersion7();

        await TransactionsDbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO transactions.AuditLogs (Id, Action, UserId, EntityName, EntityId, OccurredOn, Changes)
             VALUES ({Guid.CreateVersion7()}, 'Added', 'system', 'Test', {entityId}, {occurredOn}, 'test')
             """,
            CancellationToken);

        return entityId;
    }
}
