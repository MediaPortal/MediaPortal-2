using System;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Plugins.SlimTv.Client.TvHandler
{
  public class SlimTvConsts
  {
    public const string MEDIA_NAVIGATION_MODE = "Recordings";

    public static Guid WF_MEDIA_NAVIGATION_ROOT_STATE = new Guid("9D5B01A7-035F-46CF-8246-3C158C6CA960");
    public static Guid[] NECESSARY_RECORDING_MIAS = new []
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          new Guid("C389F655-ED60-4271-91EA-EC589BD815C6") /* RecordingAspect.ASPECT_ID*/
      };

    public const string SCREEN_RECORDINGS_FILTER_BY_CHANNEL = "RecordingsByChannel";
    public const string SCREEN_RECORDINGS_FILTER_BY_NAME = "RecordingsByName";

    public const string RES_FILTER_BY_CHANNEL_MENU_ITEM = "[SlimTvClient.ChannelMenuItemLabel]";
    public const string RES_FILTER_BY_NAME_MENU_ITEM = "[SlimTvClient.NameFilterMenuItemLabel]";

    public const string RES_FILTER_CHANNEL_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.ChannelNavBarItemLabel]";
    public const string RES_FILTER_NAME_NAVBAR_DISPLAY_LABEL = "[SlimTvClient.NameFilterNavBarItemLabel]";

    public const string RES_RECORDINGS_VIEW_NAME = "[SlimTvClient.RecordingsRootViewName]";
  }
}
