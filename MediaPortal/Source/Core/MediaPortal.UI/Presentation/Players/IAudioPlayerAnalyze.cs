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

namespace MediaPortal.UI.Presentation.Players
{
 /// <summary>
  /// Provides access to sound player functionality needed to analyze Viz Data.
  /// </summary>
  public interface IAudioPlayerAnalyze
  {
    /// <summary>
    /// Provides access to sound player functionality needed to render Wave over 32 Bit Floating Point. See BASS_SAMPLE_FLOAT.
    /// </summary> 
    /// <param name="length">The length of requested data.</param>
    /// <param name="waveData32">Returns data.</param>
    bool GetWaveData32(int length, out float[] waveData32);

    /// <summary>
    /// Copies the current FFT data to a buffer.
    /// </summary>
    /// <remarks>
    /// The FFT data in the buffer should consist only of the real number intensity values. This means that if your FFT algorithm returns
    /// complex numbers (as many do), you'd run an algorithm similar to:
    /// <code>
    /// for(int i = 0; i &lt; complexNumbers.Length / 2; i++)
    ///     fftResult[i] = Math.Sqrt(complexNumbers[i].Real * complexNumbers[i].Real + complexNumbers[i].Imaginary * complexNumbers[i].Imaginary);
    /// </code>
    /// </remarks>
    /// <param name="fftDataBuffer">The buffer to copy the FFT data to. The buffer should consist of only non-imaginary numbers.</param>
    /// <returns>True if data was written to the buffer, otherwise false.</returns>
    bool GetFFTData(float[] fftDataBuffer);

    /// <summary>
    /// Gets the index in the FFT data buffer for a given frequency.
    /// </summary>
    /// <param name="frequency">The frequency for which to obtain a buffer index.</param>
    /// <param name="frequencyIndex">If the return value is <c>true</c>, this value will return an index in the FFT data buffer which was returned
    /// by method <see cref="GetFFTData"/>.</param>
    /// <returns><c>true</c>, if the FFT buffer was already created, else <c>false</c>.</returns>
    bool GetFFTFrequencyIndex(int frequency, out int frequencyIndex);

    /// <summary>
    /// Gets the channel levels in dB.
    /// </summary>
    /// <param name="dbLevelL">Field to copy the left channel level to</param>
    /// <param name="dbLevelR">Field to copy the right channel level to</param>
    /// <returns>True if data was written, otherwise false.</returns>
    bool GetChannelLevel(out double dbLevelL, out double dbLevelR);
  }
}
