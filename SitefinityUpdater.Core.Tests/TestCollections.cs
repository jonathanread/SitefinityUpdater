using Xunit;

namespace SitefinityContentUpdater.Core.Tests
{
    /// <summary>
    /// Test collection definitions for controlling test parallelization.
    /// Tests that modify Console.Out/Console.In should use these collections
    /// to prevent parallel execution issues.
    /// </summary>
    [CollectionDefinition("ConsoleTests", DisableParallelization = true)]
    public class ConsoleTestsCollection : ICollectionFixture<ConsoleTestsFixture>
    {
    }

    /// <summary>
    /// Fixture for console-related tests. Can be extended to provide shared setup/teardown.
    /// </summary>
    public class ConsoleTestsFixture
    {
    }
}
