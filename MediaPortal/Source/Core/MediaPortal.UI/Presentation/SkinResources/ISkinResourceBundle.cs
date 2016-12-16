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

namespace MediaPortal.UI.Presentation.SkinResources
{
  public interface ISkinResourceBundle : IResourceAccessor
  {
    /// <summary>
    /// Gets the name of this resource bundle (skin name or theme name).
    /// </summary>
    string Name { get; }

    string ShortDescription { get; }

    string PreviewResourceKey { get; }

    /// <summary>
    /// Gets the native width of the skin this resource bundle belongs to.
    /// </summary>
    int SkinWidth { get; }

    /// <summary>
    /// Gets the native height of the skin this resource bundle belongs to.
    /// </summary>
    int SkinHeight { get; }

    /// <summary>
    /// Gets the name of the skin which contains this resource bundle. If this bundle is a skin, this property returns this name,
    /// else it returns the name of the parent skin.
    /// </summary>
    string SkinName { get; }

    /// <summary>
    /// Gets or sets the <see cref="SkinResources"/> instance which inherits
    /// its resources to this instance.
    /// </summary>
    ISkinResourceBundle InheritedSkinResources { get; }
  }
}
