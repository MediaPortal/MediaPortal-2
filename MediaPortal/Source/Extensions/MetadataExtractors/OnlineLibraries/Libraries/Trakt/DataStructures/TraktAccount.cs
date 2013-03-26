using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktAccount : TraktAuthentication
    {
        [DataMember(Name = "email")]
        public string Email { get; set; }
    }
}
