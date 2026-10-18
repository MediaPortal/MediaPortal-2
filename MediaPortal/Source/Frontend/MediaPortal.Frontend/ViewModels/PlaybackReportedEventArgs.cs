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
  /// The percentage is what the screen saw, not what the server will store: the library counts a title as watched
  /// when it was finished, and marks the date on anything shorter. A percentage here is therefore never 100 unless
  /// the media really ended, because "they stopped it at 99%" and "they saw the last second" are different
  /// statements and only the second one earns a tick.
  /// </para>
  /// </remarks>
  public sealed class PlaybackReportedEventArgs : EventArgs
  {
    public PlaybackReportedEventArgs(Guid mediaItemId, int percentage)
    {
      MediaItemId = mediaItemId;
      Percentage = percentage;
    }

    /// <summary>The library title this was about. Never <see cref="Guid.Empty"/>.</summary>
    public Guid MediaItemId { get; }

    /// <summary>1 to 100. The screen does not report a viewing nobody got anywhere with.</summary>
    public int Percentage { get; }
  }
}
