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

using System.IO;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Holds resource files for a theme.
  /// </summary>
  public class Theme: SkinResources
  {
    public const string THEME_META_FILE = "theme.xml";

    public Theme(string name, Skin parentSkin): base(name, parentSkin)
    { }

    /// <summary>
    /// Returns the <see cref="Skin"/> this theme belongs to.
    /// </summary>
    public Skin ParentSkin
    {
      get { return InheritedSkinResources as Skin; }
    }

    /// <summary>
    /// Adds the resources in the specified directory.
    /// </summary>
    /// <param name="themeDirectory">Directory whose contents should be added
    /// to the file cache.</param>
    protected override void LoadDirectory(DirectoryInfo themeDirectory)
    {
      base.LoadDirectory(themeDirectory);
      // TODO: Load meta information file
    }
  }
}
