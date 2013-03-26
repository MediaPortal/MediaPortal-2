using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Data structure for a follow response from Trakt
    /// </summary>
    [DataContract]
    public class TraktNetworkFollowResponse : TraktResponse
    {
        [DataMember(Name = "pending")]
        public bool Pending { get; set; }
    }
}
