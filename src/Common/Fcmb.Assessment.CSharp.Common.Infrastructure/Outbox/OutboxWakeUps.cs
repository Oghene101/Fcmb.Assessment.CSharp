using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

// One per module: lane names repeat across modules, so each module's outbox lanes are woken on their own.
public sealed class OutboxWakeUps<TUnitOfWork>(
    IReadOnlyList<string> lanes) : LaneWakeUps(lanes)
    where TUnitOfWork : ICommonUnitOfWork;
