using System.Data;
using Dapper;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.PhoneNumbers;

#pragma warning disable S2077 // Formatting SQL queries is safe here as schema is a known internal identifier
public sealed class PhoneNumberRepository(
    string schema,
    IDbConnection connection,
    IDbTransaction? transaction) : IPhoneNumberRepository
{
    public async Task<bool> PhoneNumberExistsAsync(string number)
    {
        string sql =
            $"""
             IF EXISTS (
                 SELECT 1
                 FROM {schema}.PhoneNumbers
                 WHERE Number = @number
                   AND DeletedAt IS NULL
             )
             SELECT 1 ELSE SELECT 0
             """;

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new { number },
            transaction);
    }
}
#pragma warning restore S2077
