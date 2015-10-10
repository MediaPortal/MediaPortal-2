using System.Runtime.Serialization;

namespace MediaPortal.Plugins.MP2Extended.Common
{
  [DataContract]
  public enum WebSortOrder
  {
    [EnumMember] Asc = 0,
    [EnumMember] Desc = 1,
    [EnumMember] Custom = 2
  }
}