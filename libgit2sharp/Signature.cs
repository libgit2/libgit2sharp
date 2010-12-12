using System;

namespace libgit2sharp
{
    public class Signature
    {
        public Signature(string name, string email, DateTimeOffset when)
        {
            Name = name;
            Email = email;
            When = when;
        }

        /// <summary>
        /// Full name
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Email address
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// Time when this person committed the change
        /// </summary>
        public DateTimeOffset When { get; private set; }
    }
}