using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktSeason
    {
        [DataMember(Name = "season")]
        public int Season { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "images")]
        public SeasonImages Images { get; set; }

        [DataContract]
        public class SeasonImages
        {
            [DataMember(Name = "poster")]
            public string Poster { get; set; }
        }
    }

    [DataContract]
    public class TraktShowSeason : TraktSeason
    {
        [DataMember(Name = "episodes")]
        public int EpisodeCount { get; set; }
    }

    [DataContract]
    public class TraktShowSeasonEx : TraktSeason
    {
        [DataMember(Name = "episodes")]
        public List<TraktEpisode> Episodes { get; set; }
    }
}
