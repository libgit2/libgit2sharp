using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Commands
{
    abstract class CommandBase
    {
        /// <summary>
        /// Run the command
        /// </summary>
        public abstract void Run();
    }
}
