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

using System;
using System.Collections.Generic;

namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// Playing one media item, in vocabulary that has no engine in it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the seam the DirectShow replacement hangs on. The old tree's players were reached through
  /// <c>MPCore.Player</c> and a COM filter graph; everything a screen needed then (position, length, tracks,
  /// state) is here, and nothing that only a filter graph can answer is - because a question only one engine can
  /// answer is a question every screen then has to ask twice.
  /// </para>
  /// <para>
  /// Threading: <see cref="StateChanged"/>, <see cref="Failed"/>, <see cref="EndOfMedia"/> and
  /// <see cref="PositionChanged"/> arrive on the engine's own thread, not the caller's and not a UI thread. That
  /// is said here rather than hidden because the alternative - marshalling inside the player - needs to know
  /// about dispatchers, which is exactly the knowledge this assembly must not have. A screen marshals for
  /// itself; a test asserts the order.
  /// </para>
  /// <para>
  /// Failures do not throw. <see cref="Open"/> and the transport calls answer <c>false</c> and leave a sentence
  /// in <see cref="LastFailure"/>, because on a TV there is nobody to read a stack trace and the screen still
  /// has to react. Construction is the exception: an engine that cannot be built is a configuration problem -
  /// see <see cref="LibVlcPlayer.TryCreate"/>.
  /// </para>
  /// </remarks>
  public interface IVideoPlayer : IDisposable
  {
    /// <summary>The state as last known. Reading it never blocks and never contacts the engine.</summary>
    PlayerState State { get; }

    /// <summary>What is loaded, or null if nothing has been opened on this instance.</summary>
    MediaLocation? Current { get; }

    /// <summary>The last failure this player reported, or null if it has not failed. Never empty.</summary>
    string? LastFailure { get; }

    /// <summary>Fires on every state change, including the ones caused by the caller.</summary>
    event EventHandler<PlayerStateChangedEventArgs>? StateChanged;

    /// <summary>Fires when the engine reports it cannot continue with this media.</summary>
    event EventHandler<PlayerFailedEventArgs>? Failed;

    /// <summary>Fires when the media ran out. Not fired by <see cref="Stop"/>.</summary>
    event EventHandler? EndOfMedia;

    /// <summary>
    /// Fires as playback advances, and when the length becomes known, so that a progress bar can follow the
    /// events instead of asking several times a second. The values are the engine's own at that moment - after a
    /// seek that means the position the decoder reached, not the one that was asked for; see
    /// <see cref="PlaybackPositionEventArgs"/>.
    /// </summary>
    event EventHandler<PlaybackPositionEventArgs>? PositionChanged;

    /// <summary>
    /// Load a media and start playing it. True means the engine accepted it, which is not the same as it
    /// playing - watch <see cref="StateChanged"/> for that.
    /// </summary>
    bool Open(MediaLocation location);

    /// <summary>Resume from pause, or restart a stopped player from the beginning of the same media.</summary>
    bool Play();

    /// <summary>Pause, keeping position. False when the current state cannot be paused.</summary>
    bool Pause();

    /// <summary>Stop and release the media. The player instance stays usable.</summary>
    bool Stop();

    /// <summary>Length of the media in ms, or 0 when unknown (a live stream, or not open yet).</summary>
    long LengthInMs { get; }

    /// <summary>
    /// Where the media is, in ms. Setting it seeks; reading it after a seek may still return the old position,
    /// which is the engine's timing, not a lie about this one.
    /// </summary>
    long TimeInMs { get; set; }

    /// <summary>Volume 0-100. Values outside that range are clamped, not refused.</summary>
    int Volume { get; set; }

    /// <summary>Muted, independently of <see cref="Volume"/>.</summary>
    bool Muted { get; set; }

    /// <summary>
    /// The selectable tracks of one kind, in the order the engine lists them. Empty is a normal answer - most
    /// files have no subtitle track and one audio track that is not worth a menu.
    /// </summary>
    IReadOnlyList<PlaybackTrack> Tracks(TrackKind kind);

    /// <summary>Switch to a track; an id of -1 switches that kind off.</summary>
    bool SelectTrack(PlaybackTrack track);

    /// <summary>
    /// The id of the track this player is using for <paramref name="kind"/>, or -1 when that kind is off.
    /// </summary>
    /// <remarks>
    /// Asked, not remembered by the caller: a media can pick its own default track when it starts, so a screen
    /// that marked what the viewer last chose would mark the wrong row for every media nobody touched.
    /// </remarks>
    int CurrentTrackId(TrackKind kind);

    /// <summary>
    /// The native window the engine may draw into: an HWND on Windows, 0 for none. 0 is a supported answer, not
    /// a degraded one - it is how a background transcode or a headless test runs.
    /// </summary>
    /// <returns>
    /// True when this engine can draw into that handle. False means the picture stays nowhere and
    /// <see cref="LastFailure"/> says why; on platforms whose window system this assembly does not know yet
    /// (Android's Surface) that is the answer, and an honest one, rather than a handle quietly ignored.
    /// </returns>
    bool AttachTo(nint videoTarget);

    /// <summary>
    /// Give the pictures to <paramref name="frameSink"/> instead of to a window: the engine decodes into memory
    /// the sink owns, and whatever reads the sink decides where they end up - a control, a thumbnail, nowhere.
    /// </summary>
    /// <returns>
    /// True when this engine will fill the sink. False, with <see cref="LastFailure"/> explaining, when it cannot
    /// or when this player already gave its pictures to something else: a window and a sink are two answers to one
    /// question, and a player that answered both would leave one of them showing a picture it stopped being given.
    /// A player that handed its pictures to the same sink before answers true again, so a screen that re-attaches
    /// on every navigation is not punished for it.
    /// </returns>
    /// <remarks>
    /// Ask before <see cref="Open"/>, not after: the engine decides how it will deliver pictures when the media
    /// starts, and a sink introduced after that is a message delivered too late.
    /// </remarks>
    bool AttachTo(VideoFrameSink frameSink);

    /// <summary>
    /// Frames the engine has counted for the media that is open now. All zeros when nothing is open or when the
    /// engine does not count - which is why the answer is a value and not a nullable one: "no media" and "no
    /// counters" both read as nothing, and only the counters' behaviour over time tells them apart.
    /// </summary>
    /// <remarks>
    /// Reading this never starts or stops anything and never throws. See <see cref="PlayerStatistics"/> for why
    /// a state and a picture need a third thing to compare them with.
    /// </remarks>
    PlayerStatistics Statistics { get; }
  }
}
