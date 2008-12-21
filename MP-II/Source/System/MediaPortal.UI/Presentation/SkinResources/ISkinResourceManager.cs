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

namespace MediaPortal.Presentation.SkinResources
{
  public delegate void SkinResourceCollectionChangedDlgt();

  /// <summary>
  /// Management class for skin resources.
  /// </summary>
  public interface ISkinResourceManager
  {
    /// <summary>
    /// Gets access to the skin resource accessor, which can load resource files in the context
    /// of the currently active skin.
    /// </summary>
    IResourceAccessor SkinResourceContext { get; }

    /// <summary>
    /// Gets fired when the resource collection of skin resources changed. This is the case if plugins
    /// are added or removed, for example.
    /// </summary>
    event SkinResourceCollectionChangedDlgt SkinResourcesChanged;

  }
}