using System.Collections.Generic;
using System.Text;
using LibGit2Sharp.Core;

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

        protected override int Clean(string path, GitBufReader input, GitBufWriter output)
        {
            CleanCalledCount++;
            return RotateByThirteenPlaces(input, output);
        }

        protected override int Smudge(string path, GitBufReader input, GitBufWriter output)
        {
            SmudgeCalledCount++;
            return RotateByThirteenPlaces(input, output);
        }

        public static int RotateByThirteenPlaces(GitBufReader input, GitBufWriter output)
        {
            var inputString = Encoding.UTF8.GetString(input.ReadAll());
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