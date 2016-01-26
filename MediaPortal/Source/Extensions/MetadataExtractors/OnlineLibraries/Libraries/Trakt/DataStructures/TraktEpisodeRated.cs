using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
    [DataContract]
    public class TraktEpisodeRated
    {
        [DataMember(Name = "rating")]
        public int Rating { get; set; }

        [DataMember(Name = "rated_at")]
        public string RatedAt { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }
    }
}
