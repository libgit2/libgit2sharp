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

        protected override int Clean(string path, Stream input, Stream output)
        {
            CleanCalledCount++;
            return RotateByThirteenPlaces(input, output);
        }

        protected override int Smudge(string path, Stream input, Stream output)
        {
            SmudgeCalledCount++;
            return RotateByThirteenPlaces(input, output);
        }

        public static int RotateByThirteenPlaces(Stream input, Stream output)
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

            return 0;
        }
    }
}
