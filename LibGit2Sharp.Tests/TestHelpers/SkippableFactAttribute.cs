using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

//**********************************************************************
//* This file is based on the DynamicSkipExample.cs in xUnit which is
//* provided under the following Apache license.
//*
//*
//* Copyright 2014 Outercurve Foundation
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*     http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.
//*
//*
//* Refs:
//*  - https://github.com/xunit/samples.xunit/tree/master/DynamicSkipExample
//*  - https://github.com/xunit/samples.xunit/blob/master/license.txt
//**********************************************************************

namespace LibGit2Sharp.Tests.TestHelpers
{
    [XunitTestCaseDiscoverer("LibGit2Sharp.Tests.TestHelpers.SkippableTheoryDiscoverer", "LibGit2Sharp.Tests")]
    public class SkippableTheoryAttribute : TheoryAttribute { }

    [XunitTestCaseDiscoverer("LibGit2Sharp.Tests.TestHelpers.SkippableFactDiscoverer", "LibGit2Sharp.Tests")]
    public class SkippableFactAttribute : FactAttribute { }

    public class SkippableFactDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink diagnosticMessageSink;

        public SkippableFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            yield return new SkippableFactTestCase(diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
        }
    }

    public class SkippableFactTestCase : XunitTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippableFactTestCase() { }

        public SkippableFactTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments) { }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                        IMessageBus messageBus,
                                                        object[] constructorArguments,
                                                        ExceptionAggregator aggregator,
                                                        CancellationTokenSource cancellationTokenSource)
        {
            var skipMessageBus = new SkippableFactMessageBus(messageBus);
            var result = await base.RunAsync(diagnosticMessageSink, skipMessageBus, constructorArguments, aggregator, cancellationTokenSource);
            if (skipMessageBus.DynamicallySkippedTestCount > 0)
            {
                result.Failed -= skipMessageBus.DynamicallySkippedTestCount;
                result.Skipped += skipMessageBus.DynamicallySkippedTestCount;
            }

            return result;
        }
    }

    public class SkippableTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink diagnosticMessageSink;
        readonly TheoryDiscoverer theoryDiscoverer;

        public SkippableTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;

            theoryDiscoverer = new TheoryDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();

            // Unlike fact discovery, the underlying algorithm for theories is complex, so we let the theory discoverer
            // do its work, and do a little on-the-fly conversion into our own test cases.
            return theoryDiscoverer.Discover(discoveryOptions, testMethod, factAttribute)
                                   .Select(testCase => testCase is XunitTheoryTestCase
                                                           ? (IXunitTestCase)new SkippableTheoryTestCase(diagnosticMessageSink, defaultMethodDisplay, testCase.TestMethod)
                                                           : new SkippableFactTestCase(diagnosticMessageSink, defaultMethodDisplay, testCase.TestMethod, testCase.TestMethodArguments));
        }
    }

    public class SkippableTheoryTestCase : XunitTheoryTestCase
    {
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippableTheoryTestCase() { }

        public SkippableTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod) { }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                        IMessageBus messageBus,
                                                        object[] constructorArguments,
                                                        ExceptionAggregator aggregator,
                                                        CancellationTokenSource cancellationTokenSource)
        {
            // Duplicated code from SkippableFactTestCase. I'm sure we could find a way to de-dup with some thought.
            var skipMessageBus = new SkippableFactMessageBus(messageBus);
            var result = await base.RunAsync(diagnosticMessageSink, skipMessageBus, constructorArguments, aggregator, cancellationTokenSource);
            if (skipMessageBus.DynamicallySkippedTestCount > 0)
            {
                result.Failed -= skipMessageBus.DynamicallySkippedTestCount;
                result.Skipped += skipMessageBus.DynamicallySkippedTestCount;
            }

            return result;
        }
    }

    public class SkippableFactMessageBus : IMessageBus
    {
        readonly IMessageBus innerBus;

        public SkippableFactMessageBus(IMessageBus innerBus)
        {
            this.innerBus = innerBus;
        }

        public int DynamicallySkippedTestCount { get; private set; }

        public void Dispose() { }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            var testFailed = message as ITestFailed;
            if (testFailed != null)
            {
                var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
                if (exceptionType == typeof(SkipTestException).FullName)
                {
                    DynamicallySkippedTestCount++;
                    return innerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
                }
            }

            // Nothing we care about, send it on its way
            return innerBus.QueueMessage(message);
        }
    }

    class SkipTestException : Exception
    {
        public SkipTestException(string reason)
            : base(reason) { }
    }
}
