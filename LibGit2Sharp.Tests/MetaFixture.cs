using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MetaFixture
    {
        private static readonly HashSet<Type> explicitOnlyInterfaces = new HashSet<Type>
        {
            typeof(IBelongToARepository), typeof(IDiffResult),
        };

        [Fact]
        public void LibGit2SharpPublicInterfacesCoverAllPublicMembers()
        {
            var methodsMissingFromInterfaces =
                from t in typeof(IRepository).GetTypeInfo().Assembly.GetExportedTypes()
                where !t.GetTypeInfo().IsInterface
                where t.GetTypeInfo().GetInterfaces().Any(i => i.GetTypeInfo().IsPublic && i.Namespace == typeof(IRepository).Namespace && !explicitOnlyInterfaces.Contains(i))
                let interfaceTargetMethods = from i in t.GetTypeInfo().GetInterfaces()
                                             from im in t.GetInterfaceMap(i).TargetMethods
                                             select im
                from tm in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                where !interfaceTargetMethods.Contains(tm)
                select t.Name + " has extra method " + tm.Name;

            Assert.Equal("", string.Join(Environment.NewLine,
                                         methodsMissingFromInterfaces.ToArray()));
        }

        [Fact]
        public void LibGit2SharpExplicitOnlyInterfacesAreIndeedExplicitOnly()
        {
            var methodsMissingFromInterfaces =
                from t in typeof(IRepository).GetTypeInfo().Assembly.GetExportedTypes()
                where t.GetInterfaces().Any(explicitOnlyInterfaces.Contains)
                let interfaceTargetMethods = from i in t.GetInterfaces()
                                             where explicitOnlyInterfaces.Contains(i)
                                             from im in t.GetTypeInfo().GetInterfaceMap(i).TargetMethods
                                             select im
                from tm in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                where interfaceTargetMethods.Contains(tm)
                select t.Name + " has public method " + tm.Name + " which should be explicitly implemented.";

            Assert.Equal("", string.Join(Environment.NewLine,
                                         methodsMissingFromInterfaces.ToArray()));
        }

        [Fact]
        public void PublicTestMethodsAreFactsOrTheories()
        {
            var exceptions = new[]
            {
                "LibGit2Sharp.Tests.FilterBranchFixture.Dispose",
            };

            var fixtures = from t in typeof(MetaFixture).GetTypeInfo().Assembly.GetExportedTypes()
                           where t.GetTypeInfo().IsPublic && !t.IsNested
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

            IEnumerable<Type> libGit2SharpTypes = typeof(IRepository).GetTypeInfo().Assembly.GetExportedTypes()
                .Where(t => t.GetTypeInfo().GetCustomAttributes(typeof(DebuggerDisplayAttribute), false).Length != 0);

            foreach (Type type in libGit2SharpTypes)
            {
                var debuggerDisplayAttribute = (DebuggerDisplayAttribute)type.GetTypeInfo().GetCustomAttributes(typeof(DebuggerDisplayAttribute), false).Single();

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

            if (typesWithDebuggerDisplayAndInvalidImplPattern.Count != 0)
            {
                Assert.Fail(Environment.NewLine + BuildMissingDebuggerDisplayPropertyMessage(typesWithDebuggerDisplayAndInvalidImplPattern));
            }
        }

        // Related to https://github.com/libgit2/libgit2sharp/pull/185
        [Fact]
        public void TypesInLibGit2SharpMustBeExtensibleInATestingContext()
        {
            var nonTestableTypes = new Dictionary<Type, IEnumerable<string>>();

            IEnumerable<Type> libGit2SharpTypes = typeof(IRepository).GetTypeInfo().Assembly.GetExportedTypes()
                .Where(t => MustBeMockable(t) && t.Namespace == typeof(IRepository).Namespace);

            foreach (Type type in libGit2SharpTypes)
            {
                if (type.GetTypeInfo().IsInterface || type.GetTypeInfo().IsEnum || IsStatic(type))
                    continue;

                var nonVirtualMethodNamesForType = GetNonVirtualPublicMethodsNames(type).ToList();
                if (nonVirtualMethodNamesForType.Count != 0)
                {
                    nonTestableTypes.Add(type, nonVirtualMethodNamesForType);
                    continue;
                }

                if (!HasEmptyPublicOrProtectedConstructor(type))
                {
                    nonTestableTypes.Add(type, new List<string>());
                }

                if (type.GetTypeInfo().IsAbstract)
                {
                    continue;
                }

                try
                {
                    if (type.GetTypeInfo().ContainsGenericParameters)
                    {
                        var constructType = type.MakeGenericType(Enumerable.Repeat(typeof(object), type.GetGenericArguments().Length).ToArray());
                        Activator.CreateInstance(constructType, true);
                    }
                    else
                    {
                        Activator.CreateInstance(type, true);
                    }
                }
                catch
                {
                    nonTestableTypes.Add(type, new List<string>());
                }
            }

            if (nonTestableTypes.Count != 0)
            {
                Assert.Fail(Environment.NewLine + BuildNonTestableTypesMessage(nonTestableTypes));
            }
        }

        private static bool MustBeMockable(Type type)
        {
            if (type.GetTypeInfo().IsSealed)
            {
                return false;
            }

            if (type.GetTypeInfo().IsAbstract)
            {
                return !type.GetTypeInfo().Assembly.GetExportedTypes()
                            .Where(t => t.GetTypeInfo().IsSubclassOf(type))
                            .All(t => t.GetTypeInfo().IsAbstract || t.GetTypeInfo().IsSealed);
            }

            return true;
        }


        [Fact]
        public void EnumsWithFlagsHaveMutuallyExclusiveValues()
        {
            var flagsEnums = typeof(IRepository).GetTypeInfo().Assembly.GetExportedTypes()
                .Where(t => t.GetTypeInfo().IsEnum && t.GetTypeInfo().GetCustomAttributes(typeof(FlagsAttribute), false).Length != 0);

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
            return type.GetTypeInfo().IsAbstract && type.GetTypeInfo().IsSealed;
        }

        // Related to https://github.com/libgit2/libgit2sharp/issues/644 and https://github.com/libgit2/libgit2sharp/issues/645
        [Fact]
        public void GetEnumeratorMethodsInLibGit2SharpMustBeVirtualForTestability()
        {
            var nonVirtualGetEnumeratorMethods = typeof(IRepository).GetTypeInfo().Assembly
                .GetExportedTypes()
                .Where(t =>
                    t.Namespace == typeof(IRepository).Namespace &&
                    !t.GetTypeInfo().IsSealed &&
                    !t.GetTypeInfo().IsAbstract &&
                    t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IEnumerable<>))))
                .Select(t => t.GetMethod("GetEnumerator"))
                .Where(m =>
                    m.ReturnType.Name == "IEnumerator`1" &&
                    (!m.IsVirtual || m.IsFinal))
                .ToList();

            if (nonVirtualGetEnumeratorMethods.Count != 0)
            {
                var sb = new StringBuilder();

                foreach (var method in nonVirtualGetEnumeratorMethods)
                {
                    sb.AppendFormat("GetEnumerator in type '{0}' isn't virtual.{1}",
                        method.DeclaringType, Environment.NewLine);
                }

                Assert.Fail(Environment.NewLine + sb.ToString());
            }
        }

        [Fact]
        public void NoPublicTypesUnderLibGit2SharpCoreNamespace()
        {
            const string coreNamespace = "LibGit2Sharp.Core";

            var types = typeof(IRepository).GetTypeInfo().Assembly
                .GetExportedTypes()
                .Where(t => t.FullName.StartsWith(coreNamespace + "."))

                // Ugly hack to circumvent a Mono bug
                // cf. https://bugzilla.xamarin.com/show_bug.cgi?id=27010
                .Where(t => !t.FullName.Contains("+"))
                .Where(t => t.FullName != "LibGit2Sharp.Core.LeaksContainer")
                .ToList();

            if (types.Count != 0)
            {
                var sb = new StringBuilder();

                foreach (var type in types)
                {
                    sb.AppendFormat("Public type '{0}' under the '{1}' namespace.{2}",
                        type.FullName, coreNamespace, Environment.NewLine);
                }

                Assert.Fail(Environment.NewLine + sb.ToString());
            }
        }

        [Fact]
        public void NoOptionalParametersinMethods()
        {
            IEnumerable<string> mis =
                from t in typeof(IRepository).GetTypeInfo().Assembly
                    .GetExportedTypes()
                from m in t.GetMethods()
                where !m.IsObsolete()
                from p in m.GetParameters()
                where p.IsOptional
                select m.DeclaringType + "." + m.Name;

            var sb = new StringBuilder();

            foreach (var method in mis.Distinct())
            {
                sb.AppendFormat("At least one overload of method '{0}' accepts an optional parameter.{1}",
                    method, Environment.NewLine);
            }

            Assert.Equal("", sb.ToString());
        }

        [Fact]
        public void NoOptionalParametersinConstructors()
        {
            IEnumerable<string> mis =
                from t in typeof(IRepository).GetTypeInfo().Assembly
                    .GetExportedTypes()
                from c in t.GetConstructors()
                from p in c.GetParameters()
                where p.IsOptional
                select c.DeclaringType.Name;

            var sb = new StringBuilder();

            foreach (var method in mis.Distinct())
            {
                sb.AppendFormat("At least one constructor of type '{0}' accepts an optional parameter.{1}",
                    method, Environment.NewLine);
            }

            Assert.Equal("", sb.ToString());
        }

        [Fact]
        public void PublicExtensionMethodsShouldonlyTargetInterfacesOrEnums()
        {
            IEnumerable<string> mis =
                from m in GetInvalidPublicExtensionMethods()
                select m.DeclaringType + "." + m.Name;

            var sb = new StringBuilder();

            foreach (var method in mis.Distinct())
            {
                sb.AppendFormat("'{0}' is a public extension method that doesn't target an interface or an enum.{1}",
                    method, Environment.NewLine);
            }

            Assert.Equal("", sb.ToString());
        }

        // Inspired from http://stackoverflow.com/a/299526

        static IEnumerable<MethodInfo> GetInvalidPublicExtensionMethods()
        {
            var query = from type in typeof(IRepository).GetTypeInfo().Assembly.GetTypes()
                        where type.GetTypeInfo().IsSealed && !type.GetTypeInfo().IsGenericType && !type.IsNested && type.GetTypeInfo().IsPublic
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        let parameterType = method.GetParameters()[0].ParameterType
                        where parameterType != null && !parameterType.GetTypeInfo().IsInterface && !parameterType.GetTypeInfo().IsEnum
                        select method;
            return query;
        }

        [Fact]
        public void AllIDiffResultsAreInChangesBuilder()
        {
            var diff = typeof(Diff).GetField("ChangesBuilders", BindingFlags.NonPublic | BindingFlags.Static);
            var changesBuilders = (System.Collections.IDictionary)diff.GetValue(null);

            IEnumerable<Type> diffResults = typeof(Diff).GetTypeInfo().Assembly.GetExportedTypes()
                .Where(type => type.GetTypeInfo().GetInterface("IDiffResult") != null);

            var nonBuilderTypes = diffResults.Where(diffResult => !changesBuilders.Contains(diffResult));
            Assert.False(nonBuilderTypes.Any(), "Classes which implement IDiffResult but are not registered under ChangesBuilders in Diff:" + Environment.NewLine +
                string.Join(Environment.NewLine, nonBuilderTypes.Select(type => type.FullName)));
        }
    }

    internal static class TypeExtensions
    {
        internal static bool IsObsolete(this MethodInfo methodInfo)
        {
            var attributes = methodInfo.GetCustomAttributes(false);
            return attributes.Any(a => a is ObsoleteAttribute);
        }
    }
}
