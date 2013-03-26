using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktProducer : TraktPerson
    {
        [DataMember(Name = "executive")]
        public bool Executive { get; set; }
    }
}
