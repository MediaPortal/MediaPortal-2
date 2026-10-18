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
using System.Threading;

using MediaPortal.Frontend.Player;

namespace Tests.Frontend
{
  /// <summary>
  /// A player that answers what a test tells it to, so that the screen's behaviour can be measured without an
  /// engine, a codec, or a display.
  /// </summary>
  /// <remarks>
  /// <para>
  /// It is deliberately not a mock framework: the interesting properties of a fake player are that it remembers
  /// what it was asked to do and that it can be told to report something from another thread, which is how the
  /// real engine behaves (see <see cref="IVideoPlayer"/>'s threading note) and the only way to test that a screen
  /// survives it.
  /// </para>
  /// </remarks>
  internal sealed class FakePlayer : IVideoPlayer
  {
    /// <summary>Every <see cref="Open"/> call, in order.</summary>
    public List<MediaLocation> Opened { get; } = new List<MediaLocation>();

    public List<string> Calls { get; } = new List<string>();

    public PlayerState State { get; private set; } = PlayerState.Idle;
    public MediaLocation? Current { get; private set; }
    public string? LastFailure { get; private set; }

    public long LengthInMs { get; set; } = 60_000;
    public long TimeInMs { get; set; }
    public int Volume { get; set; } = 100;
    public bool Muted { get; set; }

    /// <summary>
    /// What the fake reports for <see cref="IVideoPlayer.Statistics"/>. Settable, so that a test can script the
    /// one thing no other property can express: a player that says it is playing while nothing decodes.
    /// </summary>
    public PlayerStatistics Statistics { get; set; } = PlayerStatistics.None;

    /// <summary>False makes <see cref="Open"/> refuse with <see cref="RefuseWith"/>.</summary>
    public bool AcceptsOpen { get; set; } = true;

    public string? RefuseWith { get; set; }

    public bool PausesWhenAsked { get; set; } = true;
    public bool StopsWhenAsked { get; set; } = true;

    /// <summary>
    /// Whether stopping makes the player forget how long the media was, as libVLC really does once a media is
    /// closed. A screen that works out "how far did they get" after the stop is over gets a zero length and a
    /// meaningless percentage, so one test needs an engine that behaves like that.
    /// </summary>
    public bool ForgetsLengthOnStop { get; set; }

    public IReadOnlyList<PlaybackTrack> Tracks(TrackKind kind)
    {
      Calls.Add("Tracks:" + kind);
      return kind == TrackKind.Audio ? Audio : Subtitles;
    }

    /// <summary>What <see cref="IVideoPlayer.Tracks"/> returns for audio. Settable so a media can have tracks.</summary>
    public IReadOnlyList<PlaybackTrack> Audio { get; set; } = Array.Empty<PlaybackTrack>();

    /// <summary>What <see cref="IVideoPlayer.Tracks"/> returns for subtitles.</summary>
    public IReadOnlyList<PlaybackTrack> Subtitles { get; set; } = Array.Empty<PlaybackTrack>();

    /// <summary>The id the fake reports as in use. -1 means off, as an engine reports it.</summary>
    public int CurrentAudioId { get; set; } = -1;

    public int CurrentSubtitleId { get; set; } = -1;

    public bool SelectTrack(PlaybackTrack track)
    {
      Calls.Add("SelectTrack:" + track.Id);
      if (RefusesTrackSelection)
      {
        LastFailure = "the player would not switch.";
        return false;
      }
      if (track.Kind == TrackKind.Audio)
        CurrentAudioId = track.Id;
      else
        CurrentSubtitleId = track.Id;
      return true;
    }

    /// <summary>False makes <see cref="SelectTrack"/> fail, as an engine does with a track it cannot decode.</summary>
    public bool RefusesTrackSelection { get; set; }

    public int CurrentTrackId(TrackKind kind)
    {
      Calls.Add("CurrentTrackId:" + kind);
      return kind == TrackKind.Audio ? CurrentAudioId : CurrentSubtitleId;
    }

    public bool AttachTo(nint videoTarget)
    {
      Calls.Add("AttachTo:" + videoTarget);
      if (videoTarget == 0)
      {
        LastFailure = "no window was given.";
        return false;
      }
      LastFailure = null;
      return true;
    }

    /// <summary>
    /// The sink this fake was asked to fill. A test writes pictures into it and then looks at what the screen made
    /// of them, which is how the drawing half of playback is covered on a host with no video decoder in it.
    /// </summary>
    public VideoFrameSink? Frames
    {
      get; private set;
    }

    public bool AttachTo(VideoFrameSink frameSink)
    {
      Calls.Add("AttachToFrames");
      Frames = frameSink;
      LastFailure = null;
      return true;
    }

    public bool Open(MediaLocation location)
    {
      Opened.Add(location);
      Calls.Add("Open:" + location.Describe());
      if (!AcceptsOpen)
      {
        LastFailure = RefuseWith ?? "the engine would not take it.";
        return false;
      }
      LastFailure = null;
      Current = location;
      RaiseState(PlayerState.Opening);
      RaiseState(PlayerState.Playing);
      return true;
    }

    public bool Play()
    {
      Calls.Add("Play");
      RaiseState(PlayerState.Playing);
      return true;
    }

    public bool Pause()
    {
      Calls.Add("Pause");
      if (!PausesWhenAsked)
      {
        LastFailure = "this media cannot be paused.";
        return false;
      }
      RaiseState(PlayerState.Paused);
      return true;
    }

    public bool Stop()
    {
      Calls.Add("Stop");
      if (!StopsWhenAsked)
      {
        LastFailure = "the engine would not stop.";
        return false;
      }
      if (ForgetsLengthOnStop)
        LengthInMs = 0;
      RaiseState(PlayerState.Stopped);
      return true;
    }

    // ---- what a test uses to behave like an engine ------------------------------------------------------

    public event EventHandler<PlayerStateChangedEventArgs>? StateChanged;
    public event EventHandler<PlayerFailedEventArgs>? Failed;
    public event EventHandler? EndOfMedia;
    public event EventHandler<PlaybackPositionEventArgs>? PositionChanged;

    /// <summary>Raises a state change on the calling thread, as an engine would on its own.</summary>
    public void RaiseState(PlayerState newState)
    {
      PlayerState old = State;
      State = newState;
      StateChanged?.Invoke(this, new PlayerStateChangedEventArgs(old, newState));
    }

    public void RaisePosition(long timeInMs, long lengthInMs)
    {
      TimeInMs = timeInMs;
      LengthInMs = lengthInMs;
      PositionChanged?.Invoke(this, new PlaybackPositionEventArgs(timeInMs, lengthInMs));
    }

    public void RaiseFailure(string reason)
    {
      LastFailure = reason;
      Failed?.Invoke(this, new PlayerFailedEventArgs(reason));
    }

    public void RaiseEndOfMedia()
    {
      RaiseState(PlayerState.Ended);
      EndOfMedia?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises a state change from a separate thread and waits for it to have been delivered, which is the only
    /// honest way to test what a screen does with an event it did not cause.
    /// </summary>
    public void RaiseStateFromAnotherThread(PlayerState newState)
    {
      Exception? failure = null;
      Thread thread = new Thread(new ThreadStart(delegate
      {
        try
        {
          RaiseState(newState);
        }
        catch (Exception ex)
        {
          failure = ex;
        }
      }));
      thread.IsBackground = true;
      thread.Start();
      if (!thread.Join(TimeSpan.FromSeconds(5)))
        throw new TimeoutException("the event thread never finished");
      if (failure != null)
        throw new InvalidOperationException("raising the event on another thread threw: " + failure.Message, failure);
    }

    public void Dispose()
    {
      Calls.Add("Dispose");
    }
  }
}
