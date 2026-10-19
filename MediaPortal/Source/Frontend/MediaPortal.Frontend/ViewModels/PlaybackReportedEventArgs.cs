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

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What the playback screen says a viewer watched: which library title, and how far they got in it.
  /// </summary>
  /// <remarks>
  /// Raised by <see cref="NowPlayingViewModel.PlaybackReported"/> when a media ends, is stopped, or is replaced by
  /// another - the three moments at which "how far did they get" has a final answer.
  /// <para>
  /// The percentage is what the screen saw, passed through the viewer's watched mark: a viewing that reached the
  /// mark travels as the hundred the library door understands. That is why "did the engine say this media ended"
  /// is its own question on <see cref="EngineEnded"/> - a stop at ninety-five per cent is watched and is not
  /// finished, and the party queue must be able to tell those two apart.
  /// </para>
  /// </remarks>
  public sealed class PlaybackReportedEventArgs : EventArgs
  {
    public PlaybackReportedEventArgs(Guid mediaItemId, int percentage, bool engineEnded = false)
    {
      MediaItemId = mediaItemId;
      Percentage = percentage;
      EngineEnded = engineEnded;
    }

    /// <summary>The library title this was about. Never <see cref="Guid.Empty"/>.</summary>
    public Guid MediaItemId { get; }

    /// <summary>1 to 100. The screen does not report a viewing nobody got anywhere with.</summary>
    public int Percentage { get; }

    /// <summary>
    /// Whether the player engine itself said this media ended. Everything else - a stop, a replacement, a
    /// watched mark raising the percentage - is a viewing that was interrupted, however complete its number
    /// looks.
    /// </summary>
    public bool EngineEnded { get; }
  }
}
