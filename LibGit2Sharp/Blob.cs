namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        public Blob(string objectId)
            : base(objectId, ObjectType.Blob)
        {
        }
    }
}
