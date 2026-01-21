using FluentAssertions;
using SitefinityContentUpdater.Core.Helpers;

namespace SitefinityContentUpdater.Core.Tests.Helpers
{
    [Collection("ConsoleTests")]
    public class ConsoleHelperTests
    {
        [Fact]
        public void WriteSuccess_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteSuccess("Test success message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void WriteError_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteError("Test error message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void WriteInfo_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteInfo("Test info message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void WriteWarning_ShouldNotThrow()
        {
            var originalOut = Console.Out;
            var sw = new StringWriter();
            try
            {
                Console.SetOut(sw);
                
                Action act = () => ConsoleHelper.WriteWarning("Test warning message");
                act.Should().NotThrow();
            }
            finally
            {
                Console.SetOut(originalOut);
                sw.Dispose();
            }
        }

        [Fact]
        public void ReadLine_ShouldAcceptPromptWithoutThrowing()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("test input\n");
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.ReadLine("Enter something:");
                
                result.Should().Be("test input");
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public void Confirm_ShouldReturnTrue_WhenUserEntersY()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("y\n");
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeTrue();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public void Confirm_ShouldReturnTrue_WhenUserEntersUppercaseY()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("Y\n");
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeTrue();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public void Confirm_ShouldReturnFalse_WhenUserEntersN()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("n\n");
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeFalse();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }

        [Fact]
        public void Confirm_ShouldReturnFalse_WhenUserEntersAnythingElse()
        {
            var originalOut = Console.Out;
            var originalIn = Console.In;
            var sw = new StringWriter();
            var sr = new StringReader("maybe\n");
            try
            {
                Console.SetOut(sw);
                Console.SetIn(sr);

                var result = ConsoleHelper.Confirm("Confirm?");
                
                result.Should().BeFalse();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
                sw.Dispose();
                sr.Dispose();
            }
        }
    }
}
