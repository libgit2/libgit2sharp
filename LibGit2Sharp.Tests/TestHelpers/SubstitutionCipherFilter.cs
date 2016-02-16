using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SubstitutionCipherFilter : Filter
    {
        public static int CleanCalledCount = 0;
        public static int SmudgeCalledCount = 0;
        public static int InitializeCount = 0;

        public static void Initialize()
        {
            InitializeCount++;
        }

        public static void Clear()
        {
            CleanCalledCount = 0;
            SmudgeCalledCount = 0;
            InitializeCount = 0;
        }

        protected override void Apply(string root, string path, Stream input, Stream output, FilterMode mode, string verb)
        {
            switch (mode)
            {
                case FilterMode.Clean:
                    {
                        CleanCalledCount++;
                        RotateByThirteenPlaces(input, output);
                    }
                    break;

                case FilterMode.Smudge:
                    {
                        SmudgeCalledCount++;
                        RotateByThirteenPlaces(input, output);
                    }
                    break;
            }
        }
        public static void RotateByThirteenPlaces(Stream input, Stream output)
        {
            int value;

            while ((value = input.ReadByte()) != -1)
            {
                if ((value >= 'a' && value <= 'm') || (value >= 'A' && value <= 'M'))
                {
                    value += 13;
                }
                else if ((value >= 'n' && value <= 'z') || (value >= 'N' && value <= 'Z'))
                {
                    value -= 13;
                }

                output.WriteByte((byte)value);
            }
        }
    }
}
