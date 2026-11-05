using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class OutboxMessageRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IOutboxMessageRepository
{
    // One statement claims and leases, so no lock or transaction is held while the handlers run.
    public async Task<Guid[]> ClaimAsync(
        string lane,
        int limit,
        DateTimeOffset now,
        DateTimeOffset claimedUntil)
    {
        string sql = $"""
                      WITH Claimable AS (
                          SELECT TOP (@limit) Id, ClaimedUntil
                          FROM {schema}.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                          WHERE Lane = @lane
                            AND ProcessedOn IS NULL
                            AND (NextRetryOn IS NULL OR NextRetryOn <= @now)
                            AND (ClaimedUntil IS NULL OR ClaimedUntil < @now)
                          ORDER BY OccurredOn)
                      UPDATE Claimable
                      SET ClaimedUntil = @claimedUntil
                      OUTPUT inserted.Id
                      """;

        // Lane is a varchar column; an nvarchar parameter would force a conversion and skip the index.
        var laneParameter = new DbString { Value = lane, IsAnsi = true, Length = 50 };

        IEnumerable<Guid> claimedIds = await connection.QueryAsync<Guid>(
            sql,
            new { lane = laneParameter, limit, now, claimedUntil },
            transaction);

        return [.. claimedIds];
    }

    public async Task<OutboxMessage?> GetUnprocessedByIdAsync(Guid id)
    {
        string sql = $"""
                      SELECT *
                      FROM {schema}.OutboxMessages
                      WHERE Id = @id
                        AND ProcessedOn IS NULL
                      """;

        OutboxMessage? outboxMessage = await connection.QueryFirstOrDefaultAsync<OutboxMessage>(
            sql,
            new { id },
            transaction);

        return outboxMessage;
    }

    // Messages and their consumer records go together, so a consumer record never outlives its message.
    public async Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff)
    {
        string sql = $"""
                      SET XACT_ABORT ON;
                      BEGIN TRANSACTION;

                      DECLARE @Purged TABLE (Id uniqueidentifier PRIMARY KEY);

                      DELETE FROM {schema}.OutboxMessages
                      OUTPUT deleted.Id INTO @Purged
                      WHERE ProcessedOn < @cutoff;

                      DELETE FROM {schema}.OutboxMessageConsumers
                      WHERE OutboxMessageId IN (SELECT Id FROM @Purged);

                      COMMIT TRANSACTION;

                      SELECT COUNT(*) FROM @Purged;
                      """;

        int purgedMessages = await connection.ExecuteScalarAsync<int>(
            sql,
            new { cutoff },
            transaction);

        return purgedMessages;
    }
}
#pragma warning restore S2077
