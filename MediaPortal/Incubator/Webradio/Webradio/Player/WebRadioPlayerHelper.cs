#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.Collections.Generic;
using System.Net;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Services.ResourceAccess.RawUrlResourceProvider;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.UserManagement;
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Media.Models;
using Un4seen.Bass;
using Webradio.Helper;
using Webradio.Models;

namespace Webradio.Player
{
  internal class WebRadioPlayerHelper
  {
    public const string WEBRADIO_MIMETYPE = "webradio/stream";
    protected static BassLibraryManager _lib;

    static WebRadioPlayerHelper()
    {
      _lib = BassLibraryManager.Get();
    }

    /// <summary>
    /// Constructs a dynamic <see cref="MediaItem"/> that contains the URL for the given <paramref name="stream"/> and starts
    /// the playback.
    /// </summary>
    /// <param name="stream">Stream.</param>
    public static bool PlayStream(RadioStation stream)
    {
      var code = CheckStream(stream.Streams[0].Url);
      if (code != HttpStatusCode.OK)
      {
        WebradioDataModel.DialogMessage = code.ToString();
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
        return false;
      }

      var mediaItem = CreateStreamMediaItem(stream);
      if (ServiceRegistration.Get<IPlayerContextManager>().IsVideoContextActive)
      {
        PlayItemsModel.CheckQueryPlayAction(mediaItem);
      }
      else
      {
        var channel = Bass.BASS_StreamCreateURL(stream.Streams[0].Url, 0, BASSFlag.BASS_DEFAULT, null, IntPtr.Zero);
        Bass.BASS_ChannelPlay(channel, false);
        PlayItemsModel.PlayItem(mediaItem);
      }

      _ = ServiceRegistration.Get<IUserManagement>().NotifyUsage("webradio", stream.Id);

      return true;
    }

    /// <summary>
    /// Constructs a dynamic <see cref="MediaItem"/> that contains the URL for the given <paramref name="stream"/>.
    /// </summary>
    internal static MediaItem CreateStreamMediaItem(RadioStation stream)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      var providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
      var mediaAspect = MediaItemAspect.GetOrCreateAspect(aspects, MediaAspect.Metadata);
      var audioAspect = MediaItemAspect.GetOrCreateAspect(aspects, AudioAspect.Metadata);

      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, RawUrlResourceProvider.ToProviderResourcePath(stream.Streams[0].Url).Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, ServiceRegistration.Get<ISystemResolver>().LocalSystemId);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, WEBRADIO_MIMETYPE);

      WebradioDataModel.SelectedStream = stream;
      WebradioDataModel.DefaultStreamLogo = WebradioHomeModel.SetStreamLogo(stream);
      WebradioDataModel.CurrentStreamLogo = WebradioDataModel.DefaultStreamLogo;

      var mediaItem = new MediaItem(Guid.Empty, aspects);
      return mediaItem;
    }

    private static HttpStatusCode CheckStream(string url)
    {
      try
      {
        if (!(WebRequest.Create(url) is HttpWebRequest request))
          return HttpStatusCode.ExpectationFailed;

        request.Timeout = 5000;
        using (var response = (HttpWebResponse)request.GetResponse())
        {
          return response.StatusCode;
        }
      }
      catch (Exception ex)
      {
        return HttpStatusCode.RequestTimeout;
      }
    }
  }
}
