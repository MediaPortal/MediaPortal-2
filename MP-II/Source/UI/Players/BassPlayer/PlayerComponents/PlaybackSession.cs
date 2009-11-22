#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
  public partial class BassPlayer
  {
    /// <summary>
    /// Represents a single playbacksession. 
    /// A playback session is a sequence of sources that have the same 
    /// number of channels and the same samplerate. Within a playback 
    /// session we can perform crossfading and gapless switching.
    /// </summary>
    class PlaybackSession
    {
      #region Static members

      /// <summary>
      /// Creates and initializes an new instance.
      /// </summary>
      /// <param name="player">Reference to containing IPlayer object.</param>
      /// <returns>The new instance.</returns>
      public static PlaybackSession Create(BassPlayer player, int channels, int sampleRate, bool isPassThrough)
      {
        PlaybackSession playbackSession = new PlaybackSession(player, channels, sampleRate, isPassThrough);
        playbackSession.Initialize();
        return playbackSession;
      }

      #endregion

      #region Fields

      private BassPlayer _Player;
      private int _Channels;
      private int _SampleRate;
      private bool _IsPassThrough;

      #endregion

      #region Public members

      /// <summary>
      /// Gets the number of channels for the session.
      /// </summary>
      public int Channels
      {
        get { return _Channels; }
      }

      /// <summary>
      /// Gets the samplerate for the session.
      /// </summary>
      public int SampleRate
      {
        get { return _SampleRate; }
      }

      /// <summary>
      /// Gets whether the session is in AC3/DTS passthrough mode.
      /// </summary>
      public bool IsPassThrough
      {
        get { return _IsPassThrough; }
      }

      /// <summary>
      /// Determines whether a given inputsource fits in this session or not.
      /// </summary>
      /// <param name="inputSource"></param>
      /// <returns></returns>
      public bool MatchesInputSource(IInputSource inputSource)
      {
        return
            inputSource.OutputStream.Channels == Channels &&
            inputSource.OutputStream.SampleRate == SampleRate &&
            inputSource.OutputStream.IsPassThrough == IsPassThrough;
      }

      /// <summary>
      /// Ends and discards the playback session.
      /// </summary>
      public void End()
      {
        _Player._OutputDeviceManager.StopDevice();
        
        _Player._OutputDeviceManager.ResetInputStream();
        _Player._PlaybackBuffer.ResetInputStream();
        _Player._WinAmpDSPProcessor.ResetInputStream();
        _Player._VSTProcessor.ResetInputStream();
        _Player._UpDownMixer.ResetInputStream();
        _Player._InputSourceSwitcher.Reset();
      }

      #endregion

      #region Private members

      private PlaybackSession(BassPlayer player, int channels, int sampleRate, bool isPassThrough)
      {
        _Player = player;
        _Channels = channels;
        _SampleRate = sampleRate;
        _IsPassThrough = isPassThrough;
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      private void Initialize()
      {
        // In case we are starting a webstream, do a fade-in.
        IInputSource inputSource = _Player._InputSourceQueue.Peek();
        bool fadeIn = (inputSource.MediaItemType == MediaItemType.WebStream);

        _Player._InputSourceSwitcher.InitToInputSource();

        _Player._UpDownMixer.SetInputStream(_Player._InputSourceSwitcher.OutputStream);
        _Player._VSTProcessor.SetInputStream(_Player._UpDownMixer.OutputStream);
        _Player._WinAmpDSPProcessor.SetInputStream(_Player._VSTProcessor.OutputStream);
        _Player._PlaybackBuffer.SetInputStream(_Player._WinAmpDSPProcessor.OutputStream);
        _Player._OutputDeviceManager.SetInputStream(_Player._PlaybackBuffer.OutputStream);
        
        _Player._OutputDeviceManager.StartDevice(fadeIn);
      }

      #endregion
    }
  }
}