namespace Fcmb.Assessment.CSharp.Common.Domain;

public sealed record Error(string Code, string Message)
{
    public static Error? None => null;
};
