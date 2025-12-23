using FluentAssertions;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    public class ConsoleHelperTests
    {
        [Fact]
        public void WriteSuccess_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteSuccess("Test success message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void WriteError_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteError("Test error message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void WriteInfo_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteInfo("Test info message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void WriteWarning_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            try
            {
                using var sw = new StringWriter();
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteWarning("Test warning message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        [Fact]
        public void ReadLine_ShouldAcceptPromptWithoutThrowing()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                using var sw = new StringWriter();
                using var sr = new StringReader("test input\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.ReadLine("Enter something:");
                
                result.Should().Be("test input");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public void Confirm_ShouldReturnTrue_WhenUserEntersY()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                using var sw = new StringWriter();
                using var sr = new StringReader("y\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeTrue();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public void Confirm_ShouldReturnTrue_WhenUserEntersUppercaseY()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                using var sw = new StringWriter();
                using var sr = new StringReader("Y\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeTrue();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public void Confirm_ShouldReturnFalse_WhenUserEntersN()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                using var sw = new StringWriter();
                using var sr = new StringReader("n\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeFalse();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }

        [Fact]
        public void Confirm_ShouldReturnFalse_WhenUserEntersAnythingElse()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            try
            {
                using var sw = new StringWriter();
                using var sr = new StringReader("maybe\n");
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeFalse();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }
        }
    }
}
