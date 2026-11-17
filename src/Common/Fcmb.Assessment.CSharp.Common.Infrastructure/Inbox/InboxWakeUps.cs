using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

// One per module: lane names repeat across modules, so each module's inbox lanes are woken on their own.
public sealed class InboxWakeUps<TUnitOfWork>(
    IReadOnlyList<string> lanes) : LaneWakeUps(lanes)
    where TUnitOfWork : ICommonUnitOfWork;
