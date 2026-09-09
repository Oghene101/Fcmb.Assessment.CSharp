using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Common.Application.Data;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class InboxMessageConsumerRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IInboxMessageConsumerRepository
{
    public async Task<bool> InboxMessageConsumerExistsAsync(Guid inboxMessageId, string name)
    {
        string sql =
            $"""
             IF EXISTS (
                 SELECT 1 
                 FROM {schema}.InboxMessageConsumers 
                 WHERE InboxMessageId = @inboxMessageId 
                   AND Name = @name
             )
             SELECT 1 ELSE SELECT 0
             """;

        return await connection.ExecuteScalarAsync<bool>(sql, new { inboxMessageId, name }, transaction);
    }
}
#pragma warning restore S2077
