namespace libgit2sharp
{
    public class RawObject : GitObject
    {
        public RawObject(Header header, byte[] data) : base(header.Id, header.Type)
        {
            Data = data;
            Length = header.Length;
        }

        public ulong Length { get; private set; }
        public byte[] Data { get; private set; }
    }
}