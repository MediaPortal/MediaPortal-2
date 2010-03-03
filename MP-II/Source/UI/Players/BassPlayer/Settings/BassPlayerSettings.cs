#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core.Settings;

namespace Ui.Players.BassPlayer.Settings
{
  /// <summary>
  /// Contains the bass player user configuration.
  /// </summary>
  /// TODO: Write configuration classes for those settings
  public class BassPlayerSettings
  {
    public static class Constants
    {
      public const int Auto = -1;
    }

    public static class Defaults
    {
      public const OutputMode AudioOutputMode = OutputMode.DirectSound;
      public const string DirectSoundDevice = "";
      public const int DirectSoundBufferSizeMilliSecs = 200;
      public const int PlaybackBufferSizeMilliSecs = 500;
      public const int SeekIncrementSeconds = 20;
      public const PlaybackMode SongTransitionMode = PlaybackMode.Normal;
      public const int FadeDurationMilliSecs = 500;
      public const int CrossFadeDurationMilliSecs = 5;
      public const bool CrossFadeEnabled = true;
      public const int VizStreamLatencyCorrectionMilliSecs = 0;
      public static readonly List<string> SupportedExtensions =
          new List<string>(
              (".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1," +
               ".aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi," +
               ".kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv").Split(','));
    }

    #region Fields

    private OutputMode _OutputMode = Defaults.AudioOutputMode;
    private string _DirectSoundDevice = Defaults.DirectSoundDevice;
    private TimeSpan _DirectSoundBufferSize = TimeSpan.FromMilliseconds(Defaults.DirectSoundBufferSizeMilliSecs);
    private TimeSpan _PlaybackBufferSize = TimeSpan.FromMilliseconds(Defaults.PlaybackBufferSizeMilliSecs);
    private TimeSpan _SeekIncrement = TimeSpan.FromSeconds(Defaults.SeekIncrementSeconds);
    private PlaybackMode _songTransitionMode = Defaults.SongTransitionMode;
    private TimeSpan _FadeDuration = TimeSpan.FromMilliseconds(Defaults.FadeDurationMilliSecs);
    private TimeSpan _CrossFadeDuration = TimeSpan.FromMilliseconds(Defaults.CrossFadeDurationMilliSecs);
    private bool _CrossFadeEnabled = Defaults.CrossFadeEnabled;
    private TimeSpan _VizStreamLatencyCorrection = TimeSpan.FromMilliseconds(Defaults.VizStreamLatencyCorrectionMilliSecs);
    private List<string> _SupportedExtensions = new List<string>(Defaults.SupportedExtensions);

    #endregion

    #region Public members

    [Setting(SettingScope.Global, Defaults.AudioOutputMode)]
    public OutputMode OutputMode
    {
      get { return _OutputMode; }
      set { _OutputMode = value; }
    }

    [Setting(SettingScope.Global, Defaults.DirectSoundDevice)]
    public string DirectSoundDevice
    {
      get { return _DirectSoundDevice; }
      set { _DirectSoundDevice = value; }
    }

    [Setting(SettingScope.Global, Defaults.DirectSoundBufferSizeMilliSecs)]
    public int DirectSoundBufferSizeMilliSecs
    {
      get { return (int)_DirectSoundBufferSize.TotalMilliseconds; }
      set { _DirectSoundBufferSize = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan DirectSoundBufferSize
    {
      get { return _DirectSoundBufferSize; }
      set { _DirectSoundBufferSize = value; }
    }

    [Setting(SettingScope.Global, Defaults.PlaybackBufferSizeMilliSecs)]
    public int PlaybackBufferSizeMilliSecs
    {
      get { return (int)_PlaybackBufferSize.TotalMilliseconds; }
      set { _PlaybackBufferSize = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan PlaybackBufferSize
    {
      get { return _PlaybackBufferSize; }
      set { _PlaybackBufferSize = value; }
    }

    [Setting(SettingScope.User, Defaults.SeekIncrementSeconds)]
    public int SeekIncrementSeconds
    {
      get { return (int)_SeekIncrement.TotalSeconds; }
      set { _SeekIncrement = TimeSpan.FromSeconds(value); }
    }

    public TimeSpan SeekIncrement
    {
      get { return _SeekIncrement; }
      set { _SeekIncrement = value; }
    }

    [Setting(SettingScope.User, Defaults.SongTransitionMode)]
    public PlaybackMode SongTransitionMode
    {
      get { return _songTransitionMode; }
      set { _songTransitionMode = value; }
    }

    [Setting(SettingScope.User, Defaults.FadeDurationMilliSecs)]
    public int FadeDurationMilliSecs
    {
      get { return (int)_FadeDuration.TotalMilliseconds; }
      set { _FadeDuration = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan FadeDuration
    {
      get { return _FadeDuration; }
      set { _FadeDuration = value; }
    }

    [Setting(SettingScope.User, Defaults.CrossFadeDurationMilliSecs)]
    public int CrossFadeDurationMilliSecs
    {
      get { return (int)_CrossFadeDuration.TotalMilliseconds; }
      set { _CrossFadeDuration = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan CrossFadeDuration
    {
      get { return _CrossFadeDuration; }
      set { _CrossFadeDuration = value; }
    }

    [Setting(SettingScope.User)]
    public bool CrossFadingEnabled
    {
      get { return _CrossFadeEnabled; }
      set { _CrossFadeEnabled = value; }
    }

    [Setting(SettingScope.Global)]
    public List<string> SupportedExtensions
    {
      get { return _SupportedExtensions; }
      set { _SupportedExtensions = value; }
    }

    [Setting(SettingScope.Global, Defaults.VizStreamLatencyCorrectionMilliSecs)]
    public int VizStreamLatencyCorrectionMilliSecs
    {
      get { return (int)_VizStreamLatencyCorrection.TotalMilliseconds; }
      set { _VizStreamLatencyCorrection = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan VizStreamLatencyCorrection
    {
      get { return _VizStreamLatencyCorrection; }
      set { _VizStreamLatencyCorrection = value; }
    }

    #endregion
  }
}