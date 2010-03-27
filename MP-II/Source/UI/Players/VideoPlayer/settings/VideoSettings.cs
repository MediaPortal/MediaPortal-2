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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Geometries;

namespace Ui.Players.Video
{
  public class VideoSettings
  {
    private CropSettings _crop;
    private string _geometry;
    private string _audioCodec;
    private string _mpeg2Codec;
    private string _h264Codec;
    private string _divxCodec;
    private string _subtitleLanguage;
    private string _audioLanguage;


    [Setting(SettingScope.User, null)]
    public CropSettings Crop
    {
      get { return _crop; }
      set { _crop = value; }
    }

    [Setting(SettingScope.User, "")]
    public string Geometry
    {
      get { return _geometry; }
      set { _geometry = value; }
    }
    [Setting(SettingScope.User, "MPA Decoder Filter")]
    public string AudioCodec
    {
      get { return _audioCodec; }
      set { _audioCodec = value; }
    }
    [Setting(SettingScope.User, "CyberLink Video/SP Decoder (PDVD7)")]
    public string Mpeg2Codec
    {
      get { return _mpeg2Codec; }
      set { _mpeg2Codec = value; }
    }
    [Setting(SettingScope.User, "CyberLink H.264/AVC Decoder (PDVD7.X)")]
    public string H264Codec
    {
      get { return _h264Codec; }
      set { _h264Codec = value; }
    }
    [Setting(SettingScope.User, "ffdshow Video Decoder")]
    public string DivXCodec
    {
      get { return _divxCodec; }
      set { _divxCodec = value; }
    }
    [Setting(SettingScope.User, "English")]
    public string SubtitleLanguage
    {
      get { return _subtitleLanguage; }
      set { _subtitleLanguage = value; }
    }
    [Setting(SettingScope.User, "English")]
    public string AudioLanguage
    {
      get { return _audioLanguage; }
      set { _audioLanguage = value; }
    }
  }
}
