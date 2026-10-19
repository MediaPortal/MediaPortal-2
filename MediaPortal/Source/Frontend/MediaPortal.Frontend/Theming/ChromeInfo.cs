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
using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// What the skin's master chrome - the header and the footer the reference screens wrapped around every
  /// screen - has to say about the window it is drawn on: the time the header shows and what is playing that
  /// the footer shows.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's master screens read these from the running application: the header's clock element read
  /// the system clock, and the footer's PlayerControl elements read what the player was doing and hid
  /// themselves (AutoVisibility) while nothing played. This class is the same information in one place: the
  /// window owns it, the skin's chrome.xaml templates bind to it, and nobody else has to know either exists.
  /// </para>
  /// <para>
  /// It is the data type the skin files promise with <c>x:DataType</c>, which is why its property names are
  /// its API: a skin that binds to a name not here shows up as a compile error in the skin file, not as a
  /// silently blank header.
  /// </para>
  /// </remarks>
  public partial class ChromeInfo : ObservableObject
  {
    private System.Timers.Timer? _clockTimer;
    private Action<Action>? _marshal;

    public ChromeInfo()
    {
      _clock = FormatClock(DateTime.Now);
    }

    public ChromeInfo(Action<Action>? marshal)
        : this()
    {
      _marshal = marshal;
    }

    /// <summary>
    /// Start the header's clock ticking. Split from the constructor because a test may want the words without
    /// the timer, and a window wants both.
    /// </summary>
    public void StartClock()
    {
      if (_clockTimer != null)
        return;

      // The reference header's clock read the minute, not the second - a television ten feet away shows
      // minutes, and a timer that fires every second on a media player is a wake-up nobody asked for.
      _clockTimer = new System.Timers.Timer(TimeSpan.FromSeconds(15).TotalMilliseconds) { AutoReset = true };
      _clockTimer.Elapsed += (sender, e) => ReportClock();
      _clockTimer.Start();
    }

    /// <summary>
    /// Say the time and the day again, this moment's words: the language row on the client settings screen
    /// calls this after it saves, so a date that was English a keystroke ago is German on the next line of
    /// the head line, without waiting for the timer or a restart.
    /// </summary>
    public void RefreshClock()
    {
      ReportClock();
    }

    /// <summary>
    /// Put the new time where the header reads it. The timer rings on its own thread, and a bound property
    /// may only be spoken to on the one the window lives on - so the words are posted there, which is how the
    /// player's events arrive too.
    /// </summary>
    private void ReportClock()
    {
      string now = FormatClock(DateTime.Now);
      string today = FormatDate(DateTime.Now);
      if (_marshal == null)
      {
        Clock = now;
        Date = today;
      }
      else
        _marshal(() =>
        {
          Clock = now;
          Date = today;
        });
    }

    /// <summary>The header's clock, spelled the way the reference header spelled it.</summary>
    [ObservableProperty]
    private string _clock;

    /// <summary>
    /// Whether the head line may show its clock at all - the reference's <c>ShowTime</c>, one of Nereus'
    /// own settings. True while nobody wears a skin that has an opinion, because a header that hides its
    /// clock without being asked is a broken clock to everyone else.
    /// </summary>
    [ObservableProperty]
    private bool _showTime = true;

    /// <summary>Whether the head line may show its date - the reference's <c>ShowDate</c>, next to its twin.</summary>
    [ObservableProperty]
    private bool _showDate = true;

    /// <summary>
    /// The day the header's clock stands on - the reference showed the date under the time, and its skin
    /// settings made it the clock's own companion (ShowDate next to ShowTime), not a stranger's.
    /// </summary>
    [ObservableProperty]
    private string _date = FormatDate(DateTime.Now);

    /// <summary>The name of the screen the window is on; the header shows it where the reference masters showed their title.</summary>
    [ObservableProperty]
    private string _screenTitle = "";

    /// <summary>Whether the footer has anything to show at all - the AutoVisibility of the reference.</summary>
    [ObservableProperty]
    private bool _hasMedia;

    /// <summary>The title of whatever is playing, as the footer's first column.</summary>
    [ObservableProperty]
    private string _nowPlayingTitle = "";

    /// <summary>How far into it we are, nought to a hundred, as the footer's progress line.</summary>
    [ObservableProperty]
    private double _nowPlayingProgress;

    /// <summary>Playing, paused, whatever the player says; the footer's quiet last column.</summary>
    [ObservableProperty]
    private string _nowPlayingState = "";

    /// <summary>
    /// Say what the player is doing - the footer appears with these words.
    /// </summary>
    public void ReportPlaying(string title, double progressPercent, string state)
    {
      NowPlayingTitle = title;
      NowPlayingProgress = progressPercent;
      NowPlayingState = state;
      HasMedia = true;
    }

    /// <summary>Nothing is playing; the footer takes itself off.</summary>
    public void ReportStopped()
    {
      HasMedia = false;
      NowPlayingTitle = "";
      NowPlayingProgress = 0;
      NowPlayingState = "";
    }

    private static string FormatClock(DateTime now)
    {
      return now.ToString("HH:mm");
    }

    /// <summary>
    /// The day as this machine's calendar says it: the reference's client settings carried a language of
    /// their own, and a head line that reads "Montag" on a German box and "Monday" on an English one is
    /// that setting doing its one visible job.
    /// </summary>
    private static string FormatDate(DateTime now)
    {
      return now.ToString("D", Settings.ClientSettings.UiCulture());
    }
  }
}
