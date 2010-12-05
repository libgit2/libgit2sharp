namespace libgit2sharp
{
    public class RawObject
    {
        private readonly Header _header;

        public RawObject(Header header, byte[] data)
        {
            _header = header;
            Data = data;
        }

        public string Id { get { return _header.Id; } }
        public ObjectType Type { get { return _header.Type; } }
        public ulong Length { get { return _header.Length; } }
        public byte[] Data { get; private set; }
    }
}