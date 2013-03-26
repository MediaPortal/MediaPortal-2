using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    /// <summary>
    /// Authenticated Call will return user collection info
    /// </summary>
    [DataContract]
    public class TraktTrendingMovie : TraktMovie
    {
        [DataMember(Name = "watchers")]
        public int Watchers { get; set; }
    }
}
