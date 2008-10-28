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

// Todo: 
// Obsolete. Can be removed as soon as new Settings class is ready.

using MediaPortal.Core.Settings;

namespace Media.Players.BassPlayer
{
  public class _BassPlayerSettings
  {
    #region variables
    private int _volume;
    private int _crossfade;
    private bool _gaplessplayback;
    private bool _softstop;
    private string _supportedExtensions;
    #endregion

    /// <summary>
    /// The Volume for Playback
    /// </summary>
    [Setting(SettingScope.User, "85")]
    public int Volume
    {
      get { return _volume; }
      set { _volume = value; }
    }

    /// <summary>
    /// The Crossfading Interval in Ms
    /// </summary>
    [Setting(SettingScope.User, "4000")]
    public int Crossfade
    {
      get { return _crossfade; }
      set { _crossfade = value; }
    }

    /// <summary>
    /// Do we want Gapless Playback
    /// </summary>
    [Setting(SettingScope.User, "false")]
    public bool GaplessPlayback
    {
      get { return _gaplessplayback; }
      set { _gaplessplayback = value; }
    }

    /// <summary>
    /// Softstop
    /// </summary>
    [Setting(SettingScope.User, "true")]
    public bool SoftStop
    {
      get { return _softstop; }
      set { _softstop = value; }
    }

    /// <summary>
    /// Softstop
    /// </summary>
    [Setting(SettingScope.User, ".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1,.aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi,.kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv")]
    public string SupportedExtensions
    {
      get { return _supportedExtensions; }
      set { _supportedExtensions = value; }
    }
  }
}
