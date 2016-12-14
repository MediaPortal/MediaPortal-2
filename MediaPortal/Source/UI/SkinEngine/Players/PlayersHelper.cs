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
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.SkinEngine.Players
{
  /// <summary>
  /// Class with static helper methods for player management.
  /// </summary>
  public static class PlayersHelper
  {
    public static void ReleaseGUIResources(IPlayerSlotController psc)
    {
      ISharpDXVideoPlayer sdvp = psc.CurrentPlayer as ISharpDXVideoPlayer;
      if (sdvp == null)
        return;
      try
      {
        sdvp.ReleaseGUIResources();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem releasing GUI resources in player '{0}'", e, sdvp);
      }
    }

    public static void ReallocGUIResources(IPlayerSlotController psc)
    {
      ISharpDXVideoPlayer sdvp = psc.CurrentPlayer as ISharpDXVideoPlayer;
      if (sdvp == null)
        return;
      try
      {
        sdvp.ReallocGUIResources();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problem reallocating GUI resources in player '{0}'", e, sdvp);
      }
    }

    public static void ReleaseGUIResources()
    {
      ServiceRegistration.Get<IPlayerManager>().ForEach(ReleaseGUIResources);
    }

    public static void ReallocGUIResources()
    {
      ServiceRegistration.Get<IPlayerManager>().ForEach(ReallocGUIResources);
    }
  }
}