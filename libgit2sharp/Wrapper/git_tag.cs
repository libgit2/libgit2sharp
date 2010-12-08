using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_tag
    {
        public git_object tag;
        public IntPtr target;
        public git_otype type;

        [MarshalAs(UnmanagedType.LPStr)]
        public string tag_name;

        public IntPtr tagger;

        [MarshalAs(UnmanagedType.LPStr)]
        public string message;

        internal Tag Build()
        {
            var gitTagger = (git_person)Marshal.PtrToStructure(tagger, typeof(git_person));
            var gitObject = (git_object)Marshal.PtrToStructure(target, typeof(git_object));

            var tagTarget = gitObject.Build();
            var tagTagger = gitTagger.Build();

            return new Tag(ObjectId.ToString(tag.id.id), tag_name, tagTarget, tagTagger, message);
        }
    }
}