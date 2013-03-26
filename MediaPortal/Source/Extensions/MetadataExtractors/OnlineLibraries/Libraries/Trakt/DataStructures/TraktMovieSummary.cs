using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktMovieSummary : TraktMovie
    {
        [DataMember(Name = "last_updated")]
        public long LastUpdated { get; set; }

        [DataMember(Name = "people")]
        public TraktPeople People { get; set; }

        [DataMember(Name = "stats")]
        public TraktStatistics Stats { get; set; }

        [DataMember(Name = "top_watchers")]
        public List<TraktTopWatcher> TopWatchers { get; set; }
    }
}
