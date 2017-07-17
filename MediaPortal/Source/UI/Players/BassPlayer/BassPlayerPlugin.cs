#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Players.BassPlayer.PlayerComponents;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Common.PathManager;
using System.IO;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Collections.Generic;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.UI.Players.BassPlayer
{
  public class BassPlayerPlugin : IPluginStateTracker, IPlayerBuilder
  {
    #region IPluginStateTracker implementation

    public void Activated(PluginRuntime pluginRuntime)
    {
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop() { }

    public void Continue() { }

    void IPluginStateTracker.Shutdown() { }

    #endregion

    #region Helpers

    private bool TryCreateInsertMediaMediaItem(out MediaItem mi)
    {
      mi = null;
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string resourceDirectory = pathManager.GetPath(@"<DATA>\Resources\");
      string[] files = Directory.GetFiles(resourceDirectory, "InsertAudioMedia.*");
      if (files == null || files.Length == 0)
        return false;

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, files[0]).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, MimeTypeDetector.GetMimeType(files[0], "audio/unknown"));
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);

      MediaItemAspect mediaAspect = MediaItemAspect.GetOrCreateAspect(aspects, MediaAspect.Metadata);
      mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, "?");

      mi = new MediaItem(Guid.Empty, aspects);
      return true;
    }

    #endregion

    #region IPlayerBuilder implementation

    public IPlayer GetPlayer(MediaItem mediaItem)
    {
      if (mediaItem.IsStub)
      {
        MediaItem stubMI;
        if (TryCreateInsertMediaMediaItem(out stubMI))
          mediaItem = stubMI;
      }

      BassPlayer player = new BassPlayer();
      try
      {
        if (!player.SetMediaItem(mediaItem))
        {
          player.Dispose();
          return null;
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("BassPlayerPlugin: Error playing media item '{0}'", e, mediaItem.ToString());
        player.Dispose();
        return null;
      }
      return player;
    }

    #endregion
  }
}
