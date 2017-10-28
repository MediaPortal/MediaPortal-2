using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.TAS.Misc.BaseClasses
{
  class BaseUser
  {
    internal static WebUser User(IUser user)
    {
      return new WebUser
      {
        ChannelId = user.IdChannel,
        Name = user.Name,
        CardId = user.CardId,
        HeartBeat = user.HeartBeat,
        IsAdmin = user.IsAdmin,
        SubChannel = user.SubChannel,
        TvStoppedReason = (int)user.TvStoppedReason,
      };
    }
  }
}
