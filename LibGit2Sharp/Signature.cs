﻿using System;

namespace LibGit2Sharp
{
    public class Signature
    {
        public Signature(string name, string email, DateTimeOffset when)
        {
            Name = name;
            Email = email;
            When = when;
        }

        public Signature(string name, string email, GitDate when) : this (name, email, when.ToDateTimeOffset())
        {
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
