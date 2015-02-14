using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SubstitutionCipherFilter : Filter
    {
        public int CheckCalledCount = 0;
        public int CleanCalledCount = 0;
        public int SmudgeCalledCount = 0;

        public SubstitutionCipherFilter(string name, IEnumerable<string> attributes)
            : base(name, attributes)
        {
        }

        protected override int Check(IEnumerable<string> attributes, FilterSource filterSource)
        {
            CheckCalledCount++;
            return base.Check(attributes, filterSource);
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
            using (var streamReader = new StreamReader(input, Encoding.UTF8))
            using (var streamWriter = new StreamWriter(output, Encoding.UTF8))
            {
                while (!streamReader.EndOfStream)
                {
                    var value = streamReader.Read();
                    if ((value >= 'a' && value <= 'm') || (value >= 'A' && value <= 'M'))
                    {
                        value += 13;
                    }
                    else if ((value >= 'n' && value <= 'z') || (value >= 'N' && value <= 'Z'))
                    {
                        value -= 13;
                    }

                    streamWriter.Write((char)value);
                }

                return 0;
            }
        }
    }
}
