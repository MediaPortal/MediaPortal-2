#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// Static helper class for media playing.
  /// </summary>
  public static class PlayerHelper
  {
    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";

    public const string FAILED_TO_PLAY_SELECTED_ITEM_HEADER_RESOURCE = "[Media.FailedToPlaySelectedItemHeader]";
    public const string FAILED_TO_PLAY_SELECTED_ITEM_TEXT_RESOURCE = "[Media.FailedToPlaySelectedItemText]";

    public static Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);

    /// <summary>
    /// Tries to reuse the player in the specified <paramref name="slot"/> for the specified media item, if
    /// the player exists.
    /// </summary>
    /// <remarks>
    /// This is necessary for cross-fading or for avoiding to rebuild the player instance, which sometimes might
    /// take a long time. This method only succeeds if the current player implements the
    /// <see cref="IReusablePlayer"/> interface.
    /// </remarks>
    /// <param name="slot">Slot number of the player to reuse.</param>
    /// <param name="locator">Media locator to the media resource to be played.</param>
    /// <param name="mimeType">MimeType of the content to be played, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <returns><c>true</c>, if the player in the specified <paramref name="slot"/> could be reused.
    /// In this case, the player will start to play the specified item at once. <c>false</c>, if there is
    /// no player available in the specified <paramref name="slot"/> or if the current player
    /// cannot play the specified item. In this case, the current player will continue to work as before.</returns>
    public static bool ReusePlayer(int slot, IMediaItemLocator locator, string mimeType)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      // Test if the current player is able to play the item
      IReusablePlayer reusablePlayer = playerManager[slot] as IReusablePlayer;
      return reusablePlayer != null && reusablePlayer.NextItem(locator, mimeType);
    }

    /// <summary>
    /// Tries to reuse the primary player for the specified media item.
    /// See <see cref="ReusePlayer(int,IMediaItemLocator,string)"/>.
    /// </summary>
    /// <param name="locator">Media locator to the media resource to be played.</param>
    /// <param name="mimeType">MimeType of the content to be played, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <returns><c>true</c>, if the primary player could be reused, else <c>false</c>.</returns>
    public static bool ReusePlayer(IMediaItemLocator locator, string mimeType)
    {
      return ReusePlayer(ServiceScope.Get<IPlayerManager>().PrimaryPlayer, locator, mimeType);
    }

    /// <summary>
    /// Starts the specified media <paramref name="item"/> as primary player.
    /// </summary>
    /// <remarks>
    /// The new item will be played at once and the primary player will be exchanged or reused for the
    /// new media item. If a player does cross-fading, this will also take place when using this method.
    /// This method should be used throughout the application to simply play a media item instead of copying the
    /// code.
    /// </remarks>
    /// <param name="item">The media item to be played.</param>
    public static void PlayMediaItem(MediaItem item)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      MediaItemAspect providerAspect = item[ProviderResourceAspect.ASPECT_ID];
      string hostName = (string) providerAspect[ProviderResourceAspect.ATTR_SOURCE_COMPUTER];
      Guid providerId = new Guid((string) providerAspect[ProviderResourceAspect.ATTR_PROVIDER_ID]);
      string path = (string) providerAspect[ProviderResourceAspect.ATTR_PATH];
      MediaItemLocator mil = new MediaItemLocator(new SystemName(hostName), providerId, path);
      IPlayer resumePlayer = null;
      if (!ReusePlayer(playerManager.PrimaryPlayer, mil, null))
      {
        int playerSlot;
        resumePlayer = playerManager.PreparePlayer(mil, null, out playerSlot);
        if (resumePlayer == null)
        {
          IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
          dialogManager.ShowDialog(FAILED_TO_PLAY_SELECTED_ITEM_HEADER_RESOURCE,
              FAILED_TO_PLAY_SELECTED_ITEM_TEXT_RESOURCE, DialogType.OkDialog, false);
          return;
        }
        if (playerSlot != playerManager.PrimaryPlayer)
          // Only release the old primary player if the player manager couldn't reuse the old primary player and
          // if it didn't make the new player the primary player 
          playerManager.ReleasePlayer(playerManager.PrimaryPlayer);
        playerManager.SetPrimaryPlayer(playerSlot);
      }

      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(CURRENTLY_PLAYING_STATE_ID);
      if (resumePlayer != null)
        resumePlayer.Resume(); // When preparing a player, it will be in paused state
    }
  }
}