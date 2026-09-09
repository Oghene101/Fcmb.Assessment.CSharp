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
    public async Task<Guid[]> GetIdsAsync(int batchSize)
    {
        string sql = $"""
                      SELECT TOP (@batchSize) Id
                      FROM {schema}.InboxMessages WITH (UPDLOCK, READPAST)
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

    public async Task<InboxMessage?> GetByIdAsync(Guid id)
    {
        string sql = $"""
                      SELECT *
                      FROM {schema}.InboxMessages WITH (UPDLOCK, READPAST)
                      WHERE Id = @id
                      """;

        InboxMessage? inboxMessage = await connection.QueryFirstOrDefaultAsync<InboxMessage>(
            sql,
            new { id },
            transaction);

        return inboxMessage;
    }
}
#pragma warning restore S2077
