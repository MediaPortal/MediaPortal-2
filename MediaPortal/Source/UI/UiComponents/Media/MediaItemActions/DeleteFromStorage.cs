#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  public class DeleteFromStorage : AbstractMediaItemAction, IMediaItemActionConfirmation
  {
    /// <summary>
    /// Defines a rule how to delete media items. It can be used to remove associated files when the main media file is deleted.
    /// </summary>
    public class DeleteRule
    {
      /// <summary>
      /// Enables or disables the rule.
      /// </summary>
      public bool IsEnabled { get; set; }
      /// <summary>
      /// Check if this AspectId is contained in the media item. Only if it is present, this rule will be used.
      /// </summary>
      public Guid HasAspectGuid { get; set; }
      /// <summary>
      /// List of file extensions that will be deleted as well.
      /// </summary>
      public List<string> DeleteOtherExtensions { get; set; }
      /// <summary>
      /// If set to <c>true</c>, the parent folder will be deleted as well if it is empty after removing all related files.
      /// </summary>
      public bool DeleteEmptyFolders { get; set; }
    }

    // TODO: Add to Settings
    protected List<DeleteRule> _defaultRules = new List<DeleteRule>();

    public override Task<bool> IsAvailableAsync(MediaItem mediaItem)
    {
      var result = !IsRecordingItem(mediaItem) && IsResourceDeletor(mediaItem);
      return Task.FromResult(result);
    }

    protected static bool IsResourceDeletor(MediaItem mediaItem)
    {
      try
      {
        var rl = mediaItem.GetResourceLocator();
        using (var ra = rl.CreateAccessor())
          return ra is IResourceDeletor;
      }
      catch (Exception)
      {
        return false;
      }
    }

    protected static bool IsRecordingItem(MediaItem mediaItem)
    {
      return mediaItem.Aspects.ContainsKey(new Guid("8DB70262-0DCE-4C80-AD03-FB1CDF7E1913") /* RecordingAspect.ASPECT_ID*/);
    }

    public override async Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem)
    {
      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      bool removeFromML = IsManagedByMediaLibrary(mediaItem) && cd != null;

      var falseResult = new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(false, ContentDirectoryMessaging.MediaItemChangeType.None);

      // Support multi-resource media items and secondary resources
      IList<MultipleMediaItemAspect> providerAspects;
      if (!MediaItemAspect.TryGetAspects(mediaItem.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
        return falseResult;

      foreach (MultipleMediaItemAspect providerAspect in providerAspects)
      {
        string systemId = (string)providerAspect[ProviderResourceAspect.ATTR_SYSTEM_ID];
        string resourceAccessorPath = (string)providerAspect[ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
        var rl = new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
        using (var ra = rl.CreateAccessor())
        {
          var rad = ra as IResourceDeletor;
          if (rad == null)
            return falseResult;

          // First try to delete the file from storage.
          if (!rad.Delete())
            return falseResult;

          // If the MediaItem was loaded from ML, remove it there as well.
          if (removeFromML)
          {
            await cd.DeleteMediaItemOrPathAsync(rl.NativeSystemId, rl.NativeResourcePath, true);
          }
        }
      }

      // Check for special cases here:
      // 1) Recordings have an additional .xml attached
      // 2) Deleting files could lead to empty folders that should be also removed
      foreach (DeleteRule rule in _defaultRules.Where(r => r.IsEnabled))
      {
        if (mediaItem.Aspects.ContainsKey(rule.HasAspectGuid))
        {
          var tsPath = mediaItem.GetResourceLocator().NativeResourcePath.ToString();
          foreach (string otherExtension in rule.DeleteOtherExtensions)
          {
            string otherFilePath = ProviderPathHelper.ChangeExtension(tsPath, otherExtension);
            IResourceAccessor ra;
            if (!ResourcePath.Deserialize(otherFilePath).TryCreateLocalResourceAccessor(out ra))
              continue;

            // Delete other file. We do not check here for existance of file, the Delete needs to handle this.
            using (ra)
            {
              var rad = ra as IResourceDeletor;
              rad?.Delete();
            }
          }

          if (rule.DeleteEmptyFolders)
          {
            var folderPath = ProviderPathHelper.GetDirectoryName(tsPath);
            IResourceAccessor ra;
            if (!ResourcePath.Deserialize(folderPath).TryCreateLocalResourceAccessor(out ra))
              continue;

            // Delete folder if empty
            using (ra)
            {
              IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
              if (fsra != null)
              {
                var isEmpty = fsra.GetFiles().Count == 0 && fsra.GetChildDirectories().Count == 0;
                if (isEmpty)
                {
                  var rad = ra as IResourceDeletor;
                  rad?.Delete();
                }
              }
            }
          }
        }
      }

      return new AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>(true, ContentDirectoryMessaging.MediaItemChangeType.Deleted);
    }

    public virtual string ConfirmationMessage
    {
      get { return "[Media.DeleteFromStorage.Confirmation]"; }
    }
  }
}
