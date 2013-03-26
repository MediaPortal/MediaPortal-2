using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktFriend : TraktAuthentication
    {
        [DataMember(Name = "friend")]
        public string Friend { get; set; }
    }
}
