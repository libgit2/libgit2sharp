using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Xunit;
using Xunit.Extensions;
using Xunit.Sdk;
//**********************************************************************
//* This file is based on the DynamicSkipExample.cs in xUnit which is
//* provided under the following Ms-PL license:
//*
//* This license governs use of the accompanying software. If you use
//* the software, you accept this license. If you do not accept the
//* license, do not use the software.
//*
//* 1. Definitions
//*
//* The terms "reproduce," "reproduction," "derivative works," and
//* "distribution" have the same meaning here as under U.S. copyright
//* law.
//*
//* A "contribution" is the original software, or any additions or
//* changes to the software.
//*
//* A "contributor" is any person that distributes its contribution
//* under this license.
//*
//* "Licensed patents" are a contributor's patent claims that read
//* directly on its contribution.
//*
//* 2. Grant of Rights
//*
//* (A) Copyright Grant- Subject to the terms of this license, including
//* the license conditions and limitations in section 3, each
//* contributor grants you a non-exclusive, worldwide, royalty-free
//* copyright license to reproduce its contribution, prepare derivative
//* works of its contribution, and distribute its contribution or any
//* derivative works that you create.
//*
//* (B) Patent Grant- Subject to the terms of this license, including
//* the license conditions and limitations in section 3, each
//* contributor grants you a non-exclusive, worldwide, royalty-free
//* license under its licensed patents to make, have made, use, sell,
//* offer for sale, import, and/or otherwise dispose of its contribution
//* in the software or derivative works of the contribution in the
//* software.
//*
//* 3. Conditions and Limitations
//*
//* (A) No Trademark License- This license does not grant you rights to
//* use any contributors' name, logo, or trademarks.
//*
//* (B) If you bring a patent claim against any contributor over patents
//* that you claim are infringed by the software, your patent license
//* from such contributor to the software ends automatically.
//*
//* (C) If you distribute any portion of the software, you must retain
//* all copyright, patent, trademark, and attribution notices that are
//* present in the software.
//*
//* (D) If you distribute any portion of the software in source code
//* form, you may do so only under this license by including a complete
//* copy of this license with your distribution. If you distribute any
//* portion of the software in compiled or object code form, you may
//* only do so under a license that complies with this license.
//**********************************************************************

namespace LibGit2Sharp.Tests.TestHelpers
{
    class SkippableFactAttribute : FactAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            return base.EnumerateTestCommands(method).Select(SkippableTestCommand.Wrap(method));
        }
    }

    class SkippableTheoryAttribute : TheoryAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            return base.EnumerateTestCommands(method).Select(SkippableTestCommand.Wrap(method));
        }
    }

    class SkippableTestCommand : ITestCommand
    {
        public static Func<ITestCommand, ITestCommand> Wrap(IMethodInfo method)
        {
            return c => new SkippableTestCommand(method, c);
        }

        private readonly IMethodInfo method;
        private readonly ITestCommand inner;

        private SkippableTestCommand(IMethodInfo method, ITestCommand inner)
        {
            this.method = method;
            this.inner = inner;
        }

        public MethodResult Execute(object testClass)
        {
            try
            {
                return inner.Execute(testClass);
            }
            catch (SkipException e)
            {
                return new SkipResult(method, DisplayName, e.Reason);
            }
        }

        public XmlNode ToStartXml()
        {
            return inner.ToStartXml();
        }

        public string DisplayName
        {
            get { return inner.DisplayName; }
        }

        public bool ShouldCreateInstance
        {
            get { return inner.ShouldCreateInstance; }
        }

        public int Timeout
        {
            get { return inner.Timeout; }
        }
    }

    class SkipException : Exception
    {
        public SkipException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }
    }
}
