using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class InboxMessageRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IInboxMessageRepository
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
                          FROM {schema}.InboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
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

    public async Task<InboxMessage?> GetUnprocessedByIdAsync(Guid id)
    {
        string sql = $"""
                      SELECT *
                      FROM {schema}.InboxMessages
                      WHERE Id = @id
                        AND ProcessedOn IS NULL
                      """;

        InboxMessage? inboxMessage = await connection.QueryFirstOrDefaultAsync<InboxMessage>(
            sql,
            new { id },
            transaction);

        return inboxMessage;
    }

    // Messages and their consumer records go together, so a consumer record never outlives its message.
    public async Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff)
    {
        string sql = $"""
                      SET XACT_ABORT ON;
                      BEGIN TRANSACTION;

                      DECLARE @Purged TABLE (Id uniqueidentifier PRIMARY KEY);

                      DELETE FROM {schema}.InboxMessages
                      OUTPUT deleted.Id INTO @Purged
                      WHERE ProcessedOn < @cutoff;

                      DELETE FROM {schema}.InboxMessageConsumers
                      WHERE InboxMessageId IN (SELECT Id FROM @Purged);

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
