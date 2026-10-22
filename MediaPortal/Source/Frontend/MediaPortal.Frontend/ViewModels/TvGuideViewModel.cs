using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One programme as the guide grid draws it: where it stands and how wide it is.
  /// </summary>
  /// <remarks>
  /// The grid is not laid out in pixels but in columns, exactly as the reference's own <c>EpgGrid</c> is
  /// (<c>TV/SlimTvClient/Controls/EpgGrid.cs:61-99</c>: 75 columns holding the visible hours, a programme placed
  /// at <c>round(minutesFromViewportStart / perCellTime)</c> and spanning <c>round(duration / perCellTime)</c>).
  /// Columns rather than pixels, because the window this screen is shown in is not known here, and a block that
  /// knows which two columns it stands between looks right at a thousand pixels wide and at three thousand.
  /// </remarks>
  public sealed class TvGuideBlock
  {
    public TvGuideBlock(TvProgram program, int column, int columnSpan, bool startsBeforeWindow,
        bool endsAfterWindow)
    {
      Program = program;
      Column = column;
      ColumnSpan = columnSpan;
      StartsBeforeWindow = startsBeforeWindow;
      EndsAfterWindow = endsAfterWindow;
    }

    /// <summary>The programme itself, for the details along the bottom and for pressing on.</summary>
    public TvProgram Program { get; }

    /// <summary>The column its left edge stands in.</summary>
    public int Column { get; }

    /// <summary>How many columns it is drawn across, never less than one.</summary>
    public int ColumnSpan { get; }

    /// <summary>Whether it began before the window opens, which is said on its left edge.</summary>
    public bool StartsBeforeWindow { get; }

    /// <summary>Whether it runs past the end of the window, which is said on its right edge.</summary>
    public bool EndsAfterWindow { get; }

    /// <summary>What the block is filled with, which is its genre's colour.</summary>
    public string Fill
    {
      get { return TvGenreColors.ForGenre(Program.PrimaryGenre); }
    }

    /// <summary>The words inside the block: the title, and the series line under it when the backend said one.</summary>
    public string Title
    {
      get { return Program.Title; }
    }

    /// <summary>
    /// The series line, which the reference only drew when its <c>ShowSeriesInfo</c> switch was on
    /// (<c>Settings/SlimTvClientSettings.cs</c>) and which it built as <c>(S01.E02 Title)</c>
    /// (<c>ProgramListItem.cs:99-116</c>). Nothing is said when nothing was announced.
    /// </summary>
    public string SeriesLine
    {
      get
      {
        TvEpisode? episode = Program.Episode;
        return episode == null || !episode.IsKnown ? "" : episode.Describe();
      }
    }

    /// <summary>The hour it begins, which is the one thing a block always has room for.</summary>
    public string StartText
    {
      get { return Program.StartLocal; }
    }

    /// <summary>Whether this is what is running at the moment the guide was read.</summary>
    public bool IsRunning { get; set; }

    /// <summary>A little mark for a programme somebody asked the backend to save.</summary>
    public bool IsRecorded
    {
      get { return Program.IsRecorded; }
    }

    /// <summary>
    /// The words for the mark's tooltip. The reference drew two different pictures here, one for a single
    /// programme and one for a series (<c>record_single.png</c> and <c>record_series.png</c> in the styles);
    /// the backend's answer says which of the two it is only through the schedule's own shape, which the
    /// recording screens ask about, so one word stands for both until part bn has that answer.
    /// </summary>
    public string RecordedMark
    {
      get { return IsRecorded ? "recorded" : ""; }
    }
  }

  /// <summary>
  /// One channel's row of the guide: its name at the left and its programmes across.
  /// </summary>
  public sealed class TvGuideRow
  {
    public TvGuideRow(TvChannel channel)
    {
      Channel = channel;
      Blocks = new List<TvGuideBlock>();
    }

    /// <summary>The channel this row is about.</summary>
    public TvChannel Channel { get; }

    /// <summary>Its programmes in the window, left to right.</summary>
    public List<TvGuideBlock> Blocks { get; private set; }

    /// <summary>The number a viewer would say, which the reference drew at forty per cent opacity.</summary>
    public string Number
    {
      get { return Channel.Number > 0 ? Channel.Number.ToString(CultureInfo.InvariantCulture) : ""; }
    }

    /// <summary>The channel's name.</summary>
    public string Name
    {
      get { return Channel.Name; }
    }
  }

  /// <summary>
  /// The programme guide: every channel of one bouquet across a stretch of the evening.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The layout is the reference's full guide (<c>Skin/Nereus/screens/SlimTvClient-fullguide.xaml</c>): a fixed
  /// column of channel names at the left, a ruler of hours along the top, the programmes as blocks coloured by
  /// their genre, a line through the whole grid at the moment it is now, and two hundred and twenty pixels of
  /// detail along the bottom for whatever is selected. It scrolls by whole windows rather than sliding
  /// continuously, which is what the reference did when the viewer walked off the edge of the grid
  /// (<c>EpgGrid.cs:957-997</c> moved the model by thirty minutes and rebuilt).
  /// </para>
  /// <para>
  /// Three things were kept deliberately. The grid is asked for one bouquet at a time rather than for every
  /// channel at once, because a fortnight of programmes for two hundred channels is not a question to ask a
  /// television over the living-room Wi-Fi. The window is three hours wide, which is the reference's two hours
  /// widened to what a flat screen has room for - its own <c>EpgVisibleHours</c> could only be set between one
  /// and four (<c>EpgNumberOfHoursSetting.cs:34-36</c>). And a row whose programmes did not arrive is left in
  /// the grid empty rather than dropped: a channel that exists belongs on the list even when its broadcaster
  /// said nothing, and a guide that quietly removes channels is a guide a viewer cannot trust.
  /// </para>
  /// </remarks>
  public sealed class TvGuideViewModel : IDisposable, System.ComponentModel.INotifyPropertyChanged
  {
    /// <summary>
    /// How many columns the grid is drawn with. The reference used seventy-five for two and a half hours; ninety
    /// for three keeps each column at exactly two minutes, which is small enough that a quarter-hour programme
    /// still begins where it should look as though it begins.
    /// </summary>
    public const int COLUMNS = 90;

    /// <summary>The minutes one column stands for.</summary>
    public const int MINUTES_PER_COLUMN = 2;

    /// <summary>How wide the window is, in minutes: three hours.</summary>
    public const int WINDOW_MINUTES = COLUMNS * MINUTES_PER_COLUMN;

    private readonly Func<TvBackendSettings> _settingsProvider;
    private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
    private readonly TvMediaType _kind;
    private CancellationTokenSource? _inFlight;
    private int _generation;
    private bool _disposed;

    private TvChannelGroup? _selectedGroup;
    private TvGuideRow? _selectedRow;
    private TvGuideBlock? _selectedBlock;
    private TvGuideBlock? _actionsAbout;
    private bool _zapFromGuide = true;
    private DateTimeOffset _start = Floor(DateTimeOffset.UtcNow);
    private string _status = "";
    private bool _isLoading;
    private Task _inFlightLoad = Task.CompletedTask;

    /// <summary>
    /// Builds the guide. The settings and the client are handed over as functions because a guide left open
    /// while somebody changes the backend's address must not keep asking the machine it stopped talking to.
    /// </summary>
    public TvGuideViewModel(TvMediaType kind, Func<TvBackendSettings>? settingsProvider = null,
        Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null)
    {
      _kind = kind;
      _settingsProvider = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return new TvHeadendApiClient(settings);
      };
    }

    /// <summary>Rows of the grid, one per channel of the bouquet that is picked.</summary>
    public ObservableCollection<TvGuideRow> Rows { get; } = new ObservableCollection<TvGuideRow>();

    /// <summary>The bouquets the backend offered, the one it offered first picked.</summary>
    public ObservableCollection<TvChannelGroup> Groups { get; } = new ObservableCollection<TvChannelGroup>();

    /// <summary>The hours along the top of the grid, each standing in its own column.</summary>
    public ObservableCollection<TvGuideHour> Hours { get; } = new ObservableCollection<TvGuideHour>();

    /// <summary>Whether the guide is on its television half or its radio half.</summary>
    public TvMediaType Kind
    {
      get { return _kind; }
    }

    /// <summary>What the screen is called.</summary>
    public string Heading
    {
      get { return _kind == TvMediaType.Radio ? "Radio guide" : "TV guide"; }
    }

    /// <summary>The moment the window begins, floored to the two-minute column it belongs to.</summary>
    public DateTimeOffset Start
    {
      get { return _start; }
      private set
      {
        _start = Floor(value);
        Raise(nameof(Start));
        Raise(nameof(NowColumn));
        Raise(nameof(NowIsInside));
      }
    }

    /// <summary>The moment the window ends.</summary>
    public DateTimeOffset End
    {
      get { return _start.AddMinutes(WINDOW_MINUTES); }
    }

    /// <summary>The words above the grid: the stretch this window covers, on the viewer's own clock.</summary>
    public string WindowText
    {
      get
      {
        return _start.LocalDateTime.ToString("t", CultureInfo.CurrentCulture) + " - "
            + _start.AddMinutes(WINDOW_MINUTES).LocalDateTime.ToString("t", CultureInfo.CurrentCulture)
            + ", " + _start.LocalDateTime.ToString("ddd d MMMM", CultureInfo.CurrentCulture);
      }
    }

    /// <summary>The column the line at the moment-is-now stands in, or -1 when now is outside the window.</summary>
    public int NowColumn
    {
      get
      {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now < _start || now >= End)
          return -1;
        return (int) Math.Min(COLUMNS - 1,
            (long) Math.Round((now - _start).TotalMinutes / MINUTES_PER_COLUMN, MidpointRounding.AwayFromZero));
      }
    }

    /// <summary>Whether to draw that line at all.</summary>
    public bool NowIsInside
    {
      get { return NowColumn >= 0; }
    }

    /// <summary>The bouquet whose channels are on the grid.</summary>
    public TvChannelGroup? SelectedGroup
    {
      get { return _selectedGroup; }
      set
      {
        if (ReferenceEquals(_selectedGroup, value))
          return;
        _selectedGroup = value;
        Raise(nameof(SelectedGroup));
        Ask();
      }
    }

    /// <summary>
    /// The question the guide has out at the moment, or a finished one. The presses that change the window or the
    /// bouquet answer to a button, so they cannot hand their question back to whoever pressed it; this is where
    /// the question goes instead, so that the screen waiting to redraw and the tests waiting to measure have
    /// something to wait for.
    /// </summary>
    public Task InFlight
    {
      get { return _inFlightLoad; }
      private set { _inFlightLoad = value; }
    }

    /// <summary>The row the arrows are standing on.</summary>
    public TvGuideRow? SelectedRow
    {
      get { return _selectedRow; }
      set
      {
        _selectedRow = value;
        Raise(nameof(SelectedRow));
        Raise(nameof(SelectedChannelSays));
      }
    }

    /// <summary>The programme the details along the bottom are about.</summary>
    public TvGuideBlock? SelectedBlock
    {
      get { return _selectedBlock; }
      set
      {
        _selectedBlock = value;
        Raise(nameof(SelectedBlock));
        Raise(nameof(DetailsTitle));
        Raise(nameof(DetailsTimes));
        Raise(nameof(DetailsGenre));
        Raise(nameof(DetailsChannel));
        Raise(nameof(DetailsWords));
        Raise(nameof(DetailsSeries));
        Raise(nameof(HasSelection));
      }
    }

    /// <summary>Whether anything is selected, which is when the details have a place to come from.</summary>
    public bool HasSelection
    {
      get { return _selectedBlock != null; }
    }

    /// <summary>What the channel column says about the row the arrows stand on.</summary>
    public string SelectedChannelSays
    {
      get { return _selectedRow == null ? "" : _selectedRow.Channel.Describe(); }
    }

    /// <summary>The title, in the big line of the details.</summary>
    public string DetailsTitle
    {
      get { return _selectedBlock?.Title ?? ""; }
    }

    /// <summary>When it runs, as the reference's details led with them: start, a dash, end.</summary>
    public string DetailsTimes
    {
      get
      {
        TvProgram? program = _selectedBlock?.Program;
        if (program == null)
          return "";
        return program.StartLocal + "  -  " + program.StopLocal + "  ("
            + DescribeDuration(program.Duration) + ")";
      }
    }

    /// <summary>What kind of programme it is, which is also the colour its block wore.</summary>
    public string DetailsGenre
    {
      get
      {
        TvProgram? program = _selectedBlock?.Program;
        if (program == null)
          return "";
        string genre = program.PrimaryGenre;
        return genre.Length == 0 ? "no genre was announced" : genre;
      }
    }

    /// <summary>Which channel it is on.</summary>
    public string DetailsChannel
    {
      get
      {
        TvProgram? program = _selectedBlock?.Program;
        if (program == null)
          return "";
        return string.IsNullOrWhiteSpace(program.ChannelName)
            ? _selectedRow?.Name ?? ""
            : program.ChannelName;
      }
    }

    /// <summary>The series line, when the broadcaster told one.</summary>
    public string DetailsSeries
    {
      get { return _selectedBlock?.SeriesLine ?? ""; }
    }

    /// <summary>
    /// The words about it. The long description when the backend gave one, the shorter summary under it when
    /// it gave only that, and nothing invented when it gave neither.
    /// </summary>
    public string DetailsWords
    {
      get
      {
        TvProgram? program = _selectedBlock?.Program;
        if (program == null)
          return "Nothing is selected. The arrows walk the grid, and a programme's words appear here.";
        if (!string.IsNullOrWhiteSpace(program.Description))
          return program.Description!.Trim();
        if (!string.IsNullOrWhiteSpace(program.Summary))
          return program.Summary!.Trim();
        return "The broadcaster said nothing about this programme.";
      }
    }

    /// <summary>What the last question brought back, or what stopped it.</summary>
    public string Status
    {
      get { return _status; }
      private set
      {
        _status = value;
        Raise(nameof(Status));
      }
    }

    /// <summary>Whether a question is out.</summary>
    public bool IsLoading
    {
      get { return _isLoading; }
      private set
      {
        _isLoading = value;
        Raise(nameof(IsLoading));
      }
    }

    /// <summary>
    /// Reads the bouquets and then the programmes of the first one. Called when the screen opens, and again by
    /// the reload press - a backend that was switched on half a minute ago should answer now.
    /// </summary>
    public async Task ReloadAsync()
    {
      TvBackendSettings settings = _settingsProvider();
      if (!settings.IsConfigured)
      {
        Rows.Clear();
        Hours.Clear();
        Groups.Clear();
        SelectedGroup = null;
        Status = "No TV backend has been written down yet. Say where the television comes from on the "
            + "television settings, then read this again.";
        return;
      }

      InFlight = LoadProgramsAsync(settings);
      await InFlight.ConfigureAwait(false);
    }

    /// <summary>Moves the window half a screen back and reads it again.</summary>
    public void ScrollBackward()
    {
      Move(-WINDOW_MINUTES / 2);
    }

    /// <summary>Moves the window half a screen on.</summary>
    public void ScrollForward()
    {
      Move(WINDOW_MINUTES / 2);
    }

    /// <summary>Jumps a day back, which is how a viewer finds last Saturday's film.</summary>
    public void ScrollBackwardDay()
    {
      Move(-12 * 60);
    }

    /// <summary>Jumps a day on.</summary>
    public void ScrollForwardDay()
    {
      Move(12 * 60);
    }

    /// <summary>Brings the window back to now, which is where the line is.</summary>
    public void ScrollToNow()
    {
      Start = DateTimeOffset.UtcNow.AddMinutes(-30);
      Ask();
    }

    /// <summary>The bouquet the green and yellow buttons of a remote step through, backwards.</summary>
    public void PreviousGroup()
    {
      StepGroup(-1);
    }

    /// <summary>The bouquet the green and yellow buttons step through, forwards.</summary>
    public void NextGroup()
    {
      StepGroup(1);
    }

    /// <summary>
    /// Asked of whoever owns the watching screen: this channel, now. The guide knows nothing about players - it
    /// hands the channel over, and the application decides what playing it means - which is the same separation
    /// the reference kept between its guide and its player, talking between them through the workflow instead of
    /// holding on to one another.
    /// </summary>
    public event Action<TvChannel>? WatchRequested;

    /// <summary>
    /// The press on a programme. The first press selects it and puts its words along the bottom; pressing again
    /// on a programme that is running at the moment sends the viewer to it. That second part is the reference's
    /// <c>ZapFromGuide</c> switch, which was on unless somebody turned it off
    /// (<c>Settings/SlimTvClientSettings.cs</c>): standing in a guide and pressing the thing that is on now is how
    /// a person gets to the programme on the sofa without hunting for a channel number first.
    /// </summary>
    public void Press(TvGuideBlock block)
    {
      if (block == null)
        return;

      bool alreadyStandingHere = ReferenceEquals(block, _selectedBlock);
      SelectedBlock = block;

      if (!alreadyStandingHere)
        return;

      if (block.IsRunning && ZapFromGuide)
      {
        Watch(block);
        return;
      }

      // The second press on anything else - and on a running programme too, once a viewer said they would rather
      // be asked - is the reference's OK on a programme, which put its "Choose program action..." up rather than
      // guessing at what was wanted.
      _ = OpenProgramActionsAsync(block);
    }

    /// <summary>
    /// Sends the viewer to the channel a programme is on. Says so when the channel has no address rather than
    /// doing nothing twice, which is the guide's own version of not leaving a viewer guessing.
    /// </summary>
    /// <returns>Whether a channel was handed over.</returns>
    public bool Watch(TvGuideBlock? block)
    {
      if (block == null)
        return false;

      SelectedBlock = block;

      TvGuideRow? row = RowOf(block);
      if (row == null)
      {
        Status = "That programme is no longer on the grid.";
        return false;
      }

      SelectedRow = row;
      TvChannel channel = row.Channel;
      if (channel.StreamUrl == null)
      {
        Status = channel.Name + " has no stream address on " + _settingsProvider().Describe()
            + ", so there is nothing to watch.";
        return false;
      }

      WatchRequested?.Invoke(channel);
      return true;
    }

    /// <summary>
    /// The row a block stands in. The blocks are built per row and the rows are kept, so this is only walking
    /// back over what is already on the screen; a block that has been rebuilt away has no row and says so.
    /// </summary>
    private TvGuideRow? RowOf(TvGuideBlock block)
    {
      foreach (TvGuideRow row in Rows)
      {
        foreach (TvGuideBlock candidate in row.Blocks)
        {
          if (ReferenceEquals(candidate, block))
            return row;
        }
      }
      return null;
    }

    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      Cancel();
    }

    /// <summary>Asks after the window or the bouquet, and remembers the question for whoever waits.</summary>
    private void Ask()
    {
      InFlight = LoadProgramsAsync();
    }

    private void Move(int minutes)
    {
      Start = Start.AddMinutes(minutes);
      Ask();
    }

    private void StepGroup(int step)
    {
      if (Groups.Count == 0)
        return;

      int at = _selectedGroup == null ? 0 : Groups.IndexOf(_selectedGroup);
      if (at < 0)
        at = 0;
      int next = (at + step) % Groups.Count;
      if (next < 0)
        next += Groups.Count;
      SelectedGroup = Groups[next];
    }

    /// <summary>
    /// Reads the programmes of the picked bouquet. The answer is thrown away when a newer question has been
    /// asked in the meantime, because the arrows can be held down and a guide that painted the answers in the
    /// order they happened to arrive in would end up showing a window the viewer left three presses ago.
    /// </summary>
    private async Task LoadProgramsAsync(TvBackendSettings? settings = null)
    {
      if (_disposed)
        return;

      Cancel();
      CancellationTokenSource token = new CancellationTokenSource();
      _inFlight = token;
      int mine = ++_generation;

      TvBackendSettings askWith = settings ?? _settingsProvider();
      TvChannelGroup? group = SelectedGroup;

      if (!askWith.IsConfigured)
      {
        Status = "No TV backend has been written down yet.";
        return;
      }

      if (group == null)
      {
        // Nobody has picked a bouquet yet, which is how a guide opens. The bouquet is picked and its programmes
        // read in this one question rather than by firing a second one from the pick, because a screen that has
        // been told to reload has to be showing the grid by the time the answer comes back - otherwise the first
        // thing a viewer sees is a header over an empty grid.
        if (!await LoadGroupsAsync(askWith, mine, token.Token).ConfigureAwait(false))
          return;
        if (mine != _generation)
          return;
        group = SelectedGroup;
        if (group == null)
          return;
      }

      IsLoading = true;
      Status = "Reading " + group.Name + "...";
      BuildHours();

      try
      {
        using (TvHeadendApiClient client = _buildClient(askWith))
        {
          IReadOnlyList<TvChannel> channels = await client.GetChannelsAsync(token.Token).ConfigureAwait(false);

          // A backend with channels but no programme data is a television whose electronic programme guide was
          // never switched on. Those channels are still the channels the viewer asked to see, so they go on the
          // grid and the screen says what is missing, rather than drawing an evening nobody has scheduled.
          IReadOnlyList<TvProgram> programs;
          string? programsMissing = null;
          try
          {
            programs = await client.GetProgramsAsync(group, Start, End, token.Token).ConfigureAwait(false);
          }
          catch (TvBackendException ex)
          {
            programs = new List<TvProgram>();
            programsMissing = ex.Message;
          }

          if (mine != _generation)
            return;

          string? rowsSaid = BuildRows(channels, programs);
          Raise(nameof(Rows));
          Status = programsMissing ?? rowsSaid
              ?? string.Format("{0} channels, {1} programmes between {2}", Rows.Count, programs.Count, WindowText);
        }
      }
      catch (OperationCanceledException)
      {
        // A press arrived while this was out. The newer question says what it says.
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation)
          Status = ex.Message;
      }
      catch (Exception ex)
      {
        if (mine == _generation)
          Status = "The programme guide went wrong: " + ex.Message;
      }
      finally
      {
        if (mine == _generation)
          IsLoading = false;
        if (ReferenceEquals(_inFlight, token))
          token.Dispose();
      }
    }

    /// <summary>
    /// Reads the bouquets and picks the first one of this guide's half of the set. Whether one was picked is the
    /// answer, because the question that asked for this one goes on to read that bouquet's programmes itself -
    /// the picking does not start a second question of its own, which would leave whoever asked for a reload
    /// looking at a header over an empty grid until the second question happened to arrive.
    /// </summary>
    private async Task<bool> LoadGroupsAsync(TvBackendSettings settings, int mine, CancellationToken cancellation)
    {
      IsLoading = true;
      Status = "Asking " + settings.Describe() + " which channel groups it has...";
      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          IReadOnlyList<TvChannelGroup> groups = await client.GetChannelGroupsAsync(cancellation)
              .ConfigureAwait(false);

          if (mine != _generation)
            return false;

          List<TvChannelGroup> ofThisKind = groups.Where(g => !LooksWrongKind(g.Name)).ToList();
          Groups.Clear();
          foreach (TvChannelGroup group in ofThisKind)
            Groups.Add(group);

          if (Groups.Count == 0)
          {
            Rows.Clear();
            Hours.Clear();
            Status = string.Format("{0} answered but offered no channel groups, so there is no guide to read. "
                + "The groups are made in the backend's own configuration.", settings.Describe());
            return false;
          }

          // The bouquet a guide opens on is the one whose own name says what the guide is for, when one of them
          // does. Whoever opens the radio guide wants the radio stations, not an empty grid made of whichever
          // television bouquet the backend happens to list first. The picker is assigned here rather than gone
          // through, because going through it would start the next question on its own and this one has to
          // answer with the finished grid.
          _selectedGroup = Groups[PreferredGroupIndex()];
          Raise(nameof(SelectedGroup));
          return true;
        }
      }
      catch (OperationCanceledException)
      {
        return false;
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation)
          Status = ex.Message;
        return false;
      }
      finally
      {
        if (mine == _generation)
          IsLoading = false;
      }
    }

    /// <summary>
    /// Which bouquet to open on. A television guide takes the backend's first bouquet, which on every backend
    /// this was tried against is the full one. The radio guide looks for the bouquet whose name says radio,
    /// because a radio guide that opens on the first bouquet opens on a grid of television channels and shows
    /// somebody their radio as an empty evening. Names are the only thing a backend says about a bouquet besides
    /// what is in it, and guessing wrong costs a viewer one press of the bouquet button.
    /// </summary>
    private int PreferredGroupIndex()
    {
      if (_kind != TvMediaType.Radio)
        return 0;

      string[] hints = { "radio", "audio", "hören", "hoeren" };
      for (int i = 0; i < Groups.Count; i++)
      {
        string said = (Groups[i].Name ?? "").ToLowerInvariant();
        if (hints.Any(h => said.Contains(h, StringComparison.Ordinal)))
          return i;
      }
      return 0;
    }

    /// <summary>
    /// Whether a bouquet's name says it belongs to the other half of the set. The backend keeps one list of
    /// bouquets for television and radio together, and a guide with the radio stations in it is the wrong guide;
    /// a bouquet whose name says neither is left in, because guessing that a bouquet called "Family" is radio
    /// would take channels away from somebody.
    /// </summary>
    private bool LooksWrongKind(string name)
    {
      string said = (name ?? "").ToLowerInvariant();
      if (_kind == TvMediaType.Radio)
        return said.Contains("tv", StringComparison.Ordinal) || said.Contains("television", StringComparison.Ordinal)
            || said.Contains("fernsehen", StringComparison.Ordinal);
      return said.Contains("radio", StringComparison.Ordinal) || said.Contains("audio", StringComparison.Ordinal);
    }

    /// <summary>
    /// Lays the channels out as rows and their programmes as blocks. What the screen should say about the rows -
    /// when there is something to say - is the answer, because whoever asked for the rows has the last word on
    /// the status line.
    /// </summary>
    private string? BuildRows(IReadOnlyList<TvChannel> channels, IReadOnlyList<TvProgram> programs)
    {
      string groupSaid = SelectedGroup == null ? "This guide" : "'" + SelectedGroup.Name + "'";
      string? rowsSaid = null;

      List<TvChannel> ofThisBouquet = channels
          .Where(c => SelectedGroup == null || c.GroupIds.Contains(SelectedGroup.Id))
          .ToList();

      // Some backends list their channels without ever saying which bouquet a channel belongs to, and an empty
      // grid would then be a story about a bouquet that plainly has channels. Every channel of this guide's own
      // half of the set goes up instead, and the screen says that it had to guess.
      if (ofThisBouquet.Count == 0 && !channels.Any(c => c.GroupIds.Count > 0))
      {
        ofThisBouquet = channels.Where(c => c.MediaType == _kind).ToList();
        rowsSaid = "The backend never said which channels belong to which bouquet, so the grid holds every "
            + _kind.ToString().ToLowerInvariant() + " channel it has.";
      }
      else if (ofThisBouquet.Count == 0)
      {
        rowsSaid = groupSaid + " holds no channels, so there is nothing to put on the grid.";
      }

      DateTimeOffset now = DateTimeOffset.UtcNow;
      ILookup<string, TvProgram> byChannel = programs.ToLookup(p => (p.ChannelId ?? "").Trim(),
          StringComparer.OrdinalIgnoreCase);

      TvGuideBlock? previous = SelectedBlock;
      Rows.Clear();
      foreach (TvChannel channel in ofThisBouquet)
      {
        TvGuideRow row = new TvGuideRow(channel);
        foreach (TvProgram program in byChannel[channel.Id].OrderBy(p => p.Start))
        {
          TvGuideBlock? block = Place(program);
          if (block != null)
          {
            block.IsRunning = program.IsOnAt(now);
            row.Blocks.Add(block);
          }
        }
        Rows.Add(row);
      }

      SelectedRow = Rows.Count == 0 ? null : Rows[0];
      SelectedBlock = FindAgain(previous) ?? FirstBlockStartingNow();
      return rowsSaid;
    }

    /// <summary>
    /// Where a programme stands in the grid, or null when it is entirely outside the window. A programme that
    /// began before the window and ends inside it keeps its block from the left edge with its beginning said to
    /// be missing, exactly as the reference drew it - the bar has to end where the programme ends, or every bar
    /// to its right sits in the wrong place.
    /// </summary>
    private TvGuideBlock? Place(TvProgram program)
    {
      if (program.Stop <= Start || program.Start >= End)
        return null;

      bool before = program.Start < Start;
      bool after = program.Stop > End;

      DateTimeOffset shown = before ? Start : program.Start;
      DateTimeOffset until = after ? End : program.Stop;

      int column = (int) Math.Round((shown - Start).TotalMinutes / MINUTES_PER_COLUMN,
          MidpointRounding.AwayFromZero);
      int span = (int) Math.Round((until - shown).TotalMinutes / MINUTES_PER_COLUMN, MidpointRounding.AwayFromZero);

      column = Math.Max(0, Math.Min(COLUMNS - 1, column));
      span = Math.Max(1, Math.Min(COLUMNS - column, span));

      return new TvGuideBlock(program, column, span, before, after);
    }

    /// <summary>Finds the same programme again after a scroll, so the details do not jump away.</summary>
    private TvGuideBlock? FindAgain(TvGuideBlock? previous)
    {
      if (previous == null)
        return null;

      foreach (TvGuideRow row in Rows)
      {
        foreach (TvGuideBlock block in row.Blocks)
        {
          if (block.Program.Id == previous.Program.Id
              && string.Equals(block.Program.ChannelId, previous.Program.ChannelId, StringComparison.Ordinal))
            return block;
          if (block.Program.Start == previous.Program.Start
              && string.Equals(block.Program.Title, previous.Program.Title, StringComparison.Ordinal))
            return block;
          if (block.Program.IsOnAt(previous.Program.Start) && previous.Program.IsOnAt(block.Program.Start))
            return block;
        }
      }
      return null;
    }

    /// <summary>The programme running on the first channel, which is where a guide that opens should stand.</summary>
    private TvGuideBlock? FirstBlockStartingNow()
    {
      DateTimeOffset now = DateTimeOffset.UtcNow;
      foreach (TvGuideRow row in Rows)
      {
        foreach (TvGuideBlock block in row.Blocks)
        {
          if (block.Program.IsOnAt(now))
            return block;
        }
      }
      return Rows.Count > 0 && Rows[0].Blocks.Count > 0 ? Rows[0].Blocks[0] : null;
    }

    private void BuildHours()
    {
      Hours.Clear();

      // The ruler starts on the next full hour unless the window opens on one, because a strip that begins at
      // "20:04" is a strip nobody reads the hour off.
      DateTimeOffset first = _start;
      if (first.Minute != 0 || first.Second != 0)
        first = first.AddMinutes(60 - first.Minute).AddSeconds(-first.Second);

      for (DateTimeOffset hour = first; hour < End; hour = hour.AddHours(1))
      {
        int column = (int) Math.Round((hour - _start).TotalMinutes / MINUTES_PER_COLUMN,
            MidpointRounding.AwayFromZero);
        if (column < 0 || column >= COLUMNS)
          continue;
        Hours.Add(new TvGuideHour(hour, column,
            hour.LocalDateTime.ToString("t", CultureInfo.CurrentCulture)));
      }
    }

    private static DateTimeOffset Floor(DateTimeOffset moment)
    {
      long minutes = (long) Math.Floor((moment - DateTimeOffset.FromUnixTimeSeconds(0)).TotalMinutes);
      long floored = minutes - (((minutes % MINUTES_PER_COLUMN) + MINUTES_PER_COLUMN) % MINUTES_PER_COLUMN);
      return DateTimeOffset.FromUnixTimeSeconds(0).AddMinutes(floored);
    }

    private static string DescribeDuration(TimeSpan duration)
    {
      int minutes = (int) Math.Round(duration.TotalMinutes);
      if (minutes < 60)
        return minutes.ToString(CultureInfo.InvariantCulture) + " min";
      int hours = minutes / 60;
      int rest = minutes % 60;
      return rest == 0
          ? hours.ToString(CultureInfo.InvariantCulture) + " h"
          : hours.ToString(CultureInfo.InvariantCulture) + " h " + rest.ToString(CultureInfo.InvariantCulture)
            + " min";
    }

    #region What a viewer can ask about a programme

    /// <summary>The answers a viewer can have for the programme the dialog is about.</summary>
    public ObservableCollection<TvProgramAction> ProgramActions { get; } =
        new ObservableCollection<TvProgramAction>();

    /// <summary>
    /// Whether standing on a running programme and pressing sends the viewer straight to it. The reference had this
    /// on unless somebody turned it off (<c>Settings/SlimTvClientSettings.cs</c>, <c>ZapFromGuide</c>), and it stays
    /// on here for the same reason: on the sofa, pressing the thing that is on now is how a person gets to it. With
    /// the switch off, every second press asks the question the dialog answers.
    /// </summary>
    public bool ZapFromGuide
    {
      get { return _zapFromGuide; }
      set
      {
        if (_zapFromGuide == value)
          return;

        _zapFromGuide = value;
        Raise(nameof(ZapFromGuide));
      }
    }

    /// <summary>The programme the dialog is about, or nothing while no dialog is open.</summary>
    public TvGuideBlock? ActionsAbout
    {
      get { return _actionsAbout; }
    }

    /// <summary>The dialog's title, which is the reference's <c>[SlimTvClient.ChooseProgramAction]</c>.</summary>
    public string ActionsTitle
    {
      get { return "Choose program action..."; }
    }

    /// <summary>
    /// The line under the title, saying which programme the question is about - a dialog that only said "Choose
    /// program action" over a grid of ninety blocks would leave a viewer looking for the block they pressed.
    /// </summary>
    public string ActionsWords
    {
      get
      {
        TvGuideBlock? about = _actionsAbout;
        if (about == null)
          return "";

        TvGuideRow? row = RowOf(about);
        string channel = row == null ? "" : "  " + row.Channel.Name;
        return about.Title + "  " + about.StartText + channel;
      }
    }

    /// <summary>Whether a dialog is standing open.</summary>
    public bool ActionsOpen
    {
      get { return _actionsAbout != null; }
    }

    /// <summary>Said when the answers are ready and a dialog has to be put up.</summary>
    public event Action? ActionsRequested;

    /// <summary>Said when the answer was given and the dialog has to be taken away again.</summary>
    public event Action? ActionsClosed;

    /// <summary>
    /// Puts the question up for one programme: what can be asked about it now, in the reference's order, and asked
    /// of the backend first when the answer depends on a plan the programme only says exists.
    /// </summary>
    public async Task OpenProgramActionsAsync(TvGuideBlock? block, DateTimeOffset? moment = null)
    {
      if (block == null || _disposed)
        return;

      _actionsAbout = block;
      ProgramActions.Clear();

      // Back on the screen's thread for what follows, the way the settings screens do it: the list and the events
      // that open and close the dialog are what a screen is built of, and a question about tomorrow's evening is not
      // something a screen wants answered from a pool thread.
      foreach (TvProgramAction action in await ProgramActionsForAsync(block.Program, moment ?? DateTimeOffset.Now)
          .ConfigureAwait(true))
      {
        ProgramActions.Add(action);
      }

      if (_disposed)
        return;

      Raise(nameof(ActionsAbout));
      Raise(nameof(ActionsOpen));
      Raise(nameof(ActionsWords));
      ActionsRequested?.Invoke();
    }

    /// <summary>Takes the dialog away, which the screen answers by stepping off its own stop.</summary>
    public void CloseProgramActions()
    {
      if (_actionsAbout == null)
        return;

      _actionsAbout = null;
      Raise(nameof(ActionsAbout));
      Raise(nameof(ActionsOpen));
      ActionsClosed?.Invoke();
    }

    /// <summary>
    /// The answers a viewer can have for one programme, in the order the reference put them up and under the same
    /// conditions (<c>TV/SlimTvClient/Models/SlimTvGuideModelBase.cs:126-167</c> and
    /// <c>Models/SlimTvModelBase.cs:373-432</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three of its answers are not here, and each is left out for a reason rather than by oversight. "Watch
    /// recording from beginning" belongs to a player that can start a file at the beginning, and this front end
    /// reaches a television through a channel, so offering it would be a press that does nothing. "Choose recording
    /// schedule type" has nothing to choose between, since a TVHeadend keeps one kind of series (part bn). And the
    /// extension list at the end of the reference's list was filled by other plugins through
    /// <c>SlimTvExtensionBuilder</c>, which this front end has no equivalent of - there is nothing that could
    /// register an answer here.
    /// </para>
    /// <para>
    /// What a plan <i>is</i> cannot be read off a programme: the EPG says that something is planned and what the
    /// backend calls the state, but not whether the plan renews as a series - and the reference's list turns on
    /// exactly that (<c>SeriesScheduled</c>). So one question about the entry is asked here, which is the question
    /// the reference asked through <c>GetRecordingStatusAsync</c> before building its list; a backend that will not
    /// answer it leaves the list as short as the reference's was when its own status question failed.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<TvProgramAction>> ProgramActionsForAsync(TvProgram program,
        DateTimeOffset moment)
    {
      List<TvProgramAction> actions = new List<TvProgramAction>();
      if (program == null)
        return actions;

      bool running = moment >= program.Start && moment <= program.Stop;

      // The reference's first two lines, in its order and with its one comment kept in mind: "if program is over
      // already, there is nothing to do" - which is said about watching, not about recording. The reference went on
      // to add its recording options below the sentence, because a series is about the airing that is still to come
      // and not about the one that just ended.
      if (program.Stop < moment)
        actions.Add(new TvProgramAction(TvProgramActionKind.SaidOnly, "Program already over"));
      else if (running)
        actions.Add(new TvProgramAction(TvProgramActionKind.Watch, "Watch program now"));

      // What a plan is cannot be read off a programme, so the one question about the entry is asked - but only when
      // the programme says a plan is there. A programme without a dvrUuid is the reference's own "None" answer,
      // which it got without asking, and a box that was never named is the reference's own "no schedule control at
      // all": in both cases the recording options below are not offered, and for the same reason it did not offer
      // them - there is nothing that could answer.
      TvRecording? entry = null;
      bool statusWasRead = true;

      if (program.IsRecorded && !string.IsNullOrWhiteSpace(program.ScheduleId))
      {
        TvBackendSettings settings = _settingsProvider();
        if (!settings.IsConfigured)
          statusWasRead = false;
        else
        {
          try
          {
            using (TvHeadendApiClient client = _buildClient(settings))
            {
              entry = await client.GetRecordingAsync(program.ScheduleId!).ConfigureAwait(false);
            }
          }
          catch (Exception)
          {
            statusWasRead = false;
          }

          statusWasRead = statusWasRead && entry != null;
        }
      }

      if (!statusWasRead)
        return actions;

      bool recording = (entry != null && entry.IsRecording) || program.IsBeingRecorded;
      bool ahead = program.Stop >= moment;

      if (entry != null && entry.IsSeries)
      {
        if (recording || ahead)
        {
          actions.Add(recording
              ? new TvProgramAction(TvProgramActionKind.StopRecording,
                  "Stop current recording '" + program.Title + "'")
              : new TvProgramAction(TvProgramActionKind.DeleteSingle, "Delete single program"));
        }

        actions.Add(new TvProgramAction(TvProgramActionKind.DeleteSeries, "Delete full series schedule"));
        return actions;
      }

      // The reference's four-way choice, in its order: a writing that can be stopped, a running programme that can
      // still be joined, a plan that can be taken away, and a future programme that can be asked for. It is a
      // choice, not a list - one line comes out of it - and "Record series" stands under it whatever came out.
      if (recording)
        actions.Add(new TvProgramAction(TvProgramActionKind.StopRecording,
            "Stop current recording '" + program.Title + "'"));
      else if (running)
        actions.Add(new TvProgramAction(TvProgramActionKind.RecordOnce,
            "Record program '" + program.Title + "'"));
      else if (entry != null && entry.IsWaiting)
        actions.Add(new TvProgramAction(TvProgramActionKind.DeleteSingle, "Delete schedule"));
      else if (ahead)
        actions.Add(new TvProgramAction(TvProgramActionKind.RecordOnce, "Schedule recording"));

      actions.Add(new TvProgramAction(TvProgramActionKind.RecordSeries, "Record series"));
      return actions;
    }

    /// <summary>
    /// Does what the viewer picked and closes the dialog over the answer. The programmes are read back afterwards
    /// because a series touches more than the one block stood on - the reference re-drew the whole grid for exactly
    /// that reason (<c>SlimTvGuideExtScheduleModel.CreateOrDeleteSchedule</c>).
    /// </summary>
    /// <returns>Whether the backend did it, so that a screen can say which of the two it just heard.</returns>
    public async Task<bool> RunProgramActionAsync(TvProgramAction? action)
    {
      TvGuideBlock? about = ActionsAbout;
      if (about == null || action == null || _disposed)
        return false;

      if (action.Kind == TvProgramActionKind.Watch)
      {
        CloseProgramActions();
        return Watch(about);
      }

      if (action.Kind == TvProgramActionKind.SaidOnly)
      {
        CloseProgramActions();
        return true;
      }

      TvProgram program = about.Program;
      string uuid = program.ScheduleId ?? "";
      string said;
      bool made = false;

      TvBackendSettings settings = _settingsProvider();
      if (!settings.IsConfigured)
      {
        Status = "No TV backend has been written down yet.";
        CloseProgramActions();
        return false;
      }

      IsLoading = true;
      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          TvBackendAnswer answer = await AskForActionAsync(client, action, program, uuid).ConfigureAwait(false);
          made = answer.Succeeded;
          said = made
              ? WordsFor(action, program)
              : "The backend would not do that: " + answer.Complaint;
        }
      }
      catch (Exception ex)
      {
        said = "The backend would not answer: " + ex.Message;
      }

      CloseProgramActions();
      await LoadProgramsAsync(settings).ConfigureAwait(false);

      if (!_disposed)
      {
        Status = said;
        // The block the grid was redrawn under, so that the mark on it says what just happened.
        _selectedBlock = FindAgain(about);
        Raise(nameof(SelectedBlock));
      }

      return made;
    }

    /// <summary>One question about the entry, or the making of one, asked of the box the guide is reading.</summary>
    private static async Task<TvBackendAnswer> AskForActionAsync(TvHeadendApiClient client,
        TvProgramAction action, TvProgram program, string uuid)
    {
      switch (action.Kind)
      {
        case TvProgramActionKind.StopRecording:
          return await client.StopRecordingAsync(uuid).ConfigureAwait(false);

        case TvProgramActionKind.DeleteSingle:
          return await client.RemoveRecordingAsync(uuid).ConfigureAwait(false);

        case TvProgramActionKind.DeleteSeries:
          // The series first and the entry after, because a backend still renewing the event answers a delete by
          // putting the entry back - the same order the schedules screen keeps for the same reason.
          TvBackendAnswer series = await client.SetSeriesRenewalAsync(uuid, false).ConfigureAwait(false);
          return series.Succeeded
              ? await client.RemoveRecordingAsync(uuid).ConfigureAwait(false)
              : series;

        case TvProgramActionKind.RecordSeries:
          return program.IsRecorded && !string.IsNullOrWhiteSpace(uuid)
              ? await client.SetSeriesRenewalAsync(uuid, true).ConfigureAwait(false)
              : await client.ScheduleRecordingAsync(program, true).ConfigureAwait(false);

        default:
          return await client.ScheduleRecordingAsync(program, false).ConfigureAwait(false);
      }
    }

    /// <summary>What a done answer is called in the guide's own line.</summary>
    private static string WordsFor(TvProgramAction action, TvProgram program)
    {
      switch (action.Kind)
      {
        case TvProgramActionKind.StopRecording:
          return "The recording of '" + program.Title + "' is stopped.";
        case TvProgramActionKind.DeleteSingle:
          return "The plan for '" + program.Title + "' is gone.";
        case TvProgramActionKind.DeleteSeries:
          return "The series behind '" + program.Title + "' is gone, and this entry with it.";
        case TvProgramActionKind.RecordSeries:
          return "'" + program.Title + "' is kept as a series from now on.";
        default:
          return "'" + program.Title + "' will be recorded.";
      }
    }

    #endregion

    private void Cancel()
    {
      CancellationTokenSource? old = _inFlight;
      _inFlight = null;
      if (old != null)
      {
        try
        {
          old.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
      }
    }

    private void Raise(string name)
    {
      PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Said when a window, a row or a bouquet changed, which is when a screen redraws.
    /// </summary>
    /// <remarks>
    /// The class carries <see cref="System.ComponentModel.INotifyPropertyChanged"/> for one plain reason: the
    /// event on its own is enough for the code beside a model, which is how the grid redraws, but Avalonia's
    /// bindings look for the interface and bind nothing without it - a guide without it would sit there telling
    /// the viewer about the programme that was under the cursor when the screen opened, forever, and no test of
    /// the model would ever notice.
    /// </remarks>
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
  }

  /// <summary>
  /// One hour standing on the ruler above the grid.
  /// </summary>
  public sealed class TvGuideHour
  {
    public TvGuideHour(DateTimeOffset moment, int column, string text)
    {
      Moment = moment;
      Column = column;
      Text = text;
    }

    /// <summary>The hour itself.</summary>
    public DateTimeOffset Moment { get; }

    /// <summary>The column it is written over.</summary>
    public int Column { get; }

    /// <summary>The hour as the viewer reads it.</summary>
    public string Text { get; }
  }

  /// <summary>
  /// Which of the things a viewer can ask about a programme one answer in the dialog means.
  /// </summary>
  /// <remarks>
  /// The reference put its answers on commands rather than on words, and two of its words meant the same thing from
  /// two different places - "Stop current recording" whether a single programme or a series was being written - so
  /// an answer here carries what it does and the sentence is built around that.
  /// </remarks>
  public enum TvProgramActionKind
  {
    /// <summary>Standing on a programme that is over: nothing can be asked, and the list says so.</summary>
    SaidOnly,

    /// <summary>Get to the channel this is on, which is the reference's "Watch program now".</summary>
    Watch,

    /// <summary>End the writing and leave the entry readable.</summary>
    StopRecording,

    /// <summary>Ask for this one programme, from its own event id.</summary>
    RecordOnce,

    /// <summary>Take this one entry away and leave a series behind it standing.</summary>
    DeleteSingle,

    /// <summary>Let go of the series and take the entry with it.</summary>
    DeleteSeries,

    /// <summary>Ask for this event every time the broadcaster lists it again.</summary>
    RecordSeries
  }

  /// <summary>
  /// One answer in the guide's "Choose program action..." dialog.
  /// </summary>
  public sealed class TvProgramAction
  {
    public TvProgramAction(TvProgramActionKind Kind, string Word)
    {
      this.Kind = Kind;
      this.Word = Word;
    }

    /// <summary>What pressing it does.</summary>
    public TvProgramActionKind Kind { get; }

    /// <summary>The line on the list, which the reference wrote with the programme's own name in it.</summary>
    public string Word { get; }

    /// <summary>Whether a press has anything behind it - the one line of a finished programme never has.</summary>
    public bool IsCommand
    {
      get { return Kind != TvProgramActionKind.SaidOnly; }
    }

    public override string ToString()
    {
      return Word;
    }
  }
}
