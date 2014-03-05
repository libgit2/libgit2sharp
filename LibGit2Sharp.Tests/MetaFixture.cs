using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class MetaFixture
    {
        [Fact]
        public void PublicTestMethodsAreFactsOrTheories()
        {
            var exceptions = new[]
            {
                "LibGit2Sharp.Tests.FilterBranchFixture.Dispose",
            };

            var fixtures = from t in Assembly.GetAssembly(typeof(MetaFixture)).GetExportedTypes()
                           where t.IsPublic && !t.IsNested
                           where t.Namespace != typeof(BaseFixture).Namespace // Exclude helpers
                           let methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                           from m in methods
                           where !m.GetCustomAttributes(typeof(FactAttribute), false)
                                   .Concat(m.GetCustomAttributes(typeof(TheoryAttribute), false))
                                   .Any()
                           let name = t.FullName + "." + m.Name
                           where !exceptions.Contains(name)
                           select name;

            Assert.Equal("", string.Join(Environment.NewLine, fixtures.ToArray()));
        }

        // Related to https://github.com/libgit2/libgit2sharp/pull/251
        [Fact]
        public void TypesInLibGit2DecoratedWithDebuggerDisplayMustFollowTheStandardImplPattern()
        {
            var typesWithDebuggerDisplayAndInvalidImplPattern = new List<Type>();

            IEnumerable<Type> libGit2SharpTypes = Assembly.GetAssembly(typeof(IRepository)).GetExportedTypes()
                .Where(t => t.GetCustomAttributes(typeof(DebuggerDisplayAttribute), false).Any());

            foreach (Type type in libGit2SharpTypes)
            {
                var debuggerDisplayAttribute = (DebuggerDisplayAttribute)type.GetCustomAttributes(typeof(DebuggerDisplayAttribute), false).Single();

                if (debuggerDisplayAttribute.Value != "{DebuggerDisplay,nq}")
                {
                    typesWithDebuggerDisplayAndInvalidImplPattern.Add(type);
                    continue;
                }

                PropertyInfo debuggerDisplayProperty = type.GetProperty("DebuggerDisplay",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (debuggerDisplayProperty == null)
                {
                    typesWithDebuggerDisplayAndInvalidImplPattern.Add(type);
                    continue;
                }

                if (debuggerDisplayProperty.PropertyType != typeof(string))
                {
                    typesWithDebuggerDisplayAndInvalidImplPattern.Add(type);
                }
            }

            if (typesWithDebuggerDisplayAndInvalidImplPattern.Any())
            {
                Assert.True(false, Environment.NewLine + BuildMissingDebuggerDisplayPropertyMessage(typesWithDebuggerDisplayAndInvalidImplPattern));
            }
        }

        // Related to https://github.com/libgit2/libgit2sharp/pull/185
        [Fact]
        public void TypesInLibGit2SharpMustBeExtensibleInATestingContext()
        {
            var nonTestableTypes = new Dictionary<Type, IEnumerable<string>>();

            IEnumerable<Type> libGit2SharpTypes = Assembly.GetAssembly(typeof(IRepository)).GetExportedTypes()
                .Where(t => !t.IsSealed && t.Namespace == typeof(IRepository).Namespace);

            foreach (Type type in libGit2SharpTypes)
            {
                if (type.IsInterface || type.IsEnum || IsStatic(type))
                    continue;

                var nonVirtualMethodNamesForType = GetNonVirtualPublicMethodsNames(type).ToList();
                if (nonVirtualMethodNamesForType.Any())
                {
                    nonTestableTypes.Add(type, nonVirtualMethodNamesForType);
                    continue;
                }

                if (!HasEmptyPublicOrProtectedConstructor(type))
                {
                    nonTestableTypes.Add(type, new List<string>());
                }
            }

            if (nonTestableTypes.Any())
            {
                Assert.True(false, Environment.NewLine + BuildNonTestableTypesMessage(nonTestableTypes));
            }
        }

        [Fact]
        public void LibGit2SharpInterfacesCoverAllPublicMembers()
        {
            var methodsMissingFromInterfaces =
                from t in Assembly.GetAssembly(typeof(IRepository)).GetExportedTypes()
                where !t.IsInterface
                where t.GetInterfaces().Any(i => i.Namespace == typeof(IRepository).Namespace)
                let interfaceTargetMethods = from i in t.GetInterfaces()
                                             from im in t.GetInterfaceMap(i).TargetMethods
                                             select im
                from tm in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                where !interfaceTargetMethods.Contains(tm)
                select t.Name + " has extra method " + tm.Name;

            Assert.Equal("", string.Join(Environment.NewLine,
                                         methodsMissingFromInterfaces.ToArray()));
        }

        [Fact]
        public void EnumsWithFlagsHaveMutuallyExclusiveValues()
        {
            var flagsEnums = Assembly.GetAssembly(typeof(IRepository)).GetExportedTypes()
                                     .Where(t => t.IsEnum && t.GetCustomAttributes(typeof(FlagsAttribute), false).Any());

            var overlaps = from t in flagsEnums
                           from int x in Enum.GetValues(t)
                           where x != 0
                           from int y in Enum.GetValues(t)
                           where y != 0
                           where x != y && (x & y) == y
                           select string.Format("{0}.{1} overlaps with {0}.{2}", t.Name, Enum.ToObject(t, x), Enum.ToObject(t, y));

            var message = string.Join(Environment.NewLine, overlaps.ToArray());

            Assert.Equal("", message);
        }

        private string BuildMissingDebuggerDisplayPropertyMessage(IEnumerable<Type> typesWithDebuggerDisplayAndInvalidImplPattern)
        {
            var sb = new StringBuilder();

            foreach (Type type in typesWithDebuggerDisplayAndInvalidImplPattern)
            {
                sb.AppendFormat("'{0}' is decorated with the DebuggerDisplayAttribute, but does not follow LibGit2Sharp implementation pattern.{1}" +
                                "   Please make sure that the type is decorated with `[DebuggerDisplay(\"{{DebuggerDisplay,nq}}\")]`,{1}" +
                                "   and that the type implements a private property named `DebuggerDisplay`, returning a string.{1}",
                    type.Name, Environment.NewLine);
            }

            return sb.ToString();
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

        private static bool HasEmptyPublicOrProtectedConstructor(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            return constructors.Any(ci => ci.GetParameters().Length == 0 && (ci.IsPublic || ci.IsFamily || ci.IsFamilyOrAssembly));
        }

        private static bool IsStatic(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        // Related to https://github.com/libgit2/libgit2sharp/issues/644 and https://github.com/libgit2/libgit2sharp/issues/645
        [Fact]
        public void GetEnumeratorMethodsInLibGit2SharpMustBeVirtualForTestability()
        {
            var nonVirtualGetEnumeratorMethods = Assembly.GetAssembly(typeof(IRepository))
                .GetExportedTypes()
                .Where(t =>
                    t.Namespace == typeof (IRepository).Namespace &&
                    !t.IsSealed &&
                    !t.IsAbstract &&
                    t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IEnumerable<>))))
                .Select(t => t.GetMethod("GetEnumerator"))
                .Where(m => 
                    m.ReturnType.Name == "IEnumerator`1" &&
                    (!m.IsVirtual || m.IsFinal))
                .ToList();

            foreach (var method in nonVirtualGetEnumeratorMethods)
            {
                Debug.WriteLine(String.Format("GetEnumerator in type '{0}' isn't virtual.", method.DeclaringType));
            }

            Assert.Empty(nonVirtualGetEnumeratorMethods);
        }
    }
}
