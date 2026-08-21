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
    public async Task<Guid[]> GetIdsAsync(int batchSize)
    {
        string sql = $"""
                      SELECT TOP (@batchSize) Id
                      FROM {schema}.OutboxMessages WITH (UPDLOCK, READPAST)
                      WHERE ProcessedOn IS NULL
                        AND (NextRetryOn IS NULL OR NextRetryOn <= SYSUTCDATETIME())
                      ORDER BY OccurredOn
                      """;

        IEnumerable<Guid> ids = await connection.QueryAsync<Guid>(
            sql,
            new { batchSize },
            transaction);

        return [.. ids];
    }

    public async Task<OutboxMessage?> GetByIdAsync(Guid id)
    {
        string sql = $"""
                      SELECT *
                      FROM {schema}.OutboxMessages WITH (UPDLOCK, READPAST)
                      WHERE Id = @id
                      """;

        OutboxMessage? outboxMessage = await connection.QueryFirstOrDefaultAsync<OutboxMessage>(
            sql,
            new { id },
            transaction);

        return outboxMessage;
    }
}
#pragma warning restore S2077
