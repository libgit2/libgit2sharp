using System.Collections.Generic;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SubstitutionCipherFilter : Filter
    {
        public int CleanCalledCount = 0;
        public int SmudgeCalledCount = 0;

        public SubstitutionCipherFilter(string name, IEnumerable<FilterAttributeEntry> attributes)
            : base(name, attributes)
        {
        }

        protected override void Clean(string path, string root, Stream input, Stream output)
        {
            CleanCalledCount++;
            RotateByThirteenPlaces(input, output);
        }

        protected override void Smudge(string path, string root, Stream input, Stream output)
        {
            SmudgeCalledCount++;
            RotateByThirteenPlaces(input, output);
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
