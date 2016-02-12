using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ShadowCopyFixture : BaseFixture
    {
        [Fact]
        public void CanProbeForNativeBinariesFromAShadowCopiedAssembly()
        {
            Type type = typeof(Wrapper);
            Assembly assembly = type.Assembly;

            // Build a new domain which will shadow copy assemblies
            string cachePath = Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName());
            Directory.CreateDirectory(cachePath);

            var setup = new AppDomainSetup
            {
                ApplicationBase = Path.GetDirectoryName(new Uri(assembly.CodeBase).LocalPath),
                ApplicationName = "ShadowWalker",
                ShadowCopyFiles = "true",
                CachePath = cachePath
            };

            setup.ShadowCopyDirectories = setup.ApplicationBase;

            AppDomain domain = AppDomain.CreateDomain(
                setup.ApplicationName,
                null,
                setup, new PermissionSet(PermissionState.Unrestricted));

            // Instantiate from the remote domain
            var wrapper = (Wrapper)domain.CreateInstanceAndUnwrap(assembly.FullName, type.FullName);

            // Ensure that LibGit2Sharp correctly probes for the native binaries
            // from the other domain
            string repoPath = BuildSelfCleaningDirectory().DirectoryPath;
            wrapper.CanInitANewRepositoryFromAShadowCopiedAssembly(repoPath);

            Assembly sourceAssembly = typeof(IRepository).Assembly;

            // Ensure both assemblies share the same escaped code base...
            string cachedAssemblyEscapedCodeBase = wrapper.AssemblyEscapedCodeBase;
            Assert.Equal(sourceAssembly.EscapedCodeBase, cachedAssemblyEscapedCodeBase);

            // ...but are currently loaded from different locations...
            string cachedAssemblyLocation = wrapper.AssemblyLocation;
            Assert.NotEqual(sourceAssembly.Location, cachedAssemblyLocation);

            // ...that the assembly in the other domain is stored in the shadow copy cache...
            string cachedAssembliesPath = Path.Combine(setup.CachePath, setup.ApplicationName);
            Assert.True(cachedAssemblyLocation.StartsWith(cachedAssembliesPath));

            if (!Constants.IsRunningOnUnix)
            {
                // ...that this cache doesn't contain the `NativeBinaries` folder
                string cachedAssemblyParentPath = Path.GetDirectoryName(cachedAssemblyLocation);
                Assert.False(Directory.Exists(Path.Combine(cachedAssemblyParentPath, "NativeBinaries")));

                // ...whereas `NativeBinaries` of course exists next to the source assembly
                string sourceAssemblyParentPath =
                    Path.GetDirectoryName(new Uri(sourceAssembly.EscapedCodeBase).LocalPath);
                Assert.True(Directory.Exists(Path.Combine(sourceAssemblyParentPath, "NativeBinaries")));
            }

            AppDomain.Unload(domain);
        }

        public class Wrapper : MarshalByRefObject
        {
            private readonly Assembly assembly;
            public Wrapper()
            {
                assembly = typeof(IRepository).Assembly;
            }

            public void CanInitANewRepositoryFromAShadowCopiedAssembly(string path)
            {
                var gitDirPath = Repository.Init(path);

                using (var repo = new Repository(gitDirPath))
                {
                    Assert.NotNull(repo);
                }
            }

            public string AssemblyLocation
            {
                get
                {
                    return assembly.Location;
                }
            }

            public string AssemblyEscapedCodeBase
            {
                get
                {
                    return assembly.EscapedCodeBase;
                }
            }
        }
    }
}
