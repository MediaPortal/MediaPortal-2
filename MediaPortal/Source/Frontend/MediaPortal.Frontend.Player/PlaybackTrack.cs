using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MediaPortal.Frontend.Player
{
  /// <summary>Which picker a track belongs on.</summary>
  public enum TrackKind
  {
    Audio,
    Subtitle
  }

  /// <summary>
  /// One selectable audio or subtitle track, in MP2's words.
  /// </summary>
  /// <remarks>
  /// <see cref="Id"/> is the number to hand back to <see cref="IVideoPlayer.SelectTrack"/>; libVLC's own
  /// numbering is kept behind that call. An id of -1 means "switch this kind off", which is the same convention
  /// the old player used, so a screen does not have to learn a second one.
  /// </remarks>
  public readonly struct PlaybackTrack : IEquatable<PlaybackTrack>
  {
    public PlaybackTrack(TrackKind kind, int id, string? description = null, string? language = null)
    {
      Kind = kind;
      Id = id;
      Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
      Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim();
    }

    public TrackKind Kind { get; }
    public int Id { get; }

    /// <summary>What the engine calls it, if it says anything at all.</summary>
    public string? Description { get; }

    /// <summary>
    /// Two-letter language code when the container carries one. Null when it does not - a screen shows
    /// <see cref="Description"/> then, and never a made-up "unknown".
    /// </summary>
    public string? Language { get; }

    /// <summary>
    /// What a screen should show as the label: the language if there is one, else the description, else the id.
    /// One place decides this so that two screens cannot disagree.
    /// </summary>
    public string Label
    {
      get
      {
        if (Language != null && Description != null)
          return $"{Language} - {Description}";
        return Language ?? Description ?? Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
      }
    }

    /// <summary>
    /// The language inside an engine's track name, or null.
    /// </summary>
    /// <remarks>
    /// Engines describe a track with one string. libVLC's shape for a language in it is "Language (eng)", which
    /// is the shape the old player also read out of DirectVobSub's track names - shared history, not a new
    /// invention. It is still only a pattern, so anything else stays a description with no language rather than
    /// becoming a guess. It lives on the track rather than in the engine so that the pattern is vocabulary of
    /// this seam, testable without a native library.
    /// </remarks>
    public static string? ParseLanguageFromEngineName(string? engineTrackName)
    {
      if (string.IsNullOrWhiteSpace(engineTrackName))
        return null;
      Match match = LanguagePattern.Match(engineTrackName.Trim());
      if (!match.Success)
        return null;
      string code = match.Groups["code"].Value.Trim();
      return code.Length == 0 || code.Length > 3 ? null : code;
    }

    private static readonly Regex LanguagePattern =
        new Regex(@"^Language\s+\((?<code>[^)]+)\)$",
                  RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public bool Equals(PlaybackTrack other)
    {
      return Kind == other.Kind && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
      return obj is PlaybackTrack other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine((int)Kind, Id);
    }

    public override string ToString()
    {
      return $"{Kind} #{Id} {Label}";
    }
  }

  /// <summary>Argument of <see cref="IVideoPlayer.StateChanged"/>.</summary>
  public sealed class PlayerStateChangedEventArgs : EventArgs
  {
    public PlayerStateChangedEventArgs(PlayerState oldState, PlayerState newState)
    {
      OldState = oldState;
      NewState = newState;
    }

    public PlayerState OldState { get; }
    public PlayerState NewState { get; }
  }

  /// <summary>
  /// Argument of <see cref="IVideoPlayer.Failed"/>.
  /// </summary>
  /// <remarks>
  /// <see cref="Reason"/> is a sentence for a human and for the log. It is never an exception's stack and never
  /// empty - a failure the user cannot read about is a failure nobody can report.
  /// </remarks>
  public sealed class PlayerFailedEventArgs : EventArgs
  {
    public PlayerFailedEventArgs(string reason)
    {
      if (string.IsNullOrWhiteSpace(reason))
        throw new ArgumentException("A failure needs a reason.", nameof(reason));
      Reason = reason;
    }

    public string Reason { get; }

    public override string ToString()
    {
      return Reason;
    }
  }

  /// <summary>
  /// How a player was built. Everything in here is optional; the defaults are what a TV box uses.
  /// </summary>
  public sealed class PlayerOptions
  {
    /// <summary>
    /// Where to look for the libVLC libraries first. Null means "ask the platform" - see
    /// <see cref="NativeLibVlc.TryFind"/>.
    /// </summary>
    public string? LibraryDirectory { get; set; }

    /// <summary>
    /// Extra arguments for libVLC, verbatim. The player adds its own (see <see cref="HeadlessArguments"/>) and
    /// does not pretend to understand these.
    /// </summary>
    public IList<string> ExtraArguments { get; } = new List<string>();

    /// <summary>
    /// Ask libVLC for no video window and no audio device. This is what makes a player usable on a host with
    /// neither - a container, a service, or a test - and it is the only way the decoding half of the engine can
    /// be measured without somebody sitting in front of a screen.
    /// </summary>
    /// <remarks>
    /// A headless player is <b>not</b> paced to wall clock. Measured with VLC 3.0.20 on this repository's own
    /// fixture: a 4000 ms file reached <see cref="PlayerState.Ended"/> after 3040 ms, and after 1957 ms with
    /// <c>--clock-synchro=1</c> (so that argument is not the fix one would guess). Anything that measures
    /// "did the clock move" must therefore compare positions with each other and never with a stopwatch - which
    /// is why the tests assert that a position advanced and that a seek near the end brings the end, rather than
    /// asserting how long a second took.
    /// </remarks>
    public bool Headless { get; set; }

    /// <summary>
    /// The arguments <see cref="Headless"/> turns into. Named so that a test can assert what "headless" actually
    /// asked for instead of trusting the flag's name.
    /// </summary>
    public static IReadOnlyList<string> HeadlessArguments { get; } = new[]
    {
      // Both names are libVLC's own. `dummy` exists on every platform precisely because it needs nothing.
      "--vout=dummy",
      "--aout=dummy",
      "--no-osd",
      "--no-video-title-show"
    };
  }
}
