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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;

namespace UiComponents.SkinBase
{
  public class PlayerBackgroundManager : IBackgroundManager
  {
    public static string DEFAULT_BACKGROUND_SCREEN = "default-background";
    public static string VIDEO_BACKGROUND_SCREEN = "video-background";
    public static string PICTURE_BACKGROUND_SCREEN = "picture-background";

    internal static void DoInstall()
    {
      // Set initial background
      UpdateBackground();

      // Install manager
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PlayerManagerMessaging.QUEUE);
      queue.MessageReceived += OnPlayerManagerMessageReceived;
    }

    internal static void DoUninstall()
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate(PlayerManagerMessaging.QUEUE);
      queue.MessageReceived -= OnPlayerManagerMessageReceived;
    }

    protected static void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      UpdateBackground();
    }

    protected static void UpdateBackground()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      string targetBackgroundScreen = GetTargetBackgroundScreen();
      if (screenManager.ActiveBackgroundScreenName != targetBackgroundScreen)
        screenManager.SetBackgroundLayer(targetBackgroundScreen);
    }

    protected static string GetTargetBackgroundScreen()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      if (playerManager.NumActiveSlots == 0)
        return DEFAULT_BACKGROUND_SCREEN;
      IPlayerSlotController pscPrimary = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      if (pscPrimary.IsActive && pscPrimary.PlayerSlotState != PlayerSlotState.Stopped)
      {
        if (pscPrimary.CurrentPlayer is IVideoPlayer)
          return VIDEO_BACKGROUND_SCREEN;
        else if (pscPrimary.CurrentPlayer is IPicturePlayer)
          return PICTURE_BACKGROUND_SCREEN;
      }
      return DEFAULT_BACKGROUND_SCREEN;
    }

    #region IBackgroundManager implementation

    public void Install()
    {
      DoInstall();
    }

    public void Uninstall()
    {
      DoUninstall();
    }

    #endregion
  }
}