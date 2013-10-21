#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using DirectShow;
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
    public static CodecInfo DEFAULT_AUDIO_CODEC = new CodecInfo("LAV Audio Decoder", new DsGuid("e8e73b6b-4cb3-44a4-be99-4f7bcb96e491"));
    public static CodecInfo DEFAULT_VIDEO_CODEC = new CodecInfo("LAV Video Decoder", new DsGuid("ee30215d-164f-4a92-a4eb-9d4c13390f9f"));
    public static CodecInfo DEFAULT_AUDIO_RENDERER = new CodecInfo("Default DirectSound Device", DsGuid.Empty);

    #region Private variables

    private int _subtitleLCID = 0;
    private int _audioLCID = 0;
    private int _menuLCID = 0;
    private CodecInfo _audioCodec = null;
    private CodecInfo _audioCodecAAC = null;
    private CodecInfo _audioCodecLatmAac = null;
    private CodecInfo _audioRenderer = null;
    private CodecInfo _avcCodec = null;
    private CodecInfo _h264Codec = null;
    private CodecInfo _divXCodec = null;
    private CodecInfo _mpeg2Codec = null;

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

    /// <summary>
    /// Gets or Sets the preferred audio codec. If user didn't select a codec, this value will return <see cref="DEFAULT_AUDIO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioCodec
    {
      get { return _audioCodec ?? DEFAULT_AUDIO_CODEC; }
      set { _audioCodec = value; }
    }

    /// <summary>
    /// Gets or Sets the preferred audio codec for LATM-AAC. If user didn't select a codec, this value will return <see cref="DEFAULT_AUDIO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioCodecLATMAAC
    {
      get { return _audioCodecLatmAac ?? DEFAULT_AUDIO_CODEC; }
      set { _audioCodecLatmAac = value; }
    }

    /// <summary>
    /// Gets or Sets the preferred audio codec for AAC. If user didn't select a codec, this value will return <see cref="DEFAULT_AUDIO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioCodecAAC
    {
      get { return _audioCodecAAC ?? DEFAULT_AUDIO_CODEC; }
      set { _audioCodecAAC = value; }
    }

    /// <summary>
    /// Gets or sets the preferred MPEG2 codec. If user didn't select a codec, this value will return <see cref="DEFAULT_VIDEO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo Mpeg2Codec
    {
      get { return _mpeg2Codec ?? DEFAULT_VIDEO_CODEC; }
      set { _mpeg2Codec = value; }
    }

    /// <summary>
    /// Gets or sets the preferred H264 codec. If user didn't select a codec, this value will return <see cref="DEFAULT_VIDEO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo H264Codec
    {
      get { return _h264Codec ?? DEFAULT_VIDEO_CODEC; }
      set { _h264Codec = value; }
    }

    /// <summary>
    /// Gets or Sets the preferred AVC codec. If user didn't select a codec, this value will return <see cref="DEFAULT_VIDEO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AVCCodec
    {
      get { return _avcCodec ?? DEFAULT_VIDEO_CODEC; }
      set { _avcCodec = value; }
    }

    /// <summary>
    /// Gets or sets the preferred DivX codec. If user didn't select a codec, this value will return <see cref="DEFAULT_VIDEO_CODEC"/>.
    /// <remarks>
    /// Without default preferred codecs, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo DivXCodec
    {
      get { return _divXCodec ?? DEFAULT_VIDEO_CODEC; }
      set { _divXCodec = value; }
    }

    /// <summary>
    /// Gets or sets the preferred audio renderer. If user didn't select a renderer, this value will return <see cref="DEFAULT_AUDIO_RENDERER"/>, which will be selected by name.
    /// <remarks>
    /// Without default preferred renderer, the DirectShow graph will use intelligent connect.
    /// </remarks>
    /// </summary>
    [Setting(SettingScope.User)]
    public CodecInfo AudioRenderer
    {
      get { return _audioRenderer ?? DEFAULT_AUDIO_RENDERER; }
      set { _audioRenderer = value; }
    }

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
    /// Gets or sets a flag if multichannel audio streams should be preferred (i.e. 6ch AC3 over stereo).
    /// </summary>
    [Setting(SettingScope.User, false)]
    public bool PreferMultiChannelAudio { get; set; }

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