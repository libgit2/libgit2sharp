using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Handlers;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class CloneFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void CanClone(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);

                Assert.True(File.Exists(Path.Combine(scd.RootedDirectoryPath, "master.txt")));
                Assert.Equal("master", repo.Head.FriendlyName);
                Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", repo.Head.Tip.Id.ToString());
            }
        }

        [Theory]
        [InlineData("https://github.com/libgit2/TestGitRepository", 1)]
        [InlineData("https://github.com/libgit2/TestGitRepository", 5)]
        [InlineData("https://github.com/libgit2/TestGitRepository", 7)]
        public void CanCloneShallow(string url, int depth)
        {
            var scd = BuildSelfCleaningDirectory();

            var clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions
            {
                FetchOptions =
                {
                    Depth = depth,
                },
            });

            using (var repo = new Repository(clonedRepoPath))
            {
                var commitsFirstParentOnly = repo.Commits.QueryBy(new CommitFilter
                {
                    FirstParentOnly = true,
                });

                Assert.Equal(depth, commitsFirstParentOnly.Count());
                Assert.Equal("49322bb17d3acc9146f98c97d078513228bbf3c0", repo.Head.Tip.Id.ToString());
            }
        }

        [Theory]
        [InlineData("br2", "a4a7dce85cf63874e984719f4fdd239f5145052f")]
        [InlineData("packed", "41bc8c69075bbdb46c5c6f0566cc8cc5b46e8bd9")]
        [InlineData("test", "e90810b8df3e80c413d903f631643c716887138d")]
        public void CanCloneWithCheckoutBranchName(string branchName, string headTipId)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(BareTestRepoPath, scd.DirectoryPath, new CloneOptions { BranchName = branchName });

            using (var repo = new Repository(clonedRepoPath))
            {
                var head = repo.Head;

                Assert.Equal(branchName, head.FriendlyName);
                Assert.True(head.IsTracking);
                Assert.Equal(headTipId, head.Tip.Sha);
            }
        }

        private void AssertLocalClone(string url, string path = null, bool isCloningAnEmptyRepository = false)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var clonedRepo = new Repository(clonedRepoPath))
            using (var originalRepo = new Repository(path ?? url))
            {
                Assert.NotEqual(originalRepo.Info.Path, clonedRepo.Info.Path);
                Assert.Equal(originalRepo.Head, clonedRepo.Head);

                Assert.Equal(originalRepo.Branches.Count(), clonedRepo.Branches.Count(b => b.IsRemote && b.FriendlyName != "origin/HEAD"));
                Assert.Equal(isCloningAnEmptyRepository ? 0 : 1, clonedRepo.Branches.Count(b => !b.IsRemote));

                Assert.Equal(originalRepo.Tags.Count(), clonedRepo.Tags.Count());
                Assert.Single(clonedRepo.Network.Remotes);
            }
        }

        [Fact]
        public void CanCloneALocalRepositoryFromALocalUri()
        {
            var uri = new Uri($"file://{Path.GetFullPath(BareTestRepoPath)}");
            AssertLocalClone(uri.AbsoluteUri, BareTestRepoPath);
        }

        [Fact]
        public void CanCloneALocalRepositoryFromAStandardPath()
        {
            AssertLocalClone(BareTestRepoPath);
        }

        [Fact]
        public void CanCloneALocalRepositoryFromANewlyCreatedTemporaryPath()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory(path);
            Repository.Init(scd.DirectoryPath);
            AssertLocalClone(scd.DirectoryPath, isCloningAnEmptyRepository: true);
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void CanCloneBarely(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions
            {
                IsBare = true
            });

            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.Null(repo.Info.WorkingDirectory);
                Assert.Equal(scd.RootedDirectoryPath + Path.DirectorySeparatorChar, repo.Info.Path);
                Assert.True(repo.Info.IsBare);
            }
        }

        [Theory]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void WontCheckoutIfAskedNotTo(string url)
        {
            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
            {
                Checkout = false
            });

            using (var repo = new Repository(clonedRepoPath))
            {
                Assert.False(File.Exists(Path.Combine(repo.Info.WorkingDirectory, "master.txt")));
            }
        }

        [Theory]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void CallsProgressCallbacks(string url)
        {
            bool transferWasCalled = false;
            bool progressWasCalled = false;
            bool updateTipsWasCalled = false;
            bool checkoutWasCalled = false;

            var scd = BuildSelfCleaningDirectory();

            Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
            {
                FetchOptions =
                {
                    OnTransferProgress = _ => { transferWasCalled = true; return true; },
                    OnProgress = progress => { progressWasCalled = true; return true; },
                    OnUpdateTips = (name, oldId, newId) => { updateTipsWasCalled = true; return true; }
                },
                OnCheckoutProgress = (a, b, c) => checkoutWasCalled = true

            });

            Assert.True(transferWasCalled);
            Assert.True(progressWasCalled);
            Assert.True(updateTipsWasCalled);
            Assert.True(checkoutWasCalled);
        }

        [SkippableFact]
        public void CanCloneWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(Constants.PrivateRepoUrl, scd.DirectoryPath,
                new CloneOptions()
                {
                    FetchOptions = { CredentialsProvider = Constants.PrivateRepoCredentials }
                });


            using (var repo = new Repository(clonedRepoPath))
            {
                string dir = repo.Info.Path;
                Assert.True(Path.IsPathRooted(dir));
                Assert.True(Directory.Exists(dir));

                Assert.NotNull(repo.Info.WorkingDirectory);
                Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
                Assert.False(repo.Info.IsBare);
            }
        }

        static Credentials CreateUsernamePasswordCredentials(string user, string pass, bool secure)
        {
            if (secure)
            {
                return new SecureUsernamePasswordCredentials
                {
                    Username = user,
                    Password = Constants.StringToSecureString(pass),
                };
            }

            return new UsernamePasswordCredentials
            {
                Username = user,
                Password = pass,
            };
        }

        //[Theory]
        //[InlineData("https://libgit2@bitbucket.org/libgit2/testgitrepository.git", "libgit3", "libgit3", true)]
        //[InlineData("https://libgit2@bitbucket.org/libgit2/testgitrepository.git", "libgit3", "libgit3", false)]
        //public void CanCloneFromBBWithCredentials(string url, string user, string pass, bool secure)
        //{
        //    var scd = BuildSelfCleaningDirectory();

        //    string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, new CloneOptions()
        //    {
        //        CredentialsProvider = (_url, _user, _cred) => CreateUsernamePasswordCredentials(user, pass, secure)
        //    });

        //    using (var repo = new Repository(clonedRepoPath))
        //    {
        //        string dir = repo.Info.Path;
        //        Assert.True(Path.IsPathRooted(dir));
        //        Assert.True(Directory.Exists(dir));

        //        Assert.NotNull(repo.Info.WorkingDirectory);
        //        Assert.Equal(Path.Combine(scd.RootedDirectoryPath, ".git" + Path.DirectorySeparatorChar), repo.Info.Path);
        //        Assert.False(repo.Info.IsBare);
        //    }
        //}

        [SkippableTheory]
        [InlineData("https://github.com/libgit2/TestGitRepository.git", "github.com", typeof(CertificateX509))]
        //[InlineData("git@github.com:libgit2/TestGitRepository.git", "github.com", typeof(CertificateSsh))]
        public void CanInspectCertificateOnClone(string url, string hostname, Type certType)
        {
            var scd = BuildSelfCleaningDirectory();

            InconclusiveIf(
                () =>
                    certType == typeof(CertificateSsh) && !GlobalSettings.Version.Features.HasFlag(BuiltInFeatures.Ssh),
                "SSH not supported");

            bool wasCalled = false;
            bool checksHappy = false;

            var options = new CloneOptions
            {
                FetchOptions =
                {
                    CertificateCheck = (cert, valid, host) =>
                    {
                        wasCalled = true;

                        Assert.Equal(hostname, host);
                        Assert.Equal(certType, cert.GetType());

                        if (certType == typeof(CertificateX509))
                        {
                            Assert.True(valid);
                            var x509 = ((CertificateX509)cert).Certificate;
                            // we get a string with the different fields instead of a structure, so...
                            Assert.Contains("CN=github.com", x509.Subject);
                            checksHappy = true;
                            return false;
                        }

                        if (certType == typeof(CertificateSsh))
                        {
                            var hostkey = (CertificateSsh)cert;
                            Assert.True(hostkey.HasMD5);
                            /*
                             * Once you've connected and thus your ssh has stored the hostkey,
                             * you can get the hostkey for a host with
                             *
                             *     ssh-keygen -F github.com -l | tail -n 1 | cut -d ' ' -f 2 | tr -d ':'
                             *
                             * though GitHub's hostkey won't change anytime soon.
                             */
                            Assert.Equal("1627aca576282d36631b564debdfa648",
                                BitConverter.ToString(hostkey.HashMD5).ToLower().Replace("-", ""));
                            checksHappy = true;
                            return false;
                        }

                        return false;
                    }
                }
            };

            Assert.Throws<UserCancelledException>(() =>
                Repository.Clone(url, scd.DirectoryPath, options)
            );

            Assert.True(wasCalled);
            Assert.True(checksHappy);
        }

        [Theory]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        public void CloningWithoutWorkdirPathThrows(string url)
        {
            Assert.Throws<ArgumentNullException>(() => Repository.Clone(url, null));
        }

        [Fact]
        public void CloningWithoutUrlThrows()
        {
            var scd = BuildSelfCleaningDirectory();

            Assert.Throws<ArgumentNullException>(() => Repository.Clone(null, scd.DirectoryPath));
        }

        /// <summary>
        /// Private helper to record the callbacks that were called as part of a clone.
        /// </summary>
        private class CloneCallbackInfo
        {
            /// <summary>
            /// Was checkout progress called.
            /// </summary>
            public bool CheckoutProgressCalled { get; set; }

            /// <summary>
            /// The reported remote URL.
            /// </summary>
            public string RemoteUrl { get; set; }

            /// <summary>
            /// Was remote ref update called.
            /// </summary>
            public bool RemoteRefUpdateCalled { get; set; }

            /// <summary>
            /// Was the transition callback called when starting
            /// work on this repository.
            /// </summary>
            public bool StartingWorkInRepositoryCalled { get; set; }

            /// <summary>
            /// Was the transition callback called when finishing
            /// work on this repository.
            /// </summary>
            public bool FinishedWorkInRepositoryCalled { get; set; }

            /// <summary>
            /// The reported recursion depth.
            /// </summary>
            public int RecursionDepth { get; set; }
        }

        [Fact]
        public void CanRecursivelyCloneSubmodules()
        {
            var uri = new Uri($"file://{Path.GetFullPath(SandboxSubmoduleSmallTestRepo())}");
            var scd = BuildSelfCleaningDirectory();
            string relativeSubmodulePath = "submodule_target_wd";

            // Construct the expected URL the submodule will clone from.
            string expectedSubmoduleUrl = Path.Combine(Path.GetDirectoryName(uri.AbsolutePath), relativeSubmodulePath);
            expectedSubmoduleUrl = expectedSubmoduleUrl.Replace('\\', '/');

            Dictionary<string, CloneCallbackInfo> callbacks = new Dictionary<string, CloneCallbackInfo>();

            CloneCallbackInfo currentEntry = null;
            bool unexpectedOrderOfCallbacks = false;

            CheckoutProgressHandler checkoutProgressHandler = (x, y, z) =>
                {
                    if (currentEntry != null)
                    {
                        currentEntry.CheckoutProgressCalled = true;
                    }
                    else
                    {
                        // Should not be called if there is not a current
                        // callbackInfo entry.
                        unexpectedOrderOfCallbacks = true;
                    }
                };

            UpdateTipsHandler remoteRefUpdated = (x, y, z) =>
            {
                if (currentEntry != null)
                {
                    currentEntry.RemoteRefUpdateCalled = true;
                }
                else
                {
                    // Should not be called if there is not a current
                    // callbackInfo entry.
                    unexpectedOrderOfCallbacks = true;
                }

                return true;
            };

            RepositoryOperationStarting repositoryOperationStarting = (x) =>
                {
                    if (currentEntry != null)
                    {
                        // Should not be called if there is a current
                        // callbackInfo entry.
                        unexpectedOrderOfCallbacks = true;
                    }

                    currentEntry = new CloneCallbackInfo();
                    currentEntry.StartingWorkInRepositoryCalled = true;
                    currentEntry.RecursionDepth = x.RecursionDepth;
                    currentEntry.RemoteUrl = x.RemoteUrl;
                    callbacks.Add(x.RepositoryPath, currentEntry);

                    return true;
                };

            RepositoryOperationCompleted repositoryOperationCompleted = (x) =>
                {
                    if (currentEntry != null)
                    {
                        currentEntry.FinishedWorkInRepositoryCalled = true;
                        currentEntry = null;
                    }
                    else
                    {
                        // Should not be called if there is not a current
                        // callbackInfo entry.
                        unexpectedOrderOfCallbacks = true;
                    }
                };

            CloneOptions options = new CloneOptions()
            {
                RecurseSubmodules = true,
                OnCheckoutProgress = checkoutProgressHandler,
                FetchOptions =
                {
                    OnUpdateTips = remoteRefUpdated,
                    RepositoryOperationStarting = repositoryOperationStarting,
                    RepositoryOperationCompleted = repositoryOperationCompleted
                }
            };

            string clonedRepoPath = Repository.Clone(uri.AbsolutePath, scd.DirectoryPath, options);
            string workDirPath;

            using (Repository repo = new Repository(clonedRepoPath))
            {
                workDirPath = repo.Info.WorkingDirectory.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            }

            // Verification:
            // Verify that no callbacks were called in an unexpected order.
            Assert.False(unexpectedOrderOfCallbacks);

            Dictionary<string, CloneCallbackInfo> expectedCallbackInfo = new Dictionary<string, CloneCallbackInfo>();
            expectedCallbackInfo.Add(workDirPath, new CloneCallbackInfo()
            {
                RecursionDepth = 0,
                RemoteUrl = uri.AbsolutePath,
                StartingWorkInRepositoryCalled = true,
                FinishedWorkInRepositoryCalled = true,
                CheckoutProgressCalled = true,
                RemoteRefUpdateCalled = true,
            });

            expectedCallbackInfo.Add(Path.Combine(workDirPath, relativeSubmodulePath), new CloneCallbackInfo()
            {
                RecursionDepth = 1,
                RemoteUrl = expectedSubmoduleUrl,
                StartingWorkInRepositoryCalled = true,
                FinishedWorkInRepositoryCalled = true,
                CheckoutProgressCalled = true,
                RemoteRefUpdateCalled = true,
            });

            // Callbacks for each expected repository that is cloned
            foreach (KeyValuePair<string, CloneCallbackInfo> kvp in expectedCallbackInfo)
            {
                CloneCallbackInfo entry = null;
                Assert.True(callbacks.TryGetValue(kvp.Key, out entry), string.Format("{0} was not found in callbacks.", kvp.Key));

                Assert.Equal(kvp.Value.RemoteUrl, entry.RemoteUrl);
                Assert.Equal(kvp.Value.RecursionDepth, entry.RecursionDepth);
                Assert.Equal(kvp.Value.StartingWorkInRepositoryCalled, entry.StartingWorkInRepositoryCalled);
                Assert.Equal(kvp.Value.FinishedWorkInRepositoryCalled, entry.FinishedWorkInRepositoryCalled);
                Assert.Equal(kvp.Value.CheckoutProgressCalled, entry.CheckoutProgressCalled);
                Assert.Equal(kvp.Value.RemoteRefUpdateCalled, entry.RemoteRefUpdateCalled);
            }

            // Verify the state of the submodule
            using (Repository repo = new Repository(clonedRepoPath))
            {
                var sm = repo.Submodules[relativeSubmodulePath];
                Assert.True(sm.RetrieveStatus().HasFlag(SubmoduleStatus.InWorkDir |
                                                        SubmoduleStatus.InConfig |
                                                        SubmoduleStatus.InIndex |
                                                        SubmoduleStatus.InHead));

                Assert.NotNull(sm.HeadCommitId);
                Assert.Equal("480095882d281ed676fe5b863569520e54a7d5c0", sm.HeadCommitId.Sha);

                Assert.False(repo.RetrieveStatus().IsDirty);
            }
        }

        [Fact]
        public void CanCancelRecursiveClone()
        {
            var uri = new Uri($"file://{Path.GetFullPath(SandboxSubmoduleSmallTestRepo())}");
            var scd = BuildSelfCleaningDirectory();
            string relativeSubmodulePath = "submodule_target_wd";

            int cancelDepth = 0;

            RepositoryOperationStarting repositoryOperationStarting = (x) =>
            {
                return !(x.RecursionDepth >= cancelDepth);
            };

            CloneOptions options = new CloneOptions()
            {
                RecurseSubmodules = true,
                FetchOptions = { RepositoryOperationStarting = repositoryOperationStarting }
            };

            Assert.Throws<UserCancelledException>(() =>
                Repository.Clone(uri.AbsolutePath, scd.DirectoryPath, options));

            // Cancel after super repository is cloned, but before submodule is cloned.
            cancelDepth = 1;

            string clonedRepoPath = null;

            try
            {
                Repository.Clone(uri.AbsolutePath, scd.DirectoryPath, options);
            }
            catch (RecurseSubmodulesException ex)
            {
                Assert.NotNull(ex.InnerException);
                Assert.Equal(typeof(UserCancelledException), ex.InnerException.GetType());
                clonedRepoPath = ex.InitialRepositoryPath;
            }

            // Verify that the submodule was not initialized.
            using (Repository repo = new Repository(clonedRepoPath))
            {
                var submoduleStatus = repo.Submodules[relativeSubmodulePath].RetrieveStatus();
                Assert.Equal(SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.WorkDirUninitialized,
                             submoduleStatus);

            }
        }

        [Fact]
        public void CannotCloneWithForbiddenCustomHeaders()
        {
            var scd = BuildSelfCleaningDirectory();

            const string url = "https://github.com/libgit2/TestGitRepository";

            const string knownHeader = "User-Agent: mygit-201";
            var cloneOptions = new CloneOptions();
            cloneOptions.FetchOptions.CustomHeaders = new string[] { knownHeader };

            Assert.Throws<LibGit2SharpException>(() => Repository.Clone(url, scd.DirectoryPath, cloneOptions));
        }

        [Fact]
        public void CannotCloneWithMalformedCustomHeaders()
        {
            var scd = BuildSelfCleaningDirectory();

            const string url = "https://github.com/libgit2/TestGitRepository";

            const string knownHeader = "hello world";
            var cloneOptions = new CloneOptions();
            cloneOptions.FetchOptions.CustomHeaders = new string[] { knownHeader };

            Assert.Throws<LibGit2SharpException>(() => Repository.Clone(url, scd.DirectoryPath, cloneOptions));
        }

        [Fact]
        public void CanCloneWithCustomHeaders()
        {
            var scd = BuildSelfCleaningDirectory();

            const string url = "https://github.com/libgit2/TestGitRepository";

            const string knownHeader = "X-Hello: world";
            var cloneOptions = new CloneOptions();
            cloneOptions.FetchOptions.CustomHeaders = new string[] { knownHeader };

            var clonedRepoPath = Repository.Clone(url, scd.DirectoryPath, cloneOptions);
            Assert.True(Directory.Exists(clonedRepoPath));
        }
    }
}
