using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class DisposableTuple<T1, T2> : IDisposable
        where T1 : IDisposable where T2 : IDisposable
    {
        private Tuple<T1, T2> _tuple;

        public DisposableTuple(T1 item1, T2 item2)
        {
            _tuple = new Tuple<T1, T2>(item1, item2);
        }

        public T1 Item1 { get { return _tuple.Item1; } }
        public T2 Item2 { get { return _tuple.Item2; } }

        public void Dispose()
        {
            if (_tuple == null)
            {
                return;
            }

            _tuple.Item1.SafeDispose();
            _tuple.Item2.SafeDispose();
            _tuple = null;

            GC.SuppressFinalize(this);
        }
    }
}
