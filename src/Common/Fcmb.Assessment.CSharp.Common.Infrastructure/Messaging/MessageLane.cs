namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public sealed record MessageLane(
    string Module,
    string Name,
    MessageLaneSettings Settings,
    int LeaseSeconds);
