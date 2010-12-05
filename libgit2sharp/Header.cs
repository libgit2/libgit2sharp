namespace libgit2sharp
{
    public class Header
    {
        public Header(string id, ObjectType type, ulong length)
        {
            Id = id;
            Type = type;
            Length = length;
        }

        public string Id { get; private set; }
        public ObjectType Type { get; private set; }
        public ulong Length { get; private set; }
    }
}