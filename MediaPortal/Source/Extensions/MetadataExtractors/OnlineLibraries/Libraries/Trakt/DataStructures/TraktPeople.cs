using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktPeople
    {
        [DataMember(Name = "directors")]
        public List<TraktPerson> Directors { get; set; }

        [DataMember(Name = "writers")]
        public List<TraktWriter> Writers { get; set; }

        [DataMember(Name = "producers")]
        public List<TraktProducer> Producers { get; set; }

        [DataMember(Name = "actors")]
        public List<TraktActor> Actors { get; set; }
    }
}
