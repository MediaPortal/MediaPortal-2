using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Data structure for a movie to send to and from Trakt
    /// </summary>
    [DataContract]
    public class TraktLibraryMovies : TraktMovieBase
    {
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "plays")]
        public int Plays { get; set; }

        [DataMember(Name = "in_collection")]
        public bool InCollection { get; set; }

        [DataMember(Name = "unseen")]
        public bool UnSeen { get; set; }
    }
}
