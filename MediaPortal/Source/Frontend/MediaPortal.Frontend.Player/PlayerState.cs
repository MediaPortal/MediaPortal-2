namespace MediaPortal.Frontend.Player
{
  /// <summary>
  /// The states a player can be in, in words that belong to MP2 rather than to libVLC.
  /// </summary>
  /// <remarks>
  /// The mapping from libVLC's own states is one-to-one with three exceptions, and each of them is a decision:
  /// <list type="bullet">
  ///   <item>
  ///     libVLC reports <c>NothingSpecial</c>, <c>Opening</c> and <c>Buffering</c> as three different things. To
  ///     a screen they are one: "asked for, no picture yet". They all arrive as <see cref="Opening"/>.
  ///   </item>
  ///   <item>
  ///     libVLC's <c>Ended</c> and <c>Stopped</c> are kept apart on purpose. <see cref="Ended"/> means the media
  ///     ran out, which is the signal that plays the next item; <see cref="Stopped"/> means somebody ended it
  ///     early, which is not. Collapsing them is how a playlist starts looping the same track.
  ///   </item>
  ///   <item>
  ///     libVLC has no state for "cannot find the engine at all". <see cref="Failed"/> covers both "libVLC is
  ///     not on this machine" and "this file would not open", with a <see cref="PlayerFailedEventArgs.Reason"/>
  ///     that says which.
  ///   </item>
  /// </list>
  /// </remarks>
  public enum PlayerState
  {
    /// <summary>Nothing has been opened on this player yet.</summary>
    Idle,

    /// <summary>A media was accepted; no picture and no sound yet.</summary>
    Opening,

    /// <summary>Playing forward.</summary>
    Playing,

    /// <summary>Paused, still holding the media and its position.</summary>
    Paused,

    /// <summary>Ended by the caller. Position is not meaningful.</summary>
    Stopped,

    /// <summary>Ended because the media ran out. Different from <see cref="Stopped"/> by design.</summary>
    Ended,

    /// <summary>
    /// Nothing will come of this player instance for this media: no engine, no file, or an error the engine
    /// reported. A reason is always available - see <see cref="IVideoPlayer.LastFailure"/>.
    /// </summary>
    Failed
  }
}
