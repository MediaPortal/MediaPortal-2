using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktNetworkUser : TraktUser
    {
        [DataMember(Name = "since")]
        public long ApprovedDate { get; set; }
    }
}
