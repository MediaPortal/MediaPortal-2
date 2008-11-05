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

using System;
using MediaPortal.Core.Settings;

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Contains the bass player user configuration.
  /// </summary>
  public class BassPlayerSettings
  {
    public static class Constants
    {
      public const int Auto = -1;
    }

    public static class Defaults
    {
      public const OutputMode OutputMode = Media.Players.BassPlayer.OutputMode.DirectSound;
      public const string DirectSoundDevice = "";
      public const int DirectSoundBufferSizeMilliSecs = 200;
      public const string ASIODevice = "";
      public const int ASIOFirstChan = Constants.Auto;
      public const int ASIOLastChan = Constants.Auto;
      public const bool ASIOUseMaxBufferSize = true;
      public const int ASIOMaxRate = Constants.Auto;
      public const int ASIOMinRate = Constants.Auto;
      public const int PlaybackBufferSizeMilliSecs = 500;
      public const int SeekIncrementSeconds = 20;
      public const PlaybackMode PlaybackMode = Media.Players.BassPlayer.PlaybackMode.Normal;
      public const int FadeDurationMilliSecs = 500;
      public const int CrossFadeDurationMilliSecs = 5;
      public const int VizStreamLatencyCorrectionMilliSecs = 0;
      public const string SupportedExtensions =
        ".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1,.aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi,.kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv";
    }

    #region Fields

    private OutputMode _OutputMode = Defaults.OutputMode;
    private string _DirectSoundDevice = Defaults.DirectSoundDevice;
    private TimeSpan _DirectSoundBufferSize = TimeSpan.FromMilliseconds(Defaults.DirectSoundBufferSizeMilliSecs);
    private string _ASIODevice = Defaults.ASIODevice;
    private int _ASIOFirstChan = Defaults.ASIOFirstChan;
    private int _ASIOLastChan = Defaults.ASIOLastChan;
    private int _ASIOMaxRate = Defaults.ASIOMaxRate;
    private int _ASIOMinRate = Defaults.ASIOMinRate;
    private bool _ASIOUseMaxBufferSize = Defaults.ASIOUseMaxBufferSize;
    private TimeSpan _PlaybackBufferSize = TimeSpan.FromMilliseconds(Defaults.PlaybackBufferSizeMilliSecs);
    private TimeSpan _SeekIncrement = TimeSpan.FromSeconds(Defaults.SeekIncrementSeconds);
    private PlaybackMode _PlaybackMode = Defaults.PlaybackMode;
    private TimeSpan _FadeDuration = TimeSpan.FromMilliseconds(Defaults.FadeDurationMilliSecs);
    private TimeSpan _CrossFadeDuration = TimeSpan.FromMilliseconds(Defaults.CrossFadeDurationMilliSecs);
    private TimeSpan _VizStreamLatencyCorrection = TimeSpan.FromMilliseconds(Defaults.VizStreamLatencyCorrectionMilliSecs);
    private string _SupportedExtensions = Defaults.SupportedExtensions;

    #endregion

    #region Public members

    [Setting(SettingScope.Global, Defaults.OutputMode)]
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

    [Setting(SettingScope.Global, Defaults.ASIODevice)]
    public string ASIODevice
    {
      get { return _ASIODevice; }
      set { _ASIODevice = value; }
    }

    [Setting(SettingScope.Global, Defaults.ASIOFirstChan)]
    public int ASIOFirstChan
    {
      get { return _ASIOFirstChan; }
      set { _ASIOFirstChan = value; }
    }

    [Setting(SettingScope.Global, Defaults.ASIOLastChan)]
    public int ASIOLastChan
    {
      get { return _ASIOLastChan; }
      set { _ASIOLastChan = value; }
    }

    [Setting(SettingScope.Global, Defaults.ASIOMaxRate)]
    public int ASIOMaxRate
    {
      get { return _ASIOMaxRate; }
      set { _ASIOMaxRate = value; }
    }

    [Setting(SettingScope.Global, Defaults.ASIOMinRate)]
    public int ASIOMinRate
    {
      get { return _ASIOMinRate; }
      set { _ASIOMinRate = value; }
    }

    [Setting(SettingScope.Global, Defaults.ASIOUseMaxBufferSize)]
    public bool ASIOUseMaxBufferSize
    {
      get { return _ASIOUseMaxBufferSize; }
      set { _ASIOUseMaxBufferSize = value; }
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

    [Setting(SettingScope.User, Defaults.PlaybackMode)]
    public PlaybackMode PlaybackMode
    {
      get { return _PlaybackMode; }
      set { _PlaybackMode = value; }
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

    [Setting(SettingScope.Global, Defaults.SupportedExtensions)]
    public string SupportedExtensions
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

    public BassPlayerSettings()
    {
    }

    #endregion
  }
}
