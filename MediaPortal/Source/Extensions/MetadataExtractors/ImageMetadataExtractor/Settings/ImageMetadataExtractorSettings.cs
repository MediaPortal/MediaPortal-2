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

using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.Extensions.MetadataExtractors.ImageMetadataExtractor.Settings
{
  public class ImageMetadataExtractorSettings
  {
    protected readonly static string[] DEFAULT_IMAGE_FILE_EXTENSIONS = new string[]
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

    protected string[] _imageFileExtensions = DEFAULT_IMAGE_FILE_EXTENSIONS;

    /// <summary>
    /// Image file extensions for which the <see cref="ImageMetadataExtractor"/> should be used.
    /// </summary>
    [Setting(SettingScope.Global)]
    public string[] ImageFileExtensions
    {
      get { return _imageFileExtensions; }
      set { _imageFileExtensions = value; }
    }

    /// <summary>
    /// If <c>true</c>, Geo location details will be fetched from online sources.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool IncludeGeoLocationDetails { get; set; }
  }
}
