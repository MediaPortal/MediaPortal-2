using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktListSlug
    {
        [DataMember(Name = "username")]
        public string UserName { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        /// <summary>
        /// Slug is an unique id for the list
        /// This can be null when creating a new list
        /// </summary>
        [DataMember(Name = "slug")]
        public string Slug { get; set; }
    }
}
