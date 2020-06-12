using Emulators.Common;
using Emulators.Common.Games;
using Emulators.Common.Settings;
using Emulators.Emulator;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models.Navigation
{
  public class GameItem : PlayableMediaItem
  {
    public const string KEY_DESCRIPTION = "Emulators.Description";
    protected AbstractProperty _platformProperty = new WProperty(typeof(string), string.Empty);

    public GameItem(MediaItem mediaItem)
      : base(mediaItem)
    { }

    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);
      SimpleTitle = Title;

      SingleMediaItemAspect aspect;
      if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, GameAspect.Metadata, out aspect))
      {
        Platform = aspect.GetAttributeValue<string>(GameAspect.ATTR_PLATFORM);
        SetLabel(KEY_DESCRIPTION, aspect.GetAttributeValue<string>(GameAspect.ATTR_DESCRIPTION));
      }
    }

    public AbstractProperty PlatformProperty
    {
      get { return _platformProperty; }
    }

    public string Platform
    {
      get { return (string)_platformProperty.GetValue(); }
      set { _platformProperty.SetValue(value); }
    }

  }
}
