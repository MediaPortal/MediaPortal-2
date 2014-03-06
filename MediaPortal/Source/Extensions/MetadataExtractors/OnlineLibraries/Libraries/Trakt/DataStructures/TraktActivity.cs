using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktActivity
  {
    [DataMember(Name = "timestamps")]
    public TraktTimestamps Timestamps { get; set; }

    [DataContract]
    public class TraktTimestamps
    {
      [DataMember(Name = "start")]
      public long Start { get; set; }

      [DataMember(Name = "current")]
      public long Current { get; set; }
    }

    [DataMember(Name = "activity")]
    public List<Activity> Activities { get; set; }

    [DataContract]
    public class Activity : IEquatable<Activity>
    {
      #region IEquatable
      public bool Equals(Activity other)
      {
        if (other == null)
          return false;

        return (Id == other.Id && Type == other.Type);
      }

      public override int GetHashCode()
      {
        return (Id + "-" + Type).GetHashCode();
      }
      #endregion

      [DataMember(Name = "id")]
      public long Id { get; set; }

      [DataMember(Name = "timestamp")]
      public long Timestamp { get; set; }

      [DataMember(Name = "when")]
      public TraktWhen When { get; set; }

      [DataContract]
      public class TraktWhen
      {
        [DataMember(Name = "day")]
        public string Day { get; set; }

        [DataMember(Name = "time")]
        public string Time { get; set; }
      }

      [DataMember(Name = "elapsed")]
      public TraktElapsed Elapsed { get; set; }

      [DataContract]
      public class TraktElapsed
      {
        [DataMember(Name = "full")]
        public string Full { get; set; }

        [DataMember(Name = "short")]
        public string Short { get; set; }
      }

      [DataMember(Name = "type")]
      public string Type { get; set; }

      [DataMember(Name = "action")]
      public string Action { get; set; }

      [DataMember(Name = "user")]
      public TraktUser User { get; set; }

      [DataMember(Name = "rating")]
      public string Rating { get; set; }

      [DataMember(Name = "rating_advanced")]
      public string RatingAdvanced { get; set; }

      [DataMember(Name = "use_rating_advanced")]
      public bool UseRatingAdvanced { get; set; }

      [DataMember(Name = "episode")]
      public TraktEpisode Episode { get; set; }

      [DataMember(Name = "episodes")]
      public List<TraktEpisode> Episodes { get; set; }

      [DataMember(Name = "show")]
      public TraktShow Show { get; set; }

      [DataMember(Name = "movie")]
      public TraktMovie Movie { get; set; }

      [DataMember(Name = "review")]
      public Activity.TraktShout Review { get; set; }

      [DataMember(Name = "shout")]
      public TraktShout Shout { get; set; }

      [DataContract]
      public class TraktShout
      {
        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "text_html")]
        public string TextHTML { get; set; }

        [DataMember(Name = "spoiler")]
        public bool Spoiler { get; set; }
      }

      [DataMember(Name = "list")]
      public TraktList List { get; set; }

      [DataMember(Name = "list_item")]
      public TraktListItem ListItem { get; set; }

      [DataContract]
      public class TraktListItem
      {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "show")]
        public TraktShow Show { get; set; }

        [DataMember(Name = "movie")]
        public TraktMovie Movie { get; set; }

        [DataMember(Name = "episode")]
        public TraktEpisode Episode { get; set; }
      }
    }

  }
}
