namespace libgit2sharp
{
    public class Blob : GitObject
    {
        public Blob(string objectId)
            : base(objectId, ObjectType.Blob)
        {
        }
    }
}
