namespace libgit2sharp
{
    public interface IObjectHeaderReader
    {
        Header ReadHeader(string objectId);
    }
}