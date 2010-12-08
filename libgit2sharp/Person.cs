using System;

namespace libgit2sharp
{
    public class Person
    {
        public Person(string name, string email, DateTimeOffset time)
        {
            Name = name;
            Email = email;
            Time = time;
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
        public DateTimeOffset Time { get; private set; }
    }
}