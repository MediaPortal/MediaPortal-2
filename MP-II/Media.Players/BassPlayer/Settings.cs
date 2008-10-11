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

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Contains user configuration.
  /// </summary>
  public class Settings
  {
    public static class Defaults
    {
      public const OutputMode OutputMode = Media.Players.BassPlayer.OutputMode.DirectSound;
      public const string DirectSoundDevice = "";
      public static TimeSpan DirectSoundBufferSize = TimeSpan.FromMilliseconds(200);
      public const string ASIODevice = "";
      public const int ASIOFirstChan = Constants.Auto;
      public const int ASIOLastChan = Constants.Auto;
      public static TimeSpan PlaybackBufferSize = TimeSpan.FromMilliseconds(500);
      public static TimeSpan SeekIncrement = TimeSpan.FromSeconds(20);
      public const PlaybackMode PlaybackMode = Media.Players.BassPlayer.PlaybackMode.Normal;
      public static TimeSpan FadeDuration = TimeSpan.FromMilliseconds(500);
      public static TimeSpan CrossFadeDuration = TimeSpan.FromSeconds(5);
      public static TimeSpan VizStreamLatencyCorrection = TimeSpan.FromMilliseconds(0);
      public const string SupportedExtensions =
        ".mp3,.ogg,.wav,.flac,.cda,.asx,.dts,.mod,.mo3,.s3m,.xm,.it,.mtm,.umx,.mdz,.s3z,.itz,.xmz,.mp2,.mp1,.aiff,.m2a,.mpa,.m1a,.swa,.aif,.mp3pro,.aac,.mp4,.m4a,.m4b,.ac3,.aac,.mov,.ape,.apl,.midi,.mid,.rmi,.kar,.mpc,.mpp,.mp+,.ofr,.ofs,.spx,.tta,.wma,.wv";
    }

    #region Fields

    private OutputMode _OutputMode = Defaults.OutputMode;
    private string _DirectSoundDevice = Defaults.DirectSoundDevice;
    private TimeSpan _DirectSoundBufferSize = Defaults.DirectSoundBufferSize;
    private string _ASIODevice = Defaults.ASIODevice;
    private int _ASIOFirstChan = Defaults.ASIOFirstChan;
    private int _ASIOLastChan = Defaults.ASIOLastChan;
    private TimeSpan _PlaybackBufferSize = Defaults.PlaybackBufferSize;
    private TimeSpan _SeekIncrement = Defaults.SeekIncrement;
    private PlaybackMode _PlaybackMode = Defaults.PlaybackMode;
    private TimeSpan _FadeDuration = Defaults.FadeDuration;
    private TimeSpan _CrossFadeDuration = Defaults.CrossFadeDuration;
    private TimeSpan _VizStreamLatencyCorrection = Defaults.VizStreamLatencyCorrection;
    private string _SupportedExtensions = Defaults.SupportedExtensions;

    #endregion

    #region Public members

    public OutputMode OutputMode
    {
      get { return _OutputMode; }
      set { _OutputMode = value; }
    }

    public string DirectSoundDevice
    {
      get { return _DirectSoundDevice; }
      set { _DirectSoundDevice = value; }
    }

    public TimeSpan DirectSoundBufferSize
    {
      get { return _DirectSoundBufferSize; }
      set { _DirectSoundBufferSize = value; }
    }

    public string ASIODevice
    {
      get { return _ASIODevice; }
      set { _ASIODevice = value; }
    }

    public int ASIOFirstChan
    {
      get { return _ASIOFirstChan; }
      set { _ASIOFirstChan = value; }
    }

    public int ASIOLastChan
    {
      get { return _ASIOLastChan; }
      set { _ASIOLastChan = value; }
    }

    public TimeSpan PlaybackBufferSize
    {
      get { return _PlaybackBufferSize; }
      set { _PlaybackBufferSize = value; }
    }

    public TimeSpan SeekIncrement
    {
      get { return _SeekIncrement; }
      set { _SeekIncrement = value; }
    }

    public PlaybackMode PlaybackMode
    {
      get { return _PlaybackMode; }
      set { _PlaybackMode = value; }
    }

    public TimeSpan FadeDuration
    {
      get { return _FadeDuration; }
      set { _FadeDuration = value; }
    }

    public TimeSpan CrossFadeDuration
    {
      get { return _CrossFadeDuration; }
      set { _CrossFadeDuration = value; }
    }

    public string SupportedExtensions
    {
      get { return _SupportedExtensions; }
      set { _SupportedExtensions = value; }
    }

    public TimeSpan VizStreamLatencyCorrection
    {
      get { return _VizStreamLatencyCorrection; }
      set { _VizStreamLatencyCorrection = value; }
    }

    public Settings()
    {
    }

    #endregion
  }
}
