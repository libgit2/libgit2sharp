using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        const string DecodedInput = "This is a substitution cipher";
        const string EncodedInput = "Guvf vf n fhofgvghgvba pvcure";

        private const string FilterName = "the-filter";
        private readonly string FilterAttribute = "test";

        [Fact]
        public void CanRegisterFilterWithSingleAttribute()
        {
            var regitration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);
            try
            {
                Assert.Equal(FilterAttribute, regitration.Attribute);
            }
            finally
            {
                GlobalSettings.UnregisterFilter(regitration);
            }
        }

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var firstRegistration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);
            GlobalSettings.UnregisterFilter(firstRegistration);

            var secondRegistration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);
            GlobalSettings.UnregisterFilter(secondRegistration);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            FakeFilter.Clear();

            var registration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);

            try
            {
                GC.Collect();
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void SameFilterRegistrationIsEqual()
        {
            FakeFilter.Clear();

            var registration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);

            try
            {
                Assert.Equal(registration, registration);
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void InitCallbackNotMadeWhenFilterNeverUsed()
        {
            FakeFilter.Clear();

            var registration = GlobalSettings.RegisterFilter<FakeFilter>(FilterName, FilterAttribute);

            try
            {
                Assert.Equal(0, FakeFilter.InitializeCallCount);
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void InitCallbackMadeWhenUsingTheFilter()
        {
            FakeFilter.Clear();

            var registration = GlobalSettings.RegisterFilter<FakeFilter>(FilterName, FilterAttribute, FilterRegistration.DefaultFilterPriority, FakeFilter.Initialize);

            try
            {
                Assert.Equal(0, FakeFilter.InitializeCallCount);

                string repoPath = InitNewRepository();
                using (var repo = CreateTestRepository(repoPath))
                {
                    StageNewTxtFile(repo);
                    Assert.Equal(1, FakeFilter.InitializeCallCount);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void WhenStagingFileApplyIsCalledWithCleanForCorrectPath()
        {
            FakeFilter.Clear();

            string repoPath = InitNewRepository();

            var registration = GlobalSettings.RegisterFilter<FakeFilter>(FilterName, FilterAttribute);

            try
            {
                using (var repo = CreateTestRepository(repoPath))
                {
                    StageNewTxtFile(repo);
                    Assert.Equal(1, FakeFilter.CleanCallCount);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void CleanFilterWritesOutputToObjectTree()
        {
            string repoPath = InitNewRepository();

            var registration = GlobalSettings.RegisterFilter<SubstitutionCipherFilter>(FilterName, FilterAttribute);

            try
            {
                using (var repo = CreateTestRepository(repoPath))
                {
                    FileInfo expectedFile = StageNewTxtFile(repo, DecodedInput);
                    var commit = repo.Commit("Clean that file");
                    var blob = (Blob)commit.Tree[expectedFile.Name].Target;

                    var textDetected = blob.GetContentText();
                    Assert.Equal(EncodedInput, textDetected);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void WhenCheckingOutAFileFileSmudgeWritesCorrectFileToWorkingDirectory()
        {
            string repoPath = InitNewRepository();

            var registration = GlobalSettings.RegisterFilter<SubstitutionCipherFilter>(FilterName, FilterAttribute);

            try
            {
                using (var repo = CreateTestRepository(repoPath))
                {
                    FileInfo expectedFile = StageNewTxtFile(repo, DecodedInput);
                    var commit = repo.Commit("Clean that file");

                    var blob = (Blob)commit.Tree[expectedFile.Name].Target;

                    var textDetected = blob.GetContentText();
                    Assert.Equal(EncodedInput, textDetected);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void CanFilterLargeFiles()
        {
            const int ContentLength = 128 * 1024 * 1024 - 13;
            const char ContentValue = 'x';

            char[] content = (new string(ContentValue, 1024)).ToCharArray();

            string repoPath = InitNewRepository();

            var registration = GlobalSettings.RegisterFilter<FileExportFilter>(FilterName, FilterAttribute);

            try
            {
                string filePath = Path.Combine(Directory.GetParent(repoPath).Parent.FullName, Guid.NewGuid().ToString() + ".blob");
                FileInfo contentFile = new FileInfo(filePath);
                using (var writer = new StreamWriter(contentFile.OpenWrite()) { AutoFlush = true })
                {
                    int remain = ContentLength;

                    while (remain > 0)
                    {
                        int chunkSize = remain > content.Length ? content.Length : remain;
                        writer.Write(content, 0, chunkSize);
                        remain -= chunkSize;
                    }
                }

                string attributesPath = Path.Combine(Directory.GetParent(repoPath).Parent.FullName, ".gitattributes");
                FileInfo attributesFile = new FileInfo(attributesPath);

                string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
                var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };

                using (Repository repo = new Repository(repoPath, repositoryOptions))
                {
                    File.WriteAllText(attributesPath, "*.blob filter=test");
                    repo.Stage(attributesFile.Name);
                    repo.Stage(contentFile.Name);
                    repo.Commit("test");
                    contentFile.Delete();
                    repo.Checkout("HEAD", new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force });
                }

                contentFile = new FileInfo(filePath);
                Assert.True(contentFile.Exists, "Contents not restored correctly by forced checkout.");
                using (StreamReader reader = contentFile.OpenText())
                {
                    int totalRead = 0;
                    char[] block = new char[1024];
                    int read;
                    while ((read = reader.Read(block, 0, block.Length)) > 0)
                    {
                        Assert.True(CharArrayAreEqual(block, content, read));
                        totalRead += read;
                    }

                    Assert.Equal(ContentLength, totalRead);
                }

                contentFile.Delete();
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }
        }

        [Fact]
        public void DoubleRegistrationFailsButDoubleDeregistrationDoesNot()
        {
            Assert.Equal(0, GlobalSettings.GetRegisteredFilters().Count());

            var registration = GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute);
            try
            {
                Assert.Throws<EntryExistsException>(() => { GlobalSettings.RegisterFilter<EmptyFilter>(FilterName, FilterAttribute); });
                Assert.Equal(1, GlobalSettings.GetRegisteredFilters().Count());

                Assert.True(registration.IsValid, "FilterRegistration.IsValid should be true.");
            }
            finally
            {
                GlobalSettings.UnregisterFilter(registration);
            }

            Assert.Equal(0, GlobalSettings.GetRegisteredFilters().Count());
            Assert.False(registration.IsValid, "FilterRegistration.IsValid should be false.");

            GlobalSettings.UnregisterFilter(registration);
            Assert.Equal(0, GlobalSettings.GetRegisteredFilters().Count());

            Assert.False(registration.IsValid, "FilterRegistration.IsValid should be false.");
        }


        private unsafe bool CharArrayAreEqual(char[] array1, char[] array2, int count)
        {
            if (ReferenceEquals(array1, array2))
            {
                return true;
            }
            if (ReferenceEquals(array1, null) || ReferenceEquals(null, array2))
            {
                return false;
            }
            if (array1.Length < count || array2.Length < count)
            {
                return false;
            }

            int len = count * sizeof(char);
            int cnt = len / sizeof(long);

            fixed (char* c1 = array1, c2 = array2)
            {
                long* p1 = (long*)c1,
                      p2 = (long*)c2;

                for (int i = 0; i < cnt; i++)
                {
                    if (p1[i] != p2[i])
                    {
                        return false;
                    }
                }

                byte* b1 = (byte*)c1,
                      b2 = (byte*)c2;

                for (int i = len * sizeof(long); i < len; i++)
                {
                    if (b1[i] != b2[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static FileInfo StageNewTxtFile(IRepository repo, string contents = "null")
        {
            string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", contents);
            var stageNewFile = new FileInfo(newFilePath);
            repo.Stage(newFilePath);
            return stageNewFile;
        }

        private Repository CreateTestRepository(string path)
        {
            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            var repository = new Repository(path, repositoryOptions);
            CreateAttributesFile(repository, "* filter=test");
            return repository;
        }

        private static void CreateAttributesFile(IRepository repo, string attributeEntry)
        {
            Touch(repo.Info.WorkingDirectory, ".gitattributes", attributeEntry);
        }

        class EmptyFilter : Filter
        {
            public EmptyFilter()
                : base()
            { }
        }

        class FakeFilter : Filter
        {
            public static int CleanCallCount = 0;
            public static int InitializeCallCount = 0;
            public static int SmudgeCallCount = 0;

            public static void Initialize()
            {
                InitializeCallCount++;
            }

            public static void Clear()
            {
                CleanCallCount = 0;
                InitializeCallCount = 0;
                SmudgeCallCount = 0;
            }

            public FakeFilter() { }

            protected override void Apply(string path, string root, Stream input, Stream output, FilterMode mode, string verb)
            {
                switch (mode)
                {
                    case FilterMode.Clean:
                        CleanCallCount++;
                        break;

                    case FilterMode.Smudge:
                        SmudgeCallCount++;
                        break;
                }

                base.Apply(path, root, input, output, FilterMode.Clean, verb);
            }
        }
    }
}
