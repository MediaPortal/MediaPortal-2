using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  [DataContract]
  public enum WebSortField
  {
    [EnumMember] Title = 0,
    [EnumMember] DateAdded = 1,
    [EnumMember] Year = 2,
    [EnumMember] Genre = 3,
    [EnumMember] Rating = 4,
    [EnumMember] Categories = 5,
    [EnumMember] MusicTrackNumber = 6,
    [EnumMember] MusicComposer = 7,
    [EnumMember] TVEpisodeNumber = 8,
    [EnumMember] TVSeasonNumber = 9,
    [EnumMember] PictureDateTaken = 10,
    [EnumMember] TVDateAired = 11,
    [EnumMember] Type = 12,
    [EnumMember] User = 15,
    [EnumMember] Channel = 16,
    [EnumMember] StartTime = 17,
    [EnumMember] NaturalTitle = 18
  }
}