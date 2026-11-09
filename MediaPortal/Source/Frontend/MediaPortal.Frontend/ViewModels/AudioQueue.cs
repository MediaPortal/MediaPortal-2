#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// How the end of a queue is answered: nothing after the last tune, the queue again, or the same tune for
  /// ever. Named <c>None</c> rather than <c>Off</c> because the word on the screen is the answer to "what
  /// happens when the last one is through", not a switch that is off.
  /// </summary>
  public enum AudioRepeat
  {
    None,
    All,
    One
  }

  /// <summary>
  /// One tune of a queue, as the panel to the right of the cover draws it: its name, its length once the
  /// library has said it, and whether the player is standing on it.
  /// </summary>
  public sealed class AudioQueueRow : INotifyPropertyChanged
  {
    private string _lengthWords = string.Empty;
    private bool _isCurrent;

    public AudioQueueRow(TitleViewModel title)
    {
      Title = title;
      Number = title.Title.Track;
      Name = TuneName(title.Name, Number);
    }

    /// <summary>The library row this tune is played from - its file, its id, its name.</summary>
    public TitleViewModel Title { get; }

    /// <summary>
    /// What the row prints: the tune's own name, without the record hung in front of it. See
    /// <see cref="TuneName"/> for why the library's name is not that.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The library spells an audio title's name with its record in front of it, because on a shelf of many
    /// records "3 - Anonymous" says nothing: the title a track carries is <c>TrackInfo.ToString()</c>
    /// (MediaManagement/Helpers/TrackInfo.cs:305), which writes either
    /// <c>"{record}: {number} - {name}"</c> (same file, line 59) or <c>"{number} - {name}"</c> (line 63).
    /// Inside the record's own list that prefix is doubled noise twice over - the panel is already the record,
    /// and the row's number is already known - so here it is cut again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only the library's own two spellings are cut, and only when the number cut is this row's number.</b>
    /// A record's name is allowed to contain colons and dashes of its own ("Rock - Best: 2 - Song"), so the
    /// cut is looked for at every " - " from the front, and the part before it has to read as either digits
    /// alone or "<anything>: <digits>" with those digits matching <see cref="Number"/> - which is the shape
    /// the library's own reader uses (TrackInfo.cs:65). A name that does not match keeps every character it
    /// has, and a tune the library never numbered is left alone entirely, because there is then nothing to
    /// check the cut against.
    /// </para>
    /// </remarks>
    internal static string TuneName(string? libraryName, int? number)
    {
      if (string.IsNullOrEmpty(libraryName) || !number.HasValue)
        return libraryName ?? string.Empty;

      string name = libraryName!;
      for (int dash = name.IndexOf(" - ", StringComparison.Ordinal);
          dash > 0;
          dash = name.IndexOf(" - ", dash + 1, StringComparison.Ordinal))
      {
        string prefix = name.Substring(0, dash);
        int afterNumber = prefix.LastIndexOf(':') + 1; // Zero when the record is not in front of it.
        string digits = prefix.Substring(afterNumber).Trim();
        if (digits.Length > 0
            && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int cut)
            && cut == number.Value)
          return name.Substring(dash + 3);
      }

      return name;
    }

    /// <summary>
    /// The number the library gave this tune on its record, or null when it never wrote one. Not drawn in
    /// front of the name - the template's list carries no numbers - but a reader reading the list aloud gets
    /// it from <see cref="Spoken"/>, and a list of tunes nobody numbered reads differently.
    /// </summary>
    public int? Number { get; }

    /// <summary>How long the tune is, "3:52", or empty while the library has not answered.</summary>
    public string LengthWords
    {
      get { return _lengthWords; }
      private set
      {
        if (_lengthWords == value)
          return;
        _lengthWords = value;
        Raise(nameof(LengthWords));
        Raise(nameof(Spoken));
      }
    }

    /// <summary>Whether the player stands on this tune.</summary>
    public bool IsCurrent
    {
      get { return _isCurrent; }
      internal set
      {
        if (_isCurrent == value)
          return;
        _isCurrent = value;
        Raise(nameof(IsCurrent));
        Raise(nameof(Spoken));
      }
    }

    /// <summary>The whole row in one sentence, for a screen that is read aloud.</summary>
    public string Spoken
    {
      get
      {
        string number = Number.HasValue
            ? "track " + Number.Value.ToString(CultureInfo.InvariantCulture) + ", "
            : string.Empty;
        string length = _lengthWords.Length > 0 ? ", " + _lengthWords : string.Empty;
        return number + Name + length + (IsCurrent ? ", playing now" : string.Empty);
      }
    }

    /// <summary>
    /// Put the library's answer on the row. Seconds rather than a formatted string, because the row is the
    /// one place that knows whether it has an answer at all: nought means the library never wrote a length,
    /// which stays an empty space rather than becoming "0:00".
    /// </summary>
    internal void SetLength(int? seconds)
    {
      if (seconds == null || seconds.Value <= 0)
        return;
      int whole = seconds.Value;
      LengthWords = (whole / 60).ToString(CultureInfo.InvariantCulture) + ":"
          + (whole % 60).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }

  /// <summary>
  /// The tunes a viewer pressed their way into: the record the pressed tune sits on, or the list the press
  /// happened in. The player is still the one thing that plays; this only answers "which tune next", keeps
  /// the current one lit, and says what the list to the right of the cover has to show.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this is its own little thing.</b> The application has had one queue before - the party's, in
  /// <see cref="PartyViewModel"/> - which reads a whole shelf, shuffles it and asks for the head of it. That
  /// one is the party's own: it owns its pool, refills it when the shelf ran out, and answers to its own
  /// screen. A record the viewer pressed into needs none of that and must not grow a second copy of it in
  /// the party's class, so the answering part - an order, a place in it, and what the end of it means -
  /// stands here, and both screens stay what they were.
  /// </para>
  /// <para>
  /// <b>Order and place are kept apart on purpose.</b> <c>_order</c> holds row numbers in the sequence the
  /// player will walk them, and <c>_at</c> is where in that sequence the player stands. Shuffling rewrites
  /// <c>_order</c> beneath a fixed <c>_at</c> ... which is exactly the behaviour a viewer expects: pressing
  /// shuffle while a tune plays does not stop it, it only changes what comes after it.
  /// </para>
  /// <para>
  /// Nothing here plays or stops anything. Whoever owns the player is asked through
  /// <see cref="PlayRequested"/> and reports back through <see cref="Finished"/> and
  /// <see cref="Refused"/>, the same bargain the application already keeps with the party's queue.
  /// </para>
  /// </remarks>
  public sealed class AudioQueue
  {
    /// <summary>
    /// How many rows the panel has room for at the tallest. The panel grows with the record - four tunes make a
    /// four row panel - and stops growing here, because a list taller than the picture behind it is a list the
    /// viewer has to read past a face. A shorter screen asks for fewer of them (see <see cref="Slots"/>).
    /// </summary>
    public const int SLOTS = 10;

    /// <summary>
    /// How many rows the panel is allowed to show at once, which the screen sets: the list stands with its foot
    /// a fixed distance above the floor, so how many rows it has room for is a question of how tall the room is.
    /// A record of twelve tunes on a short screen is read as a window that walks with the player, and the head of
    /// the panel keeps saying "12" so the viewer is not told the record is shorter than it is.
    /// </summary>
    public int Slots
    {
      get { return _slots; }
      set
      {
        int room = Math.Max(1, Math.Min(SLOTS, value));
        if (_slots == room)
          return;
        _slots = room;
        Say();
      }
    }

    private int _slots = SLOTS;

    /// <summary>
    /// Tunes that would not open in a row before the queue gives up. Three is the number the party's queue
    /// stopped on for a shelf it could not read at all, and a record whose first three files are broken is a
    /// record the viewer is better off hearing about than watching walk on.
    /// </summary>
    private const int REFUSALS_TO_STOP = 3;

    private readonly List<AudioQueueRow> _rows = new List<AudioQueueRow>();
    private readonly List<int> _order = new List<int>();
    private readonly Random _random;
    private int _at = -1;
    private int _refusals;
    private int _reading;
    private bool _shuffled;
    private AudioRepeat _repeat = AudioRepeat.None;

    /// <param name="shuffleSeed">
    /// A seed so that a test can watch a known order unfold. Null - what the application passes - means a
    /// different shuffle every time it is asked for, which is the entire point of the mark.
    /// </param>
    public AudioQueue(int? shuffleSeed = null)
    {
      _random = shuffleSeed.HasValue ? new Random(shuffleSeed.Value) : new Random();
    }

    /// <summary>
    /// Every tune of the record, in the order the library named them. A tune that would not open keeps its row
    /// in this list - the walk through the record is made of places in it, and a list that renumbered itself
    /// mid-record would lose the player's place with it - and is left out of the walk and off the panel instead.
    /// </summary>
    public IReadOnlyList<AudioQueueRow> Rows => _rows;

    /// <summary>
    /// The tunes in the order the player will walk them: the library's own order until the shuffle mark is
    /// pressed. The panel shows a window out of this (<see cref="VisibleRows"/>); the whole of it is what
    /// whoever asks "what comes after this" wants to be answered.
    /// </summary>
    public IReadOnlyList<AudioQueueRow> Walk
    {
      get
      {
        List<AudioQueueRow> walk = new List<AudioQueueRow>(_order.Count);
        foreach (int at in _order)
          walk.Add(_rows[at]);
        return walk;
      }
    }

    /// <summary>
    /// The rows the panel shows: the current tune, the ones after it, and as many of the ones before it as
    /// are left over. The ones after it win against the ones before it because "what is coming" is the
    /// question a list beside a player answers, and the tunes that already played are the ones a viewer can
    /// spare.
    /// </summary>
    public IReadOnlyList<AudioQueueRow> VisibleRows
    {
      get
      {
        if (_at < 0 || _order.Count == 0)
          return Array.Empty<AudioQueueRow>();

        int current = _order[_at];
        int slots = Math.Min(_slots, _order.Count);
        int after = _order.Count - 1 - _at;
        int shownAfter = Math.Min(after, slots - 1);
        int shownBefore = Math.Min(_at, slots - 1 - shownAfter);

        List<AudioQueueRow> shown = new List<AudioQueueRow>(shownBefore + 1 + shownAfter);
        for (int at = _at - shownBefore; at <= _at + shownAfter; at++)
          shown.Add(_rows[_order[at]]);
        return shown;
      }
    }

    /// <summary>Whether there is anything to show at all: a lone tune is not a list.</summary>
    public bool ShowsList
    {
      get { return _at >= 0 && _order.Count > 1; }
    }

    /// <summary>Where the player stands, in the viewer's words: "3 / 12".</summary>
    public string PositionWords
    {
      get
      {
        return _at < 0
            ? string.Empty
            : (_at + 1).ToString(CultureInfo.InvariantCulture) + " / "
                + _order.Count.ToString(CultureInfo.InvariantCulture);
      }
    }

    /// <summary>The tune the player was asked for last, or null while nobody was asked.</summary>
    public AudioQueueRow? Current
    {
      get { return _at >= 0 && _at < _order.Count ? _rows[_order[_at]] : null; }
    }

    public bool Shuffled
    {
      get { return _shuffled; }
      set
      {
        if (_shuffled == value || _order.Count < 2)
          return;
        _shuffled = value;
        Reorder();
        Say();
      }
    }

    public AudioRepeat Repeat
    {
      get { return _repeat; }
      set
      {
        if (_repeat == value)
          return;
        _repeat = value;
        Say();
      }
    }

    /// <summary>Whether the tune after the last one is the first one again.</summary>
    public bool GoesOn
    {
      get { return _repeat == AudioRepeat.All; }
    }

    /// <summary>
    /// Whether another tune follows the one the player is standing on: the same tune again under repeat-one,
    /// the next one while there is one, the first one again under repeat-all. This is the question a screen
    /// asks before it steps aside at the end of a tune - a record that is not through yet has nowhere to go.
    /// </summary>
    public bool PlaysOn
    {
      get
      {
        if (_at < 0)
          return false;
        return _repeat == AudioRepeat.One || _at + 1 < _order.Count || GoesOn;
      }
    }

    /// <summary>
    /// Raised for whoever owns the player to open this tune. The queue opens nothing itself: a screen that
    /// plays is the application's business, and a queue that played on its own would be two owners of one
    /// player.
    /// </summary>
    public event EventHandler<AudioQueueRow>? PlayRequested;

    /// <summary>Raised whenever the list, the lit row, or one of the two marks changed.</summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Take the tunes a viewer pressed their way into. <paramref name="startIndex"/> is the one they pressed;
    /// the ones before it stay in the list, because a viewer who pressed the fourth track of a record wants
    /// to see that there were three before it.
    /// </summary>
    /// <returns>Whether anything playable came of it.</returns>
    public bool Load(IEnumerable<TitleViewModel> titles, int startIndex)
    {
      Clear();

      foreach (TitleViewModel title in titles)
      {
        if (title.Playable)
          _rows.Add(new AudioQueueRow(title));
      }

      if (_rows.Count == 0)
        return false;

      for (int at = 0; at < _rows.Count; at++)
        _order.Add(at);

      // The pressed row may not be in the playable ones - a row the viewer pressed that this machine cannot
      // open is refused before this is ever asked, but a caller that hands a row it built elsewhere must not
      // be answered with a place outside the list.
      _at = startIndex >= 0 && startIndex < _order.Count ? startIndex : 0;
      _rows[_order[_at]].IsCurrent = true;
      _refusals = 0;
      Say();
      return true;
    }

    /// <summary>Let go of everything. The player is left alone, as always.</summary>
    public void Clear()
    {
      if (_rows.Count == 0 && _at < 0)
        return;

      foreach (AudioQueueRow row in _rows)
        row.IsCurrent = false;
      _rows.Clear();
      _order.Clear();
      _at = -1;
      _refusals = 0;
      _reading++;
      Say();
    }

    /// <summary>
    /// On to the next tune. False when there is none, which is the answer that lets the player fall silent
    /// rather than repeat itself.
    /// </summary>
    /// <param name="byHand">
    /// Whether a viewer pressed the mark. Pressing "next" on the last tune of a record that does not repeat
    /// is a viewer asking for something that is not there, and is answered the same way as the tune ending:
    /// nothing.
    /// </param>
    public bool Next(bool byHand = true)
    {
      if (_at < 0)
        return false;

      if (_at + 1 < _order.Count)
      {
        Move(_at + 1);
        return true;
      }

      if (!GoesOn)
        return false;

      Move(0);
      return true;
    }

    /// <summary>
    /// Back to the previous tune. Pressed at the very first tune this starts that same tune from its
    /// beginning - which is what every player's back mark has always done when there is nothing behind it.
    /// </summary>
    public bool Previous()
    {
      if (_at < 0)
        return false;

      if (_at > 0)
      {
        Move(_at - 1);
        return true;
      }

      if (GoesOn && _order.Count > 1)
      {
        Move(_order.Count - 1);
        return true;
      }

      Move(0); // The first tune again, from the top.
      return true;
    }

    /// <summary>A row of the panel was pressed: that tune now, the ones behind it keeping their places.</summary>
    public bool PlayAt(int visibleAt)
    {
      IReadOnlyList<AudioQueueRow> shown = VisibleRows;
      if (visibleAt < 0 || visibleAt >= shown.Count)
        return false;
      return PlayRow(shown[visibleAt]);
    }

    /// <summary>Ask for one particular tune of this queue, whoever found it.</summary>
    public bool PlayRow(AudioQueueRow row)
    {
      int at = _order.IndexOf(_rows.IndexOf(row));
      if (at < 0)
        return false;
      Move(at);
      return true;
    }

    /// <summary>
    /// The tune with this id reached its end. Repeat-one sends the same tune back; anything else walks on,
    /// and a tune that is not the one being waited on is history and answers nothing - advancing twice from
    /// one ending is the bug this check exists to keep out.
    /// </summary>
    public AudioQueueRow? Finished(Guid mediaItemId)
    {
      AudioQueueRow? done = Current;
      if (done == null || done.Title.Title.Id != mediaItemId)
        return null;

      _refusals = 0;

      if (_repeat == AudioRepeat.One)
      {
        Ask(done);
        return done;
      }

      return Next(false) ? Current : null;
    }

    /// <summary>
    /// The tune just asked for would not open. It leaves the queue - a file that is broken now is not
    /// unbroken later in this list - and the queue walks on until <see cref="REFUSALS_TO_STOP"/> in a row
    /// say the record itself is the problem.
    /// </summary>
    /// <returns>The next tune to ask for, or null when the queue stopped.</returns>
    public AudioQueueRow? Refused(Guid mediaItemId)
    {
      AudioQueueRow? dead = Current;
      if (dead == null || dead.Title.Title.Id != mediaItemId)
        return null;

      _refusals++;

      int at = _order.IndexOf(_rows.IndexOf(dead));
      if (at >= 0)
      {
        _order.RemoveAt(at);
        dead.IsCurrent = false;
        // The tune behind the broken one slides into its place, so the place the player stands on stays where
        // it is. Only a removal in front of the player pulls it along.
        if (at < _at)
          _at--;
      }

      if (_order.Count == 0)
      {
        _at = -1;
        Say();
        return null;
      }

      if (_at >= _order.Count)
        _at = GoesOn ? 0 : _order.Count - 1;

      _at = Math.Max(_at, 0);

      if (_refusals >= REFUSALS_TO_STOP)
      {
        // The record stays where it is, with the current row lit, so the panel still says what was meant to
        // play. Walking on after three broken files in a row would be a queue arguing with a dead share.
        _rows[_order[_at]].IsCurrent = true;
        Say();
        return null;
      }

      AudioQueueRow next = _rows[_order[_at]];
      Ask(next);
      return next;
    }

    /// <summary>
    /// Ask the library how long every tune of this record is, a tune at a time. The title list a row came out
    /// of carries no length - the server writes one only on a title's own answer - so a list of tunes that
    /// each say their own length is one small question per tune, asked while the first tune plays and
    /// dropped the moment another record is loaded.
    /// </summary>
    public async Task ReadLengthsAsync(Func<Guid, Task<Api.MediaItemDetails>>? readDetails)
    {
      if (readDetails == null || _rows.Count == 0)
        return;

      int generation = _reading;
      // Over the rows as they stand now, and not over the live list: a viewer who presses another record while
      // the questions are out empties this list, and a walk standing in the middle of it would be an enumerator
      // told the floor moved.
      foreach (AudioQueueRow row in _rows.ToArray())
      {
        if (row.LengthWords.Length > 0)
          continue;

        try
        {
          Api.MediaItemDetails details = await readDetails(row.Title.Title.Id);
          // The answer arriving late is not a reason to write it onto a record the viewer has left.
          if (generation == _reading)
            row.SetLength(details.DurationSeconds);
        }
        catch (Exception)
        {
          // A tune with no length is a tune whose length nobody knows, which the row already says by
          // standing empty. The rest of the record is still worth asking about.
        }
      }
    }

    /// <summary>
    /// Rewrite the walk through the tunes. Turning shuffle on leaves the tune that is playing exactly where it
    /// stands and puts everything after it - and everything that went before it, which is now a tune the
    /// viewer could walk back to - in a different order. Turning it off hands the record back its own order,
    /// with the player standing on the same tune in it.
    /// </summary>
    private void Reorder()
    {
      AudioQueueRow? playing = Current;
      int place = _at;

      if (!_shuffled)
      {
        // The library's order, untouched. Where the player stands in it is where that tune's own number is.
        _order.Clear();
        for (int at = 0; at < _rows.Count; at++)
          _order.Add(at);
        _at = playing == null ? 0 : _rows.IndexOf(playing);
        if (_at >= 0 && _at < _order.Count)
          _rows[_order[_at]].IsCurrent = true;
        return;
      }

      List<int> rest = new List<int>();
      for (int at = 0; at < _rows.Count; at++)
      {
        if (playing == null || !ReferenceEquals(_rows[at], playing))
          rest.Add(at);
      }
      Shuffle(rest);

      // The tune that is playing keeps its place in the sequence rather than being pushed to the front: the
      // place is how many tunes the viewer has already had, and pressing a mark must not rewrite that.
      _order.Clear();
      int next = 0;
      for (int at = 0; at < _rows.Count; at++)
      {
        if (playing != null && at == place)
          _order.Add(_rows.IndexOf(playing));
        else
          _order.Add(rest[next++]);
      }

      _at = place;
      if (_at >= _order.Count)
        _at = 0;
      if (_at >= 0 && _at < _order.Count)
        _rows[_order[_at]].IsCurrent = true;
    }

    private void Shuffle(List<int> at)
    {
      for (int last = at.Count - 1; last > 0; last--)
      {
        int swap = _random.Next(last + 1);
        (at[last], at[swap]) = (at[swap], at[last]);
      }
    }

    /// <summary>Light the row at <paramref name="at"/> and ask for its tune.</summary>
    private void Move(int at)
    {
      if (at < 0 || at >= _order.Count)
        return;

      if (_at >= 0 && _at < _order.Count)
        _rows[_order[_at]].IsCurrent = false;

      _at = at;
      AudioQueueRow row = _rows[_order[at]];
      row.IsCurrent = true;
      _refusals = 0;
      Ask(row);
    }

    private void Ask(AudioQueueRow row)
    {
      Say();
      PlayRequested?.Invoke(this, row);
    }

    private void Say()
    {
      Changed?.Invoke(this, EventArgs.Empty);
    }
  }
}
