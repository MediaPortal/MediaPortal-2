#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class SearchForSubtitles : AbstractMediaItemAction
  {
    public SearchForSubtitles()
    {
    }

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      try
      {
        SubtitleMatchModel misub = ServiceRegistration.Get<IWorkflowManager>().GetModel(SubtitleMatchModel.MODEL_ID_SUBMATCH) as SubtitleMatchModel;
        return Task.FromResult(misub?.IsValidMediaItem(mediaItem) ?? false);
      }
      catch (Exception)
      {
        return Task.FromResult(false);
      }
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      // If the MediaItem was loaded from ML
      if (IsManagedByMediaLibrary(mediaItem))
      {
        IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (cd == null)
          return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);

        SubtitleMatchModel misub = ServiceRegistration.Get<IWorkflowManager>().GetModel(SubtitleMatchModel.MODEL_ID_SUBMATCH) as SubtitleMatchModel;
        await misub.OpenSelectEditionDialogAsync(mediaItem).ConfigureAwait(false);
        IEnumerable<MediaItemAspect> aspects = await misub.OpenSelectMatchDialogAsync(mediaItem).ConfigureAwait(false);
        if (aspects != null)
        {
          SubtitleInfo subtitle = new SubtitleInfo();
          subtitle.FromMetadata(MediaItemAspect.GetAspects(aspects));

          //Check if it is from a local share
          bool isLocalMediaItem = false;
          ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
          var localShares = sharesManagement.Shares.Values;
          foreach(var media in subtitle.MediaFiles)
          {
            if (localShares.Any(s => s.BaseResourcePath.IsSameOrParentOf(media.NativeResourcePath)))
            {
              isLocalMediaItem = true;
              break;
            }
          }

          //Import directly if it is a local media item
          if (isLocalMediaItem)
          {
            ServiceRegistration.Get<ILogger>().Debug("Downloading subtitle with local extractors");
            IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
            foreach (IMetadataExtractor extractor in mediaAccessor.LocalMetadataExtractors.Values)
            {
              if (!extractor.Metadata.ExtractedAspectTypes.ContainsKey(SubtitleAspect.ASPECT_ID))
                continue;

              await extractor.DownloadMetadataAsync(mediaItem.MediaItemId, MediaItemAspect.GetAspects(aspects)).ConfigureAwait(false);
            }

            foreach (var media in subtitle.MediaFiles)
            {
              var share = localShares.FirstOrDefault(s => s.BaseResourcePath.IsSameOrParentOf(media.NativeResourcePath));
              if(share != null)
              {
                sharesManagement.ReImportShare(share.ShareId);
                break;
              }
            }

            return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.Updated);
          }
          else //If not get the server to import
          {
            ServiceRegistration.Get<ILogger>().Debug("Downloading subtitle with server extractors");
            await cd.DownloadMetadataAsync(mediaItem.MediaItemId, aspects);

            //After refresh is completed on server a change message will be fired
            return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.None);
          }
        }
      }
      return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);
    }
  }
}
