using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Common.Application.Data;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class OutboxMessageConsumerRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IOutboxMessageConsumerRepository
{
    public async Task<bool> OutboxMessageConsumerExistsAsync(Guid outboxMessageId, string name)
    {
        string sql =
            $"""
             IF EXISTS (
                 SELECT 1 
                 FROM {schema}.OutboxMessageConsumers 
                 WHERE OutboxMessageId = @outboxMessageId 
                   AND Name = @name
             )
             SELECT 1 ELSE SELECT 0
             """;

        return await connection.ExecuteScalarAsync<bool>(sql, new { outboxMessageId, name }, transaction);
    }
}
#pragma warning restore S2077
