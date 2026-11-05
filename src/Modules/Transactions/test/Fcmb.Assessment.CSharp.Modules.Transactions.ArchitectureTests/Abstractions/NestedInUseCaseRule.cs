using Mono.Cecil;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.ArchitectureTests.Abstractions;

// Use cases group their Command, Handler and Validator as nested types of a static *UseCase class.
internal sealed class NestedInUseCaseRule : ICustomRule
{
    private const string UseCaseSuffix = "UseCase";

    public bool MeetsRule(TypeDefinition type)
    {
        TypeDefinition? declaringType = type.DeclaringType;

        if (declaringType is null)
        {
            return false;
        }

        return declaringType.Name.EndsWith(UseCaseSuffix, StringComparison.Ordinal);
    }
}
