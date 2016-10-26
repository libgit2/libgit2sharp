using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MergeDriverFixture : BaseFixture
    {
        [Fact]
        public void CanRegisterAndUnregisterTheSameMergeDriver()
        {
            var driverName = "mergedriver_SameMergeDriverIsEqual";
            var mergeDriver = new EmptyMergeDriver(driverName);

            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);
            GlobalSettings.DeregisterMergeDriver(registration);

            var secondRegistration = GlobalSettings.RegisterMergeDriver(mergeDriver);
            GlobalSettings.DeregisterMergeDriver(secondRegistration);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var driverName = "mergedriver_CanRegisterAndDeregisterAfterGarbageCollection";
            var registration = GlobalSettings.RegisterMergeDriver(new EmptyMergeDriver(driverName));
            GC.Collect();
            GlobalSettings.DeregisterMergeDriver(registration);
        }

        [Fact]
        public void SameMergeDriverIsEqual()
        {
            var driverName = "mergedriver_SameMergeDriverIsEqual";
            var mergeDriver = new EmptyMergeDriver(driverName);
            Assert.Equal(mergeDriver, mergeDriver);
        }

        [Fact]
        public void InitCallbackNotMadeWhenMergeDriverNeverUsed()
        {
            var driverName = "mergedriver_InitCallbackNotMadeWhenMergeDriverNeverUsed";
            bool called = false;
            Action initializeCallback = () =>
            {
                called = true;
            };

            var driver = new FakeMergeDriver(driverName, initializeCallback);
            var registration = GlobalSettings.RegisterMergeDriver(driver);

            try
            {
                Assert.False(called);
            }
            finally
            {
                GlobalSettings.DeregisterMergeDriver(registration);
            }
        }

        [Fact]
        public void WhenMergingApplyIsCalledWhenThereIsAConflict()
        {
            var driverName = "mergedriver_WhenMergingApplyIsCalledWhenThereIsAConflict";
            string repoPath = InitNewRepository();
            bool called = false;

            Func<MergeDriverSource, MergeDriverResult> apply = (source) =>
            {
                called = true;
                return new MergeDriverResult { Status = MergeStatus.Conflicts };
            };

            var mergeDriver = new FakeMergeDriver(driverName, applyCallback: apply);
            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);

            try
            {
                using (var repo = CreateTestRepository(repoPath, driverName))
                {
                    string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".atom", "file1");
                    var stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit", Constants.Signature, Constants.Signature);

                    var branch = repo.CreateBranch("second");

                    var id = Guid.NewGuid() + ".atom";
                    newFilePath = Touch(repo.Info.WorkingDirectory, id, "file2");
                    stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit in master", Constants.Signature, Constants.Signature);

                    Commands.Checkout(repo, branch.FriendlyName);

                    newFilePath = Touch(repo.Info.WorkingDirectory, id, "file3");
                    stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit in second branch", Constants.Signature, Constants.Signature);

                    var result = repo.Merge("master", Constants.Signature, new MergeOptions { CommitOnSuccess = false });
                    Assert.True(called);
                }
            }
            finally
            {
                GlobalSettings.DeregisterMergeDriver(registration);
            }
        }

        [Fact]
        public void MergeDriverCanFetchFileContents()
        {
            var driverName = "mergedriver_MergeDriverCanFetchFileContents";
            string repoPath = InitNewRepository();
            string contents = null;

            Func <MergeDriverSource, MergeDriverResult> apply = (source) =>
            {
                var repos = source.Repository;
                var blob = repos.Lookup<Blob>(source.Theirs.Id);
                var content = blob.GetContentStream();

                using (var ms = new MemoryStream())
                {
                    content.CopyTo(ms);
                    ms.Position = 0;
                    var reader = new StreamReader(ms, System.Text.Encoding.UTF8);
                    contents = reader.ReadToEnd();
                    content.Position = 0;
                }
                return new MergeDriverResult { Status = MergeStatus.UpToDate, Content = content };
            };

            var mergeDriver = new FakeMergeDriver(driverName, applyCallback: apply);
            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);

            try
            {
                using (var repo = CreateTestRepository(repoPath, driverName))
                {
                    string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".atom", "file1");
                    var stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit", Constants.Signature, Constants.Signature);

                    var branch = repo.CreateBranch("second");

                    var id = Guid.NewGuid() + ".atom";
                    newFilePath = Touch(repo.Info.WorkingDirectory, id, "file2");
                    stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit in master", Constants.Signature, Constants.Signature);

                    Commands.Checkout(repo, branch.FriendlyName);

                    newFilePath = Touch(repo.Info.WorkingDirectory, id, "file3");
                    stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit in second branch", Constants.Signature, Constants.Signature);

                    var result = repo.Merge("master", Constants.Signature, new MergeOptions { CommitOnSuccess = false });
                    Assert.Equal("file2", contents);
                }
            }
            finally
            {
                GlobalSettings.DeregisterMergeDriver(registration);
            }
        }

        private static FileInfo CommitFileOnBranch(Repository repo, string branchName, String content)
        {
            var branch = repo.CreateBranch(branchName);
            Commands.Checkout(repo, branch.FriendlyName);

            FileInfo expectedPath = StageNewFile(repo, content);
            repo.Commit("Commit", Constants.Signature, Constants.Signature);
            return expectedPath;
        }

        private static FileInfo StageNewFile(IRepository repo, string contents = "null")
        {
            string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", contents);
            var stageNewFile = new FileInfo(newFilePath);
            Commands.Stage(repo, newFilePath);
            return stageNewFile;
        }

        private Repository CreateTestRepository(string path, string mergeDriverName)
        {
            var repository = new Repository(path);
            CreateConfigurationWithDummyUser(repository, Constants.Identity);
            CreateAttributesFile(repository, "* merge=" + mergeDriverName);
            return repository;
        }

        class EmptyMergeDriver : MergeDriver
        {
            public EmptyMergeDriver(string name)
                : base(name)
            { }

            protected override MergeDriverResult Apply(MergeDriverSource source)
            {
                throw new NotImplementedException();
            }

            protected override void Initialize()
            {
                throw new NotImplementedException();
            }
        }

        class FakeMergeDriver : MergeDriver
        {
            private readonly Action initCallback;
            private readonly Func<MergeDriverSource, MergeDriverResult> applyCallback;

            public FakeMergeDriver(string name, Action initCallback = null, Func<MergeDriverSource, MergeDriverResult> applyCallback = null)
                : base(name)
            {
                this.initCallback = initCallback;
                this.applyCallback = applyCallback;
            }

            protected override void Initialize()
            {
                if (initCallback != null)
                {
                    initCallback();
                }
            }
            protected override MergeDriverResult Apply(MergeDriverSource source)
            {
                if (applyCallback != null)
                    return applyCallback(source);
                return new MergeDriverResult { Status = MergeStatus.UpToDate };
            }
        }
    }
}
