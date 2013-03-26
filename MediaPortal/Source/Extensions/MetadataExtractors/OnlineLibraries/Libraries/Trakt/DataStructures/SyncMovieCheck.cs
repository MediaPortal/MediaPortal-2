using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    public class SyncMovieCheck
    {
        [DataMember(Name = "last_skipped_sync")]
        public long LastSkippedSync { get; set; }

        [DataMember(Name = "movies")]
        public List<TraktMovieSync.Movie> Movies { get; set; }
    }
}
