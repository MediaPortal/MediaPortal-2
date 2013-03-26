using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace TraktPlugin.TraktAPI.DataStructures
{
    [DataContract]
    public class TraktUserList
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }

        [DataMember(Name = "show_numbers")]
        public bool ShowNumbers { get; set; }

        [DataMember(Name = "allow_shouts")]
        public bool AllowShouts { get; set; }

        [DataMember(Name = "items")]
        public List<TraktUserListItem> Items { get; set; }

        public int SortOrder { get; set; }
    }
}