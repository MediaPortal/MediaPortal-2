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

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// What the close screen has to say. The reference front ends answered "how do I leave?" with
  /// <c>dialogSystemState</c>, a small dialog whose rows were the machine's ways out; this front end asks
  /// the one question that has an answer here - leave or stay - and the answer changes while something is
  /// playing, because closing the application is not the same promise with a film half-watched.
  /// </summary>
  /// <remarks>
  /// The sentence is written here rather than in the view because it is assembled from what the player is
  /// doing at the moment the screen opens, and the player is the application's business. It names what is
  /// playing and says what Close really does: the player belongs to the application and not to any screen
  /// (see App, where the playback state outlives the window it was watched on), so closing this screen
  /// takes the picture away and leaves the sound running, and the dialog says exactly that instead of
  /// promising a stop it does not perform.
  /// </remarks>
  public sealed class CloseViewModel
  {
    public CloseViewModel(NowPlayingViewModel? playback)
    {
      if (playback != null && playback.HasMedia)
      {
        PlayingTitle = playback.Title;
        Message = PlayingTitle + " is still playing. Closing takes the picture away: the player belongs "
            + "to the application and not to this screen, so closing does not stop it and the sound carries on.";
      }
      else
      {
        Message = "Nothing is playing. Closing leaves the application and gives the television back.";
      }
    }

    /// <summary>The words under the question: what closing does in this moment, playing or not.</summary>
    public string Message { get; }

    /// <summary>The title that is still playing, or empty when nothing is.</summary>
    public string PlayingTitle { get; } = "";

    /// <summary>Whether the screen has to warn about a running playback at all.</summary>
    public bool IsPlaying
    {
      get { return PlayingTitle.Length > 0; }
    }
  }
}
