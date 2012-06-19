﻿using System.Text;
using Xunit;
using System.Reflection;
using System;
using System.Linq;
using System.Collections.Generic;

namespace LibGit2Sharp.Tests
{
    public class MetaFixture
    {
        private static readonly Type[] excludedTypes = new[] { typeof(Repository) };

        // Related to https://github.com/libgit2/libgit2sharp/pull/185
        [Fact]
        public void TypesInLibGit2SharpMustBeExtensibleInATestingContext()
        {
            var nonTestableTypes = new Dictionary<Type, IEnumerable<string>>();

            IEnumerable<Type> libGit2SharpTypes = Assembly.GetAssembly(typeof(Repository)).GetExportedTypes().Where(t => !excludedTypes.Contains(t) && t.Namespace == typeof(Repository).Namespace);

            foreach (Type type in libGit2SharpTypes)
            {
                if (type.IsInterface || type.IsEnum || IsStatic(type))
                    continue;

                ConstructorInfo[] publicConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                if (publicConstructor.Any())
                {
                    continue;
                }

                var nonVirtualMethodNamesForType = GetNonVirtualPublicMethodsNames(type).ToList();
                if (nonVirtualMethodNamesForType.Any())
                {
                    nonTestableTypes.Add(type, nonVirtualMethodNamesForType);
                    continue;
                }

                if (!HasEmptyProtectedConstructor(type))
                {
                    nonTestableTypes.Add(type, new List<string>());
                }
            }

            if (nonTestableTypes.Any())
            {
                Assert.True(false, Environment.NewLine + BuildNonTestableTypesMessage(nonTestableTypes));
            }
        }

        private static string BuildNonTestableTypesMessage(Dictionary<Type, IEnumerable<string>> nonTestableTypes)
        {
            var sb = new StringBuilder();

            foreach (var kvp in nonTestableTypes)
            {
                sb.AppendFormat("'{0}' cannot be easily abstracted in a testing context. Please make sure it either has a public constructor, or an empty protected constructor.{1}", 
                    kvp.Key, Environment.NewLine);

                foreach (string methodName in kvp.Value)
                {
                    sb.AppendFormat("    - Method '{0}' must be virtual{1}", methodName, Environment.NewLine);
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<string> GetNonVirtualPublicMethodsNames(Type type)
        {
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            return from mi in publicMethods where !mi.IsVirtual && !mi.IsStatic select mi.ToString();
        }

        private static bool HasEmptyProtectedConstructor(Type type)
        {
            ConstructorInfo[] nonPublicConstructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            
            return nonPublicConstructors.Any(ci => !ci.IsPrivate && !ci.IsAssembly && !ci.IsFinal && !ci.GetParameters().Any());
        }

        private static bool IsStatic(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }
    }
}
