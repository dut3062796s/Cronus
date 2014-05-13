﻿using System.Runtime.Serialization;
using Elders.Cronus.DomainModelling;

namespace Elders.Cronus.Sample.IdentityAndAccess.Accounts.Events
{
    [DataContract(Name = "594c1ff2-07b1-42ac-b622-bc9d5045057a")]
    public class AccountRegistered : Event
    {
        AccountRegistered() { }

        public AccountRegistered(AccountId id, string email)
            : base(id)
        {
            Id = id;
            Email = email;
        }

        [DataMember(Order = 1)]
        public AccountId Id { get; private set; }

        [DataMember(Order = 2)]
        public string Email { get; private set; }

        public override string ToString()
        {
            return this.ToString("New user registered with email '{0}'. {1}", Email, Id);
        }
    }
}
