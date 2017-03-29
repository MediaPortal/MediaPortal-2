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

// Todo: Implement. This is just a passthrough stub.

using System;
using MediaPortal.Extensions.BassLibraries;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  /// <summary>
  /// Performs upmixing and downmixing.
  /// </summary>
  public class UpDownMixer : IDisposable
  {
    #region Fields

    private BassStream _inputStream;
    private BassStream _outputStream;
    private BassStream _mixerStream = null;
    private bool _initialized;
    private int _mixerHandle = 0;
    private float[,] _mixingMatrix = null;

    private const BASSFlag MIXER_FLAGS = BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_STREAM_DECODE;

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion

    #region Public members

    /// <summary>
    /// Gets the current inputstream as set by <see cref="SetInputStream"/>.
    /// </summary>
    public BassStream InputStream
    {
      get { return _inputStream; }
    }

    /// <summary>
    /// Gets the output Bass stream.
    /// In case of Upmixing a Mixer is returned.
    /// </summary>
    public BassStream OutputStream
    {
      get { return _mixerStream ?? _outputStream; }
    }

    /// <summary>
    /// Sets the Bass inputstream.
    /// </summary>
    /// <param name="stream">The stream to be used as input stream.</param>
    public void SetInputStream(BassStream stream)
    {
      ResetInputStream();
      _inputStream = stream;
      _outputStream = stream;
      CheckForUpDownMixing();
      _initialized = true;
    }

    /// <summary>
    /// Resets the instance to its uninitialized state.
    /// </summary>
    public void ResetInputStream()
    {
      if (_initialized)
      {
        _initialized = false;

        // Dispose has to be done if we have our own OutputStream
        //_OutputStream.Dispose();
        _outputStream = null;
        _inputStream = null;
        _mixerStream = null;
      }
    }

    #endregion

    #region Private members

    /// <summary>
    /// Check, if we need Up- or Downmixing
    /// </summary>
    private void CheckForUpDownMixing()
    {
      var outputChannels = 2;

      // Note: Currently only the special case of creating a Mixing matrix for 3.0 or 5.0 files
      // is implemented. If the team decides for a general Up- / Downmixing to be implemnted,
      // then the code form MP1 shall be ported.
      switch (_inputStream.Channels)
      {
        case 3:
        {
            Log.Info("BASS: Found a 3 channel file. Set upmixing with LFE, LR, RR set to silent");
            _mixingMatrix = CreateThreeDotZeroUpMixMatrix();
            outputChannels = 6;
            break;
        }

        case 4:
        {
            Log.Info("BASS: Found a 4 channel file. Set upmixing with Center and LFE set to silent");
            _mixingMatrix = CreateFourDotZeroUpMixMatrix();
            outputChannels = 4; // Quadrophonic should stay with four channels
            break;
        }

        case 5:
        {
            Log.Info("BASS: Found a 5 channel file. Set upmixing with LFE set to silent");
            _mixingMatrix = CreateFiveDotZeroUpMixMatrix();
            outputChannels = 6;
            break;
        }
      }

      if (_mixingMatrix != null)
      {
        CreateMixer(outputChannels);
      }
    }

    /// <summary>
    /// Create a mixer to be used as Output stream.
    /// We currently need this in case of Up- or Downmixing
    /// </summary>
    private void CreateMixer(int channels)
    {
      _mixerHandle = BassMix.BASS_Mixer_StreamCreate(_inputStream.SampleRate, channels, MIXER_FLAGS);
      if (_mixerHandle == BassConstants.BassInvalidHandle)
        throw new BassLibraryException("BASS_Mixer_StreamCreate");

      _mixerStream = BassStream.Create(_mixerHandle);

      // Now Attach the Input Stream to the mixer
      try
      {
        Bass.BASS_ChannelLock(_mixerHandle, true);

        bool result = BassMix.BASS_Mixer_StreamAddChannel(_mixerHandle, _inputStream.Handle,
                                          BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_MIXER_BUFFER |
                                          BASSFlag.BASS_MIXER_MATRIX | BASSFlag.BASS_MIXER_DOWNMIX |
                                          BASSFlag.BASS_STREAM_AUTOFREE
                                          );
        if (!result)
          throw new BassLibraryException("BASS_UpDownMix_StreamAddChannel");
      }
      finally
      {
        Bass.BASS_ChannelLock(_mixerHandle, false);
      }

      if (_mixingMatrix != null)
      {
        bool result = BassMix.BASS_Mixer_ChannelSetMatrix(_inputStream.Handle, _mixingMatrix);
        if (!result)
          throw new BassLibraryException("BASS_UpDownMix_SetMixingMatrix");
      }
    }

    /// <summary>
    /// Create a mixing matrix to upmix a 3.0 file to 5.1 or 7.1
    /// </summary>
    /// <returns></returns>
    private float[,] CreateThreeDotZeroUpMixMatrix()
    {
      float[,] mixMatrix = new float[8, 3] {
            {1,0,0}, // left front out = left front in
	          {0,1,0}, // right front out = right front in
	          {0,0,1}, // centre out = centre in
	          {0,0,0}, // LFE out = silent
	          {0,0,0}, // left rear out = silent
	          {0,0,0}, // right rear out = silent
            {0,0,0}, // left back out = silent
            {0,0,0}  // right back out = silent
           };
      return mixMatrix;
    }

    /// <summary>
    /// Create a mixing matrix to upmix a 4.0 file to 5.1 or 7.1
    /// </summary>
    /// <returns></returns>
    private float[,] CreateFourDotZeroUpMixMatrix()
    {
      float[,] mixMatrix = new float[8, 4] {
            {1,0,0,0}, // left front out = left front in
	          {0,1,0,0}, // right front out = right front in
	          {0,0,0,0}, // centre out = silent
	          {0,0,0,0}, // LFE out = silent
	          {0,0,1,0}, // left rear out = left rear in
	          {0,0,0,1}, // right rear out = right rear in
            {0,0,0,0}, // left back out = silent
            {0,0,0,0}  // right back out = silent
           };
      return mixMatrix;
    }

    /// <summary>
    /// Create a mixing matrix to upmix a 5.0 file to 5.1 or 7.1
    /// </summary>
    /// <returns></returns>
    private float[,] CreateFiveDotZeroUpMixMatrix()
    {
      float[,] mixMatrix = new float[8, 5] {
            {1,0,0,0,0}, // left front out = left front in
	          {0,1,0,0,0}, // right front out = right front in
	          {0,0,1,0,0}, // centre out = centre in
	          {0,0,0,0,0}, // LFE out = silent
	          {0,0,0,1,0}, // left rear out = left rear in
	          {0,0,0,0,1}, // right rear out = right rear in
            {0,0,0,0,0}, // left back out = silent
            {0,0,0,0,0}  // right back out = silent
           };
      return mixMatrix;
    }

    #endregion
  }
}
