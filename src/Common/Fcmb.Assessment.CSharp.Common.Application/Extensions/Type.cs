namespace Fcmb.Assessment.CSharp.Common.Application.Extensions;

public static class TypeExtensions
{
    public static string GetOuterAndInnerName(this Type type)
    {
        string[] parts = type.FullName!.Split('.');

        return parts[^1].Replace('+', '.');
    }
}
