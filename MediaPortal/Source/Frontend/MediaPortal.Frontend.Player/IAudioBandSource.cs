/*
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

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// What an engine answers when it knows the level of each band of the sound but cannot name the sound's own
  /// numbers: <see cref="IAudioSpectrumSource"/> hands over the samples' magnitudes and leaves the sorting into
  /// bands to the screen, this one hands over the bands themselves, already sorted, each level between nought
  /// and one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The two seams exist side by side because there are two honest kinds of answer and hiding the difference
  /// behind one would make a screen believe numbers it was never given. An engine that measured the sound
  /// itself - the reference's BASS player counting over its own stream (UI/Players/BassPlayer/PlayerComponents/
  /// PlaybackBuffer.cs:384) - can give magnitudes and a rate. An engine that was only ever shown a
  /// <em>picture</em> of the sound can give one band per slice of that picture and nothing beside it, which is
  /// exactly what libVLC offers: its visualisation filter draws the spectrum, and every attempt at the numbers
  /// behind the drawing was refused in this project's own runs -
  /// </para>
  /// <list type="bullet">
  /// <item>
  /// <description>
  /// <c>MediaPlayer.SetEqualizer</c> and <c>Equalizer.Amp</c> answer with whatever was last set: ten bands
  /// standing at nought decibels while music played, because the equalizer is a control, not a meter.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// <c>MediaPlayer.SetAudioCallbacks</c> was tried with libVLC 3.0.19 and LibVLCSharp 3.10.2 and stalled: the
  /// format callback was answered (44100 hertz, two channels) and no sample callback ever came, no byte arrived,
  /// the position stayed at nought, and stopping the player took the process down with it (a second after the
  /// refusal the exit code read 0xC000001D). Its own documentation had said as much - "The audio callbacks
  /// override any other audio output mechanism" (LibVLCSharp.xml, member M:LibVLCSharp.Shared.MediaPlayer.
  /// SetAudioCallbacks) - so the samples are only handed over by taking over the playing of them.
  /// </description>
  /// </item>
  /// </list>
  /// <para>
  /// A screen that is offered both asks this one first, because a band answer that arrived is a measurement,
  /// while the magnitude seam on an engine that cannot fill it is silence.
  /// </para>
  /// </remarks>
  public interface IAudioBandSource
  {
    /// <summary>
    /// How many bands an answer from <see cref="GetBandLevels"/> carries, lowest band first.
    /// </summary>
    int BandCount { get; }

    /// <summary>
    /// Fills <paramref name="levels"/> with one level per band, lowest band first, each between nought and one.
    /// Returns false when nothing can be said - nothing is playing, no picture has arrived yet - which leaves the
    /// bars to fall rather than to freeze at the level the last loud moment left them at.
    /// </summary>
    bool GetBandLevels(float[] levels);
  }

  /// <summary>
  /// The other half of <see cref="IAudioBandSource"/>: who is asked to start and stop the listening the bands are
  /// read from. Split because the two questions are asked by different code - a row of bars is bound to the source
  /// and reads it on its clock, while the screen that shows or hides those bars is the only one that knows whether
  /// the money listening costs is being looked at - and because an engine that already hands its sound out for
  /// free has a source and nothing to be asked.
  /// </summary>
  public interface IAudioSpectrumWatcher
  {
    /// <summary>
    /// Asks the engine to look at its own sound and answer <see cref="IAudioBandSource.GetBandLevels"/> from it.
    /// Said again by the screen every time the bars come back on show; an engine already listening does nothing.
    /// Listening costs something - an engine that has no bands of its own pays a second decoder for it - which is
    /// why the same screen says when it stops wanting them.
    /// </summary>
    void WatchSpectrum();

    /// <summary>
    /// Says that the bands are no longer looked at and the engine may let go of what listening cost. Said whenever
    /// a screen leaves the audio arrangement, whether or not it had started the listening: letting go of nothing
    /// is permitted.
    /// </summary>
    void StopWatchingSpectrum();
  }
}
