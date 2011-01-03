using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTvClient.Helpers
{
  /// <summary>
  /// Holds a GUI item for the MultiChannel program guide, that contains the channel name and an ItemList
  /// with the Programs.
  /// </summary>
  public class ChannelProgramListItem : ListItem
  {
    public ItemsList Programs { get; set; }
    public IChannel Channel { get; set; }
    public ChannelProgramListItem(IChannel channel, ItemsList programs)
    {
      SetLabel(Consts.KEY_NAME, channel.Name);
      Programs = programs;
      Channel = channel;
    }
  }
}