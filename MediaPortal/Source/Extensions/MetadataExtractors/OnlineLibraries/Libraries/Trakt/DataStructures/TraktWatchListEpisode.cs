using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktWatchListEpisode : TraktShow
    {
        [DataMember(Name = "episodes")]
        public List<Episode> Episodes { get; set; }

        [DataContract]
        public class Episode : TraktEpisode
        {
            [DataMember(Name = "inserted")]
            public long Inserted { get; set; }
        }
    }
}
