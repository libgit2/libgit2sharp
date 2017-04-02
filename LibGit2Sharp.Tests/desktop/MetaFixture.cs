using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public partial class MetaFixture
    {
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
    }
}
