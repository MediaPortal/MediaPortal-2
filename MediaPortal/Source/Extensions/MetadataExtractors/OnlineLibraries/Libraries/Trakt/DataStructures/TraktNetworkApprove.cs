using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktNetworkApprove : TraktNetwork
    {
        [DataMember(Name = "follow_back")]
        public bool FollowBack { get; set; }
    }
}
