using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MergeDriverFixture : BaseFixture
    {
        private const string MergeDriverName = "the-merge-driver";

        [Fact]
        public void CanRegisterAndUnregisterTheSameMergeDriver()
        {
            var mergeDriver = new EmptyMergeDriver(MergeDriverName);

            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);
            GlobalSettings.DeregisterMergeDriver(registration);

            var secondRegistration = GlobalSettings.RegisterMergeDriver(mergeDriver);
            GlobalSettings.DeregisterMergeDriver(secondRegistration);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var registration = GlobalSettings.RegisterMergeDriver(new EmptyMergeDriver(MergeDriverName));

            GC.Collect();

            GlobalSettings.DeregisterMergeDriver(registration);
        }

        [Fact]
        public void SameMergeDriverIsEqual()
        {
            var mergeDriver = new EmptyMergeDriver(MergeDriverName);
            Assert.Equal(mergeDriver, mergeDriver);
        }

        [Fact]
        public void InitCallbackNotMadeWhenMergeDriverNeverUsed()
        {
            bool called = false;
            void initializeCallback()
            {
                called = true;
            }

            var driver = new FakeMergeDriver(MergeDriverName, initializeCallback);
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
            string repoPath = InitNewRepository();
            bool called = false;

            MergeDriverResult apply(MergeDriverSource source)
            {
                called = true;
                return new MergeDriverResult { Status = MergeStatus.Conflicts };
            }

            var mergeDriver = new FakeMergeDriver(MergeDriverName, applyCallback: apply);
            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);

            try
            {
                using (var repo = CreateTestRepository(repoPath))
                {
                    string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", "file1");
                    var stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit", Constants.Signature, Constants.Signature);

                    var branch = repo.CreateBranch("second");

                    var id = Guid.NewGuid() + ".txt";
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
            string repoPath = InitNewRepository();

            MergeDriverResult apply(MergeDriverSource source)
            {
                var repos = source.Repository;
                var blob = repos.Lookup<Blob>(source.Theirs.Id);
                var content = blob.GetContentStream();
                return new MergeDriverResult { Status = MergeStatus.UpToDate, Content = content };
            }

            var mergeDriver = new FakeMergeDriver(MergeDriverName, applyCallback: apply);
            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);

            try
            {
                using (var repo = CreateTestRepository(repoPath))
                {
                    string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", "file1");
                    var stageNewFile = new FileInfo(newFilePath);
                    Commands.Stage(repo, newFilePath);
                    repo.Commit("Commit", Constants.Signature, Constants.Signature);

                    var branch = repo.CreateBranch("second");

                    var id = Guid.NewGuid() + ".txt";
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
                }
            }
            finally
            {
                GlobalSettings.DeregisterMergeDriver(registration);
            }
        }

        [Fact]
        public void DoubleRegistrationFailsButDoubleDeregistrationDoesNot()
        {
            Assert.Empty(GlobalSettings.GetRegisteredMergeDrivers());

            var mergeDriver = new EmptyMergeDriver(MergeDriverName);
            var registration = GlobalSettings.RegisterMergeDriver(mergeDriver);

            Assert.Throws<EntryExistsException>(() => { GlobalSettings.RegisterMergeDriver(mergeDriver); });
            Assert.Single(GlobalSettings.GetRegisteredMergeDrivers());

            Assert.True(registration.IsValid, "MergeDriverRegistration.IsValid should be true.");

            GlobalSettings.DeregisterMergeDriver(registration);
            Assert.Empty(GlobalSettings.GetRegisteredMergeDrivers());

            Assert.False(registration.IsValid, "MergeDriverRegistration.IsValid should be false.");

            GlobalSettings.DeregisterMergeDriver(registration);
            Assert.Empty(GlobalSettings.GetRegisteredMergeDrivers());

            Assert.False(registration.IsValid, "MergeDriverRegistration.IsValid should be false.");
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

        private Repository CreateTestRepository(string path)
        {
            var repository = new Repository(path);
            CreateConfigurationWithDummyUser(repository, Constants.Identity);
            CreateAttributesFile(repository, "* merge=the-merge-driver");
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
                initCallback?.Invoke();
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
