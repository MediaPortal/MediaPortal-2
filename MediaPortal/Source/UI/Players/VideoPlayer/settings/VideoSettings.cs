#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Geometries;
using System.Xml.Serialization;

namespace MediaPortal.UI.Players.Video.Settings
{
  /// <summary>
  /// VideoSettings class contains settings for VideoPlayers.
  /// </summary>
  [XmlInclude(typeof(CodecInfo))]
  public class VideoSettings
  {
    #region Private variables

    private int _subtitleLCID = 0;
    private int _audioLCID = 0;
    private int _menuLCID = 0;

    #endregion

    /// <summary>
    /// Gets or sets CropSettings.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public CropSettings Crop { get; set; }

    /// <summary>
    /// Gets or sets the default geometry to use.
    /// </summary>
    [Setting(SettingScope.User, "")]
    public string Geometry { get; set; }

    // Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// <summary>
    /// Gets or Sets the preferred audio codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioCodec { get; set; }

    // Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// <summary>
    /// Gets or Sets the preferred audio codec for LATM-AAC.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioCodecLATMAAC { get; set; }

    /// <summary>
    /// Gets or sets the preferred MPEG2 codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo Mpeg2Codec { get; set; }

    /// <summary>
    /// Gets or sets the preferred H264 codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo H264Codec { get; set; }

    /// <summary>
    /// Gets or sets the preferred DivX codec.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo DivXCodec { get; set; }

    /// <summary>
    /// Gets or sets the preferred audio renderer.
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioRenderer { get; set; }

    /// <summary>
    /// Gets or sets a flag if closed captions should be enabled by default.
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool EnableClosedCaption { get; set; }

    /// <summary>
    /// Gets or sets a flag if subtitles should be enabled by default.
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool EnableSubtitles { get; set; }


    /// <summary>
    /// Gets or sets the preferred subtitle stream name for video playback.
    /// </summary>
    [Setting(SettingScope.User)]
    public string PreferredSubtitleSteamName { get; set; }

    /// <summary>
    /// Gets or sets the preferred subtitle language.
    /// If no choice was made before, the getter returns the global MP CurrentCulture.
    /// </summary>
    [Setting(SettingScope.User, 0)]
    public int PreferredSubtitleLanguage
    {
      get { return _subtitleLCID == 0 ? ServiceRegistration.Get<ILocalization>().CurrentCulture.LCID : _subtitleLCID; }
      set { _subtitleLCID = value; }
    }

    /// <summary>
    /// Gets or sets the preferred audio language.
    /// If no choice was made before, the getter returns the global MP CurrentCulture.
    /// </summary>
    [Setting(SettingScope.User, 0)]
    public int PreferredAudioLanguage
    {
      get { return _audioLCID == 0 ? ServiceRegistration.Get<ILocalization>().CurrentCulture.LCID : _audioLCID; }
      set { _audioLCID = value; }
    }

    /// <summary>
    /// Gets or sets the preferred menu language.
    /// If no choice was made before, the getter returns the global MP CurrentCulture.
    /// </summary>
    [Setting(SettingScope.User, 0)]
    public int PreferredMenuLanguage
    {
      get { return _menuLCID == 0 ? ServiceRegistration.Get<ILocalization>().CurrentCulture.LCID : _menuLCID; }
      set { _menuLCID = value; }
    }
  }
}