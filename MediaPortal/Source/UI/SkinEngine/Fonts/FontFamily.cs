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

using System;
using MediaPortal.Utilities.SystemAPI;
using SharpFont;

namespace MediaPortal.UI.SkinEngine.Fonts
{
  /// <summary>
  /// Represents a font family.
  /// </summary>
  public class FontFamily : IDisposable
  {
    private static Library _library = null;

    private readonly string _name;
    private readonly Face _face;

    static FontFamily()
    {
      string absolutePlatformDir;
      if (!NativeMethods.SetPlatformSearchDirectories(out absolutePlatformDir))
        throw new Exception("Error adding dll probe path");
    }

    public FontFamily(string name, string filePathName)
    {
      _name = name;

      if (_library == null)
        _library = new Library();

      // Load the requested font.
      _face = new Face(_library, filePathName, 0);
    }

    /// <summary>
    /// Gets the name of font family.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets the font face FreeType object.
    /// </summary>
    public Face Face
    {
      get { return _face; }
    }

    public void Dispose()
    {
      if (_face != null)
        _face.Dispose();
    }
  }
}
