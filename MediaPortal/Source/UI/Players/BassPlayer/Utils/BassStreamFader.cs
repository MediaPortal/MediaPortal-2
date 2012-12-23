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
using System.Threading;
using Un4seen.Bass;

namespace MediaPortal.UI.Players.BassPlayer.Utils
{
  /// <summary>
  /// Performs fade-in and fade-out on a Bass stream.
  /// </summary>
  /// <remarks>
  /// Only modifies the volume attribute.
  /// The actual fading must be implemented in code that reads from the stream!
  /// </remarks>
  internal class BassStreamFader
  {
    #region Fields

    protected readonly BassStream _stream;
    protected readonly TimeSpan _duration;
    protected readonly int _durationMS;

    #endregion

    #region Public members

    public BassStreamFader(BassStream stream, TimeSpan duration)
    {
      _stream = stream;
      _duration = duration;
      _durationMS = Convert.ToInt32(duration.TotalMilliseconds);
    }

    public TimeSpan Duration
    {
      get { return _duration; }
    }

    public int DurationMS
    {
      get { return _durationMS; }
    }

    /// <summary>
    /// Prepares for a fadein; sets the volume to zero.
    /// </summary>
    public void PrepareFadeIn()
    {
      SetVolume(0f);
    }

    /// <summary>
    /// Performs a fadein.
    /// </summary>
    /// <remarks>
    /// If the fade duration is set to 0, volume is set to 100% instantly.
    /// </remarks>
    /// <param name="async">If set to <c>true</c>, the fading process is performed asynchronously and the execution thread
    /// returns at once. If set to <c>false</c>, the execution thread will be blocked until the fading has finished.
    /// The fade duration is <see cref="DurationMS"/> milli seconds.</param>
    public void FadeIn(bool async)
    {
      if (_durationMS != 0)
      {
        SlideVolume(1f);
        if (!async)
          WaitForVolumeSlide();
      }
      else
        SetVolume(1f);
    }

    /// <summary>
    /// Performs a fadeout.
    /// </summary>
    /// <remarks>
    /// If the fade duration is set to 0, volume is set to 0% instantly.
    /// </remarks>
    /// <param name="async">If set to <c>true</c>, the fading process is performed asynchronously and the execution thread
    /// returns at once. If set to <c>false</c>, the execution thread will be blocked until the fading has finished.
    /// The fade duration is <see cref="DurationMS"/> milli seconds.</param>
    public void FadeOut(bool async)
    {
      if (_durationMS != 0)
      {
        SlideVolume(0f);
        if (!async)
          WaitForVolumeSlide();
      }
      else
        SetVolume(0f);
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Blocks the calling thread until a volume slide has finished.
    /// </summary>
    protected void WaitForVolumeSlide()
    {
      while (Bass.BASS_ChannelIsSliding(_stream.Handle, BASSAttribute.BASS_ATTRIB_VOL))
        Thread.Sleep(10);
    }

    /// <summary>
    /// Sets the volume to the given value instantly.
    /// </summary>
    /// <param name="volume">0.0f-1.0f -> 0-100%</param>
    protected void SetVolume(float volume)
    {
      if (!Bass.BASS_ChannelSetAttribute(_stream.Handle, BASSAttribute.BASS_ATTRIB_VOL, volume))
        throw new BassLibraryException("BASS_ChannelSetAttribute");
    }

    /// <summary>
    /// Slides the volume to the given value over the period of time given by <see cref="Duration"/>.
    /// </summary>
    /// <param name="volume">0.0f-1.0f -> 0-100%</param>
    protected void SlideVolume(float volume)
    {
      if (!Bass.BASS_ChannelSlideAttribute(_stream.Handle, BASSAttribute.BASS_ATTRIB_VOL, volume, _durationMS))
        throw new BassLibraryException("BASS_ChannelSlideAttribute");
    }

    #endregion
  }
}