using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.UPnP.Items;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public static class TvEntityExtensions
  {
    public static IProgram ToProgram(this Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram)
    {
      return new Program
        {
          ChannelId = tvProgram.idChannel,
          ProgramId = tvProgram.idProgram,
          Title = tvProgram.title,
          Description = tvProgram.description,
          StartTime = tvProgram.startTime,
          EndTime = tvProgram.endTime,
          // TODO: Genre!
        };
    }

    public static IChannel ToChannel(this Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return new Channel { ChannelId = tvChannel.idChannel, Name = tvChannel.displayName };
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      return new ChannelGroup { ChannelGroupId = tvGroup.idGroup, Name = tvGroup.groupName };
    }
  }
}