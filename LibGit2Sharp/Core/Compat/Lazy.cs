using System;

namespace LibGit2Sharp.Core.Compat
{
    /// <summary>
    ///   Provides support for lazy initialization.
    /// </summary>
    /// <typeparam name = "TType">Specifies the type of object that is being lazily initialized.</typeparam>
    public class Lazy<TType>
    {
        private readonly Func<TType> evaluator;
        private TType value;
        private bool hasBeenEvaluated;
        private readonly object padLock = new object();

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Lazy{TType}" /> class.
        /// </summary>
        /// <param name = "evaluator"></param>
        public Lazy(Func<TType> evaluator)
        {
            Ensure.ArgumentNotNull(evaluator, "evaluator");

            this.evaluator = evaluator;
        }

        /// <summary>
        ///   Gets the lazily initialized value of the current instance.
        /// </summary>
        public TType Value
        {
            get { return Evaluate(); }
        }

        private TType Evaluate()
        {
            if (!hasBeenEvaluated)
            {
                lock (padLock)
                {
                    if (!hasBeenEvaluated)
                    {
                        value = evaluator();
                        hasBeenEvaluated = true;
                    }
                }
            }

            return value;
        }
    }
}
