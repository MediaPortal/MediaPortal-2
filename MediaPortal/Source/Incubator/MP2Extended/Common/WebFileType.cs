using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  [DataContract]
  public enum WebFileType
  {
    [EnumMember] Content = 0,
    [EnumMember] Backdrop = 1,
    [EnumMember] Banner = 2,
    [EnumMember] Poster = 3,
    [EnumMember] Cover = 4,
    [EnumMember] Logo = 5
  }
}