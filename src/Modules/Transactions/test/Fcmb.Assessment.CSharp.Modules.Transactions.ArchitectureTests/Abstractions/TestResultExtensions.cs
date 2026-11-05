using TestResult = NetArchTest.Rules.TestResult;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.ArchitectureTests.Abstractions;

internal static class TestResultExtensions
{
    internal static void ShouldBeSuccessful(this TestResult testResult)
    {
        IReadOnlyList<string> failingTypeNames = testResult.FailingTypeNames ?? [];

        failingTypeNames.ShouldBeEmpty();
    }
}
