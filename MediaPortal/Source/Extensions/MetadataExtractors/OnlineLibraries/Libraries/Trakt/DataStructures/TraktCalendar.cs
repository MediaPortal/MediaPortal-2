using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCalendar : TraktResponse
    {
        [DataMember(Name = "date")]
        public string Date { get; set; }

        [DataMember(Name = "episodes")]
        public List<TraktEpisodeSummary> Episodes { get; set; }
    }
}
