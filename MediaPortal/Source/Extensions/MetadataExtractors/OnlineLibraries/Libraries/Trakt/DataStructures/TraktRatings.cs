using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktRatings
    {
        [DataMember(Name = "percentage")]
        public int Percentage { get; set; }

        [DataMember(Name = "votes")]
        public int Votes { get; set; }

        [DataMember(Name = "loved")]
        public int LovedCount { get; set; }

        [DataMember(Name = "hated")]
        public int HatedCount { get; set; }
    }
}
