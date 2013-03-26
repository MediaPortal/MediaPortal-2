using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktTopEpisode
    {
        [DataMember(Name = "first_aired")]
        public long FirstAired { get; set; }

        [DataMember(Name = "first_aired_utc")]
        public long FirstAiredUtc { get; set; }

        [DataMember(Name = "first_aired_iso")]
        public string FirstAiredIso { get; set; }

        [DataMember(Name = "number")]
        public uint Number { get; set; }

        [DataMember(Name = "plays")]
        public uint Plays { get; set; }

        [DataMember(Name = "season")]
        public uint Season { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "url")]
        public string url { get; set; }
    }
}
