using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktEpisodeSummary
    {
        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}x{2}{3}", Show.Title, Episode.Season.ToString(), Episode.Number.ToString(), string.IsNullOrEmpty(Episode.Title) ? string.Empty : " - " + Episode.Title);
        }
    }
}
