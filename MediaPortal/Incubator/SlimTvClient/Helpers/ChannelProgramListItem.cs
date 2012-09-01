using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Helpers
{
  /// <summary>
  /// Holds a GUI item for the MultiChannel program guide, that contains the channel name and an ItemList
  /// with the Programs.
  /// </summary>
  public class ChannelProgramListItem : ListItem
  {
    public ItemsList Programs { get; set; }
    public IChannel Channel { get; set; }
    public string ChannelLogoPath { get; set; }
    public ChannelProgramListItem(IChannel channel, ItemsList programs)
    {
      SetLabel(Consts.KEY_NAME, channel.Name);
      Programs = programs;
      Channel = channel;
      ChannelLogoPath = string.Format("channellogos\\{0}.png", channel.Name);
    }
  }
}