#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.UI.Players.Image.Settings
{
  /// <summary>
  /// Image player settings.
  /// </summary>
  public class ImagePlayerSettings
  {
    public const int DEFAULT_SLIDE_SHOW_IMAGE_DURATION = 4;
    public static readonly List<string> DEFAULT_SUPPORTED_EXTENSIONS =
        new List<string>(".bmp,.png,.jpg,.jpeg".Split(','));

    protected double _slideShowImageDuration = DEFAULT_SLIDE_SHOW_IMAGE_DURATION;
    protected List<string> _supportedExtensions = new List<string>(DEFAULT_SUPPORTED_EXTENSIONS);

    /// <summary>
    /// Duration in seconds until the next image is shown in slideshow mode.
    /// </summary>
    [Setting(SettingScope.User, DEFAULT_SLIDE_SHOW_IMAGE_DURATION)]
    public double SlideShowImageDuration
    {
      get { return _slideShowImageDuration; }
      set { _slideShowImageDuration = value; }
    }

    /// <summary>
    /// Gets or sets a list of (lower-case!) extensions which are played with this image player.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> SupportedExtensions
    {
      get { return _supportedExtensions; }
      set { _supportedExtensions = value; }
    }

    /// <summary>
    /// Enables pan and zoom effect (Ken Burns).
    /// </summary>
    [Setting(SettingScope.User, true)]
    public bool UseKenBurns
    {
      get;
      set;
    }
  }
}
