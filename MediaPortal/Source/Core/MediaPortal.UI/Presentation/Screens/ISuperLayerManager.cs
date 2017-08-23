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

namespace MediaPortal.UI.Presentation.Screens
{
  public interface ISuperLayerManager
  {
    /// <summary>
    /// Shows a busy screen. Remember to use a try/finally block to hide busy screen after usage.
    /// </summary>
    void ShowBusyScreen();

    /// <summary>
    /// Hides a formerly shown busy screen.
    /// </summary>
    void HideBusyScreen();

    /// <summary>
    /// Shows the super layer with the given <paramref name="superLayerScreenName"/> for the given <paramref name="duration"/>.
    /// </summary>
    /// <param name="superLayerScreenName">Name of the super layer screen to show.</param>
    /// <param name="duration">Duration to show the given super layer.</param>
    void ShowSuperLayer(string superLayerScreenName, TimeSpan duration);
  }
}