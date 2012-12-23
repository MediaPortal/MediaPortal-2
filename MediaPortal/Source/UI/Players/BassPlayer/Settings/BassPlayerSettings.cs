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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;

namespace MediaPortal.UI.Players.BassPlayer.Settings
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
      public const double CrossFadeDurationSecs = 5d;
      public const bool CrossFadeEnabled = true;
      public const int VizStreamLatencyCorrectionMilliSecs = 0;
      public static readonly List<string> SupportedExtensions =
          new List<string>(
              (".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1," +
               ".aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi," +
               ".kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv").Split(','));
    }

    #region Fields

    private OutputMode _outputMode = Defaults.AudioOutputMode;
    private string _directSoundDevice = Defaults.DirectSoundDevice;
    private TimeSpan _directSoundBufferSize = TimeSpan.FromMilliseconds(Defaults.DirectSoundBufferSizeMilliSecs);
    private TimeSpan _playbackBufferSize = TimeSpan.FromMilliseconds(Defaults.PlaybackBufferSizeMilliSecs);
    private TimeSpan _seekIncrement = TimeSpan.FromSeconds(Defaults.SeekIncrementSeconds);
    private PlaybackMode _songTransitionMode = Defaults.SongTransitionMode;
    private TimeSpan _fadeDuration = TimeSpan.FromMilliseconds(Defaults.FadeDurationMilliSecs);
    private TimeSpan _crossFadeDuration = TimeSpan.FromSeconds(Defaults.CrossFadeDurationSecs);
    private bool _crossFadeEnabled = Defaults.CrossFadeEnabled;
    private TimeSpan _vizStreamLatencyCorrection = TimeSpan.FromMilliseconds(Defaults.VizStreamLatencyCorrectionMilliSecs);
    private List<string> _supportedExtensions = new List<string>(Defaults.SupportedExtensions);

    #endregion

    #region Public members

    [Setting(SettingScope.Global, Defaults.AudioOutputMode)]
    public OutputMode OutputMode
    {
      get { return _outputMode; }
      set { _outputMode = value; }
    }

    [Setting(SettingScope.Global, Defaults.DirectSoundDevice)]
    public string DirectSoundDevice
    {
      get { return _directSoundDevice; }
      set { _directSoundDevice = value; }
    }

    [Setting(SettingScope.Global, Defaults.DirectSoundBufferSizeMilliSecs)]
    public int DirectSoundBufferSizeMilliSecs
    {
      get { return (int)_directSoundBufferSize.TotalMilliseconds; }
      set { _directSoundBufferSize = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan DirectSoundBufferSize
    {
      get { return _directSoundBufferSize; }
      set { _directSoundBufferSize = value; }
    }

    [Setting(SettingScope.Global, Defaults.PlaybackBufferSizeMilliSecs)]
    public int PlaybackBufferSizeMilliSecs
    {
      get { return (int)_playbackBufferSize.TotalMilliseconds; }
      set { _playbackBufferSize = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan PlaybackBufferSize
    {
      get { return _playbackBufferSize; }
      set { _playbackBufferSize = value; }
    }

    [Setting(SettingScope.User, Defaults.SeekIncrementSeconds)]
    public int SeekIncrementSeconds
    {
      get { return (int)_seekIncrement.TotalSeconds; }
      set { _seekIncrement = TimeSpan.FromSeconds(value); }
    }

    public TimeSpan SeekIncrement
    {
      get { return _seekIncrement; }
      set { _seekIncrement = value; }
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
      get { return (int)_fadeDuration.TotalMilliseconds; }
      set { _fadeDuration = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan FadeDuration
    {
      get { return _fadeDuration; }
      set { _fadeDuration = value; }
    }

    [Setting(SettingScope.User, Defaults.CrossFadeDurationSecs)]
    public double CrossFadeDurationSecs
    {
      get { return _crossFadeDuration.TotalSeconds; }
      set { _crossFadeDuration = TimeSpan.FromSeconds(value); }
    }

    public TimeSpan CrossFadeDuration
    {
      get { return _crossFadeDuration; }
      set { _crossFadeDuration = value; }
    }

    [Setting(SettingScope.User)]
    public bool CrossFadingEnabled
    {
      get { return _crossFadeEnabled; }
      set { _crossFadeEnabled = value; }
    }

    /// <summary>
    /// Gets or sets the (lower-case!) list of extensions which will be played with this player.
    /// </summary>
    [Setting(SettingScope.Global)]
    public List<string> SupportedExtensions
    {
      get { return _supportedExtensions; }
      set { _supportedExtensions = value; }
    }

    [Setting(SettingScope.Global, Defaults.VizStreamLatencyCorrectionMilliSecs)]
    public int VizStreamLatencyCorrectionMilliSecs
    {
      get { return (int)_vizStreamLatencyCorrection.TotalMilliseconds; }
      set { _vizStreamLatencyCorrection = TimeSpan.FromMilliseconds(value); }
    }

    public TimeSpan VizStreamLatencyCorrection
    {
      get { return _vizStreamLatencyCorrection; }
      set { _vizStreamLatencyCorrection = value; }
    }

    #endregion
  }
}