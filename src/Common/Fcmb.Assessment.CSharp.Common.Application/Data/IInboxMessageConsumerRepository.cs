namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IInboxMessageConsumerRepository
{
    Task<bool> InboxMessageConsumerExistsAsync(Guid inboxMessageId, string name);
}
