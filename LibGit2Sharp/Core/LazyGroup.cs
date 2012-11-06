using System;
using System.Collections.Generic;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp.Core
{
    internal abstract class LazyGroup<T>
    {
        private readonly IList<IEvaluator<T>> evaluators = new List<IEvaluator<T>>();
        private readonly object @lock = new object();
        private bool evaluated;

        public ILazy<TResult> AddLazy<TResult>(Func<T, TResult> func)
        {
            var prop = new Dependent<T, TResult>(func, this);
            evaluators.Add(prop);
            return prop;
        }

        public void Evaluate()
        {
            if (evaluated)
                return;

            lock (@lock)
            {
                if (evaluated)
                    return;

                EvaluateInternal(input =>
                                 {
                                     foreach (var e in evaluators)
                                         e.Evaluate(input);
                                 });
                evaluated = true;
            }
        }

        protected abstract void EvaluateInternal(Action<T> evaluator);

        protected static ILazy<TResult> Singleton<TResult>(Func<TResult> resultSelector)
        {
            return new LazyWrapper<TResult>(resultSelector);
        }

        private interface IEvaluator<TInput>
        {
            void Evaluate(TInput input);
        }

        private class Dependent<TInput, TOutput> : ILazy<TOutput>, IEvaluator<TInput>
        {
            private readonly Func<TInput, TOutput> valueFactory;
            private readonly LazyGroup<TInput> lazyGroup;

            private TOutput value;
            private bool hasBeenEvaluated;

            public Dependent(Func<TInput, TOutput> valueFactory, LazyGroup<TInput> lazyGroup)
            {
                this.valueFactory = valueFactory;
                this.lazyGroup = lazyGroup;
            }

            public TOutput Value
            {
                get { return Evaluate(); }
            }

            private TOutput Evaluate()
            {
                if (!hasBeenEvaluated)
                {
                    lazyGroup.Evaluate();
                }

                return value;
            }

            void IEvaluator<TInput>.Evaluate(TInput input)
            {
                value = valueFactory(input);
                hasBeenEvaluated = true;
            }
        }

        protected class LazyWrapper<TType> : Lazy<TType>, ILazy<TType>
        {
            public LazyWrapper(Func<TType> evaluator)
                : base(evaluator)
            {
            }
        }
    }
}
