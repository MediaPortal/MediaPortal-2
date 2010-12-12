#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Core.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.PictureMetadataExtractor.Settings
{
  public class PictureMetadataExtractorSettings
  {
    protected readonly static List<string> DEFAULT_PICTURE_EXTENSIONS = new List<string>
      {
          ".jpg",
          ".jpeg",
          ".png",
          ".bmp",
          ".gif",
          ".tga",
          ".tiff",
          ".tif",
      };

    protected List<string> _pictureExtensions = new List<string>(DEFAULT_PICTURE_EXTENSIONS);

    /// <summary>
    /// Picture extensions for which the <see cref="PictureMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> PictureExtensions
    {
      get { return _pictureExtensions; }
      set { _pictureExtensions = value; }
    }
  }
}
