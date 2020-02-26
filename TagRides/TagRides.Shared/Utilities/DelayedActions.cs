using System;
using System.Collections.Generic;
using System.Threading;

namespace TagRides.Shared.Utilities
{
    /// <summary>
    /// Can be used to run a list of actions at the end of some scope. Use
    /// in a <see langword="using"/> statement.
    /// </summary>
    public class DelayedActions : IDisposable
    {
        public void Run(Action action)
        {
            actionsToRun.Add(action);
        }

        public void Dispose()
        {
            foreach (var action in actionsToRun)
                action();
        }

        readonly List<Action> actionsToRun = new List<Action>();
    }
}
