#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.UI.Presentation.Screens
{
  /// <summary>
  /// Management class for the background layer of the screen manager.
  /// </summary>
  public interface IBackgroundManager
  {
    /// <summary>
    /// Installs the background manager. This method will set-up event listeners to be able to exchange
    /// the screen manager's background, if necessary.
    /// This method also should set the initial background. Future changes should be triggered asynchronously.
    /// </summary>
    void Install();

    /// <summary>
    /// Uninstalls the background manager. This method will remove all event listeners which have been
    /// installed by method <see cref="Install"/>. After this method was called, no more asynchronous
    /// background changes should take place for this background manager.
    /// </summary>
    void Uninstall();
  }
}