using System.Reflection;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}

