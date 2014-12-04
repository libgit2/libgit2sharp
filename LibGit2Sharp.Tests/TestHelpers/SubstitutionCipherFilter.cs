using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SubstitutionCipherFilter : Filter
    {
        public int CheckCalledCount = 0;
        public int CleanCalledCount = 0;
        public int SmudgeCalledCount = 0;

        public SubstitutionCipherFilter(string name, string attributes)
            : base(name, attributes)
        {
        }

        protected override int Check(IEnumerable<string> attributes, FilterSource filterSource)
        {
            CheckCalledCount++;
            var fileInfo = new FileInfo(filterSource.Path);
            var matches = attributes.Any(currentExtension => fileInfo.Extension == currentExtension);
            return matches ? 0 : -30;
        }

        protected override int Clean(GitBufReader input, GitBufWriter output)
        {
            CleanCalledCount++;
            return RotatByThirteenPlaces(input, output);
        }

        protected override int Smudge(GitBufReader input, GitBufWriter output)
        {
            SmudgeCalledCount++;
            return RotatByThirteenPlaces(input, output);
        }

        public static int RotatByThirteenPlaces(GitBufReader input, GitBufWriter output)
        {
            var inputString = Encoding.UTF8.GetString(input.Read());
            char[] array = inputString.ToCharArray();
            char value;
            for (int i = 0; i < inputString.Length; i++)
            {
                value = inputString[i];
                if ((value >= 'a' && value <= 'm') || (value >= 'A' && value <= 'M'))
                    array[i] = (char)(value + 13);
                else if ((value >= 'n' && value <= 'z') || (value >= 'N' && value <= 'Z'))
                    array[i] = (char)(value - 13);
            }
            var outputString = new string(array);
            output.Write(Encoding.UTF8.GetBytes(outputString));
            return 0;
        }
    }
}