namespace libgit2sharp
{
    public class Person
    {
        public Person(string name, string email, ulong time)
        {
            Name = name;
            Email = email;
            Time = time;
        }

        public string Name { get; private set; }   /**< Full name */
        public string Email { get; private set; }  /**< Email address */
        public ulong Time { get; private set; }     /**< Time when this person committed the change */
    }
}