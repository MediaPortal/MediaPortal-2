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

using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Tools;
using System.Xml.Serialization;

namespace MediaPortal.UI.Players.Video.Settings
{
  /// <summary>
  /// VideoSettings class contains settings for VideoPlayers.
  /// </summary>
  [XmlInclude(typeof(CodecInfo))]
  public class BluRayPlayerSettings
  {
    private CodecInfo _vc1Codec;

    /// <summary>
    /// Gets or sets the preferred VC1 codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo VC1Codec
    {
      get { return _vc1Codec ?? VideoSettings.DEFAULT_VIDEO_CODEC; }
      set { _vc1Codec = value; }
    }

    /// <summary>
    /// Gets or sets the value for parental rating.
    /// </summary>
    [Setting(SettingScope.User, 99)]
    public int ParentalControl { get; set; }

    /// <summary>
    /// Gets or sets the region code for BD discs.
    /// </summary>
    [Setting(SettingScope.User, "B")]
    public string RegionCode { get; set; }
  }
}
