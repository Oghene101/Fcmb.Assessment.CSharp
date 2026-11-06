using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Common.Application.Data;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

// Each module's AuditLogs table sits on its own monthly RANGE RIGHT partition function and scheme, created by the
// module's migration: one boundary per UTC month start, so each month is one partition.
#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class AuditLogPartitionRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IAuditLogPartitionRepository
{
    // Partition functions and schemes are database-wide, so each module's carries its schema in the name.
    private string PartitionFunction => $"pf_{schema}_AuditLogs";

    private string PartitionScheme => $"ps_{schema}_AuditLogs";

    // A boundary for every month start up to the one after the last month ahead, so each of those months is its
    // own partition. Splitting an empty partition only changes metadata.
    public async Task CreateMonthsAheadAsync(
        DateTimeOffset currentMonth,
        int monthsAhead)
    {
        string sql = $"""
                      DECLARE @month datetimeoffset(7) = @currentMonth;
                      DECLARE @lastBoundary datetimeoffset(7) = DATEADD(MONTH, @monthsAhead + 1, @currentMonth);

                      WHILE @month <= @lastBoundary
                      BEGIN
                          IF NOT EXISTS (
                              SELECT 1
                              FROM sys.partition_range_values boundary
                              JOIN sys.partition_functions partitionFunction
                                  ON partitionFunction.function_id = boundary.function_id
                              WHERE partitionFunction.name = '{PartitionFunction}'
                                AND CAST(boundary.value AS datetimeoffset(7)) = @month)
                          BEGIN
                              ALTER PARTITION SCHEME {PartitionScheme} NEXT USED [PRIMARY];
                              ALTER PARTITION FUNCTION {PartitionFunction}() SPLIT RANGE (@month);
                          END

                          SET @month = DATEADD(MONTH, 1, @month);
                      END
                      """;

        await connection.ExecuteAsync(sql, new { currentMonth, monthsAhead }, transaction);
    }

    // Partition 1 holds everything before the lowest boundary. While that boundary is no later than keepFrom, every
    // row in partition 1 is older than keepFrom: truncate it, then merge the boundary so the next month moves in.
    public async Task<int> DropMonthsBeforeAsync(DateTimeOffset keepFrom)
    {
        string sql = $"""
                      SET XACT_ABORT ON;

                      DECLARE @lowestBoundary datetimeoffset(7);
                      DECLARE @dropped int = 0;

                      WHILE 1 = 1
                      BEGIN
                          SET @lowestBoundary = NULL;

                          SELECT TOP (1) @lowestBoundary = CAST(boundary.value AS datetimeoffset(7))
                          FROM sys.partition_range_values boundary
                          JOIN sys.partition_functions partitionFunction
                              ON partitionFunction.function_id = boundary.function_id
                          WHERE partitionFunction.name = '{PartitionFunction}'
                          ORDER BY boundary.boundary_id;

                          IF @lowestBoundary IS NULL OR @lowestBoundary > @keepFrom
                              BREAK;

                          TRUNCATE TABLE {schema}.AuditLogs WITH (PARTITIONS (1));
                          ALTER PARTITION FUNCTION {PartitionFunction}() MERGE RANGE (@lowestBoundary);

                          SET @dropped += 1;
                      END

                      SELECT @dropped;
                      """;

        int droppedMonths = await connection.ExecuteScalarAsync<int>(sql, new { keepFrom }, transaction);

        return droppedMonths;
    }
}
#pragma warning restore S2077
