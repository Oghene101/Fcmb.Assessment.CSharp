using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Emails;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class EmailRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IEmailRepository
{
    public async Task<bool> EmailExistsAsync(string address)
    {
        string sql =
            $"""
             IF EXISTS (
                 SELECT 1
                 FROM {schema}.Emails
                 WHERE Address = @address
                   AND DeletedAt IS NULL
             )
             SELECT 1 ELSE SELECT 0
             """;

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new { address },
            transaction);
    }
}
#pragma warning restore S2077
