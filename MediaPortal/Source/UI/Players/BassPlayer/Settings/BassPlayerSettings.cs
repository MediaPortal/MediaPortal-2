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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.UI.Players.BassPlayer.Settings
{
  /// <summary>
  /// Contains the bass player user configuration.
  /// </summary>
  public class BassPlayerSettings
  {
    #region Fields

    private readonly List<string> _supportedExtensions =
                new List<string>(
              (".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1," +
               ".aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi," +
               ".kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv,.dff,.dsf").Split(','));

    #endregion

    public BassPlayerSettings()
    {
      SupportedExtensions = new List<string>(_supportedExtensions);
    }

    #region Public members

    [Setting(SettingScope.Global, OutputMode.DirectSound)]
    public OutputMode OutputMode { get; set; }

    [Setting(SettingScope.Global, "")]
    public string DirectSoundDevice { get; set; }

    [Setting(SettingScope.Global, "")]
    public string WASAPIDevice { get; set; }

    [Setting(SettingScope.Global, true)]
    public bool WASAPIExclusiveMode { get; set; }

    [Setting(SettingScope.Global, 200)]
    public int DirectSoundBufferSizeMilliSecs { get; set; }

    [Setting(SettingScope.Global, 300)]
    public int PlaybackBufferSizeMilliSecs { get; set; }

    [Setting(SettingScope.User, 20)]
    public int SeekIncrementSeconds { get; set; }

    [Setting(SettingScope.User, PlaybackMode.Normal)]
    public PlaybackMode SongTransitionMode { get; set; }

    [Setting(SettingScope.User, 500)]
    public int FadeDurationMilliSecs { get; set; }

    [Setting(SettingScope.User, 5d)]
    public double CrossFadeDurationSecs { get; set; }

    /// <summary>
    /// Gets or sets the (lower-case!) list of extensions which will be played with this player.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> SupportedExtensions { get; set; }

    [Setting(SettingScope.Global, 0)]
    public int VizStreamLatencyCorrectionMilliSecs { get; set; }

    #endregion
  }
}
