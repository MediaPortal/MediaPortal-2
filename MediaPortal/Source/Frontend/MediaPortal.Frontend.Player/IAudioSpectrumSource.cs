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
  /// What an engine has to be able to say for a screen to draw the sound as bars: the level of each frequency
  /// band of what is playing right now, and at what rate the sound runs.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The seam is the reference's own. Its spectrum control asks exactly one question of whatever is playing -
  /// <c>player.GetFFTData(_channelData)</c> (UI/UiComponents/Media/Controls/AudioSpectrumAnalyzer.cs:377) -
  /// through exactly one interface, <c>IAudioPlayerAnalyze.GetFFTData</c>
  /// (Core/MediaPortal.UI/Presentation/Players/IAudioPlayerAnalyze.cs:52), and in the whole tree only the BASS
  /// player answers it, because BASS keeps a second stream alongside the playing one to count over
  /// (UI/Players/BassPlayer/PlayerComponents/PlaybackBuffer.cs:384, and its two other answerers
  /// BassPlayer.cs:479 and OutputDevices/WASAPIOutputDevice.cs:457 - which together say how much of the reference
  /// is BASS rather than skin). This frontend plays through libVLC, and libVLC is asked plainly: it has nothing
  /// that measures. What it offers is a filter to turn (<c>MediaPlayer.SetEqualizer</c>) and a way to be handed
  /// the decoded samples itself (<c>MediaPlayer.SetAudioCallbacks</c>), of which its own documentation says
  /// "The audio callbacks override any other audio output mechanism. If the callbacks are set, LibVLC will not
  /// output audio in any way" (LibVLCSharp 3.10.2, LibVLCSharp.xml, member
  /// M:LibVLCSharp.Shared.MediaPlayer.SetAudioCallbacks) - so the samples can be looked at only by taking over
  /// the playing of them, which is not a trade to make with a listener's sound unnoticed.
  /// </para>
  /// <para>
  /// So the bars and their source are kept apart on purpose, and the distance between them is this nine-line
  /// interface. A screen draws what an engine tells it; an engine that cannot tell stays silent and the bars
  /// stand at their floor, which is the same answer the reference gives when its player is not active
  /// (AudioSpectrumAnalyzer.cs:490-493: keep drawing, level nought, so the peaks fall). Today no engine in this
  /// frontend implements this, and that is reported rather than hidden: the bars are drawn, and empty.
  /// </para>
  /// </remarks>
  public interface IAudioSpectrumSource
  {
    /// <summary>
    /// The rate the sound is running at, in hertz, because it is what turns a frequency into a band: the bands
    /// of an <see cref="GetFftData"/> answer lie between nought and half this number, evenly. The reference
    /// asks the same question of its stream when it works out which band a frequency falls in
    /// (PlaybackBuffer.cs:389-395).
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Fills <paramref name="bands"/> with the level of each frequency band of the sound playing now, from the
    /// lowest band upwards, each level between nought and one. Returns false when nothing can be said - nothing
    /// is playing, the engine is stopped, or this engine has no way of listening to itself - which leaves the
    /// bars to fall rather than to freeze at the last level.
    /// </summary>
    bool GetFftData(float[] bands);
  }
}
