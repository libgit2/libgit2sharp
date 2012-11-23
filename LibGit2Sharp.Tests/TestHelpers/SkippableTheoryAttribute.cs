using System.Collections.Generic;
using System.Linq;
using Xunit.Extensions;
using Xunit.Sdk;

namespace LibGit2Sharp.Tests.TestHelpers
{
    internal class SkippableTheoryAttribute : TheoryAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            IEnumerable<ITestCommand> commands = base.EnumerateTestCommands(method);
            return commands.Select(c => new SkippableTestCommand(method, (TheoryCommand)c)).Cast<ITestCommand>();
        }

        class SkippableTestCommand : TheoryCommand
        {
            private readonly TheoryCommand internalCommand;

            public SkippableTestCommand(IMethodInfo method, TheoryCommand internalCommand)
                : base(method, null)
            {
                this.internalCommand = internalCommand;
            }

            public override MethodResult Execute(object testClass)
            {
                try
                {
                    return internalCommand.Execute(testClass);
                }
                catch (SkipException e)
                {
                    return new SkipResult(testMethod, DisplayName, e.Reason);
                }
            }
        }
    }
}