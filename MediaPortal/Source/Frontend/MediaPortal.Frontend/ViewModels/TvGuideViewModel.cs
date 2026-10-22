using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
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
        bool endsAfterWindow, bool genreColors = true, bool seriesInfo = true)
    {
      Program = program;
      Column = column;
      ColumnSpan = columnSpan;
      StartsBeforeWindow = startsBeforeWindow;
      EndsAfterWindow = endsAfterWindow;
      GenreColors = genreColors;
      SeriesInfo = seriesInfo;
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

    /// <summary>Whether the viewer wants programmes coloured by what they are.</summary>
    public bool GenreColors { get; }

    /// <summary>Whether the viewer wants a programme to carry its episode line.</summary>
    public bool SeriesInfo { get; }

    /// <summary>
    /// What the block is filled with: its genre's colour, or the one colour the guide uses when a viewer said they
    /// would rather not have genres painted at all (<c>EpgShowGenreColors</c>).
    /// </summary>
    public string Fill
    {
      get { return GenreColors ? TvGenreColors.ForGenre(Program.PrimaryGenre) : TvGenreColors.FLAT; }
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
        if (!SeriesInfo)
          return "";

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

    /// <summary>
    /// The channel's picture, as its own TV machine drew it, or null while it has not arrived and for a channel the
    /// machine has no picture for. Bytes rather than a picture, because a row holding a bitmap could not be built by
    /// a test without a window; the screen decodes them, as every screen of this front end decodes what a machine
    /// described in bytes.
    /// </summary>
    public byte[]? Logo { get; internal set; }

    /// <summary>
    /// Whether this row has a picture to wait for at all, which is the viewer having asked for pictures and the
    /// machine having named one for this channel. A guide leaves room for a picture only beside rows that want one.
    /// </summary>
    public bool WantsLogo { get; internal set; }
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

    /// <summary>
    /// The minutes one column stands for by default, which is what three hours across ninety columns comes to. A
    /// viewer changing the hours changes this with it (<see cref="ColumnMinutes"/>), exactly as the reference's grid
    /// divided its hours by its own seventy-five columns.
    /// </summary>
    public const int MINUTES_PER_COLUMN = 2;

    /// <summary>How wide the window is by default, in minutes: three hours.</summary>
    public const int WINDOW_MINUTES = COLUMNS * MINUTES_PER_COLUMN;

    private readonly Func<TvBackendSettings> _settingsProvider;
    private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
    private readonly ChannelLogos? _logos;
    private readonly Action<Action> _marshal;
    private readonly TvMediaType _kind;

    /// <summary>
    /// Every channel of the bouquet, whether or not it is on the screen. The grid only ever holds the page the
    /// viewer is standing on (<see cref="Rows"/>), because a real backend has three hundred channels and three
    /// hundred rows of ninety columns is twenty-seven thousand controls on a screen that can show eight of them.
    /// </summary>
    private readonly List<TvGuideRow> _allRows = new List<TvGuideRow>();

    private int _viewOffset;
    private string _pickedChannel = "";
    private CancellationTokenSource? _inFlight;
    private int _generation;
    private bool _disposed;

    private TvChannelGroup? _selectedGroup;
    private TvGuideRow? _selectedRow;
    private TvGuideBlock? _selectedBlock;
    private TvGuideBlock? _actionsAbout;
    private bool _zapFromGuide = true;
    private double _columnMinutes = MINUTES_PER_COLUMN;
    private int _visibleRows = TvBackendSettings.DEFAULT_VISIBLE_ROWS;
    private bool _showChannelNames = true;
    private bool _showChannelNumbers = true;
    private bool _showChannelLogos = true;
    private bool _showGenreColors = true;
    private bool _showSeriesInfo = true;
    private bool _hideAllChannelsGroup;
    private DateTimeOffset _start = DateTimeOffset.UtcNow;
    private string _status = "";
    private bool _isLoading;
    private Task _inFlightLoad = Task.CompletedTask;

    /// <summary>
    /// Builds the guide. The settings and the client are handed over as functions because a guide left open
    /// while somebody changes the backend's address must not keep asking the machine it stopped talking to.
    /// </summary>
    /// <param name="kind">Whether this is the television grid or the radio one.</param>
    /// <param name="settingsProvider">Where the grid's shape and the backend's address come from.</param>
    /// <param name="clientFactory">How to build the backend to ask. A grid under test hands in a scripted one.</param>
    /// <param name="marshal">
    /// How to get an answer onto the thread the screen lives on, or null to apply it where it arrives. A backend
    /// across the network answers on a thread of its own, and the rows a screen is bound to belong to the thread
    /// that made them, so a grid with controls on it has to hand this over - see <c>Views/ScreenThread.cs</c>.
    /// </param>
    /// <param name="logos">
    /// Where the channel pictures come from, or null for a grid that is not going to draw any - which is what every
    /// test does and what a screen without a picture shelf does too. A guide with them asks for the pictures of the
    /// channels it is showing, never for the pictures of the three hundred it is not.
    /// </param>
    public TvGuideViewModel(TvMediaType kind, Func<TvBackendSettings>? settingsProvider = null,
        Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null, Action<Action>? marshal = null,
        ChannelLogos? logos = null)
    {
      _kind = kind;
      _settingsProvider = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return DemoTvBackend.Open(settings);
      };
      _marshal = marshal ?? delegate (Action action) { action(); };
      _logos = logos;

      // The shape of the grid comes from the settings before the first window is worked out, because the width of a
      // column is what a window's start is floored to.
      FollowSettings();
      _start = Floor(_start);
    }

    /// <summary>
    /// Takes the answers that decide what the grid looks like out of the settings: how wide a column is, how many
    /// rows the screen has room for, what stands at the head of a row, and which of the two names a programme
    /// carries. Read again on every reload, so a viewer who changes the settings and walks back to the guide sees
    /// the guide they asked for without a restart - the reference read them once when its grid was built and needed
    /// a plugin reload for a change to show, which is the part of its behaviour worth diverging from.
    /// </summary>
    private void FollowSettings()
    {
      TvBackendSettings settings;
      try
      {
        settings = _settingsProvider();
      }
      catch (Exception)
      {
        // A settings file nobody can read is not a reason to draw no grid. The defaults are the shape the guide
        // had before anybody wrote anything down.
        return;
      }

      if (settings == null)
        return;

      _columnMinutes = settings.VisibleHoursOr * 60.0 / COLUMNS;
      _visibleRows = settings.VisibleRowsOr;
      _showChannelNames = settings.ShowChannelNames;
      _showChannelNumbers = settings.ShowChannelNumbers;
      _showChannelLogos = settings.ShowChannelLogos;
      _showGenreColors = settings.ShowGenreColors;
      _showSeriesInfo = settings.ShowSeriesInfo;
      _hideAllChannelsGroup = settings.HideAllChannelsGroup;
      _zapFromGuide = settings.ZapFromGuide;
    }

    /// <summary>The minutes one column of this guide's grid stands for, once the viewer's hours are divided up.</summary>
    public double ColumnMinutes
    {
      get { return _columnMinutes; }
    }

    /// <summary>How wide the window this guide draws is, in minutes: its own columns times their own width.</summary>
    public double WindowMinutes
    {
      get { return COLUMNS * _columnMinutes; }
    }

    /// <summary>How many channels the screen keeps room for at once.</summary>
    public int VisibleRows
    {
      get { return _visibleRows; }
    }

    /// <summary>Whether a row's head carries the channel's number.</summary>
    public bool ShowChannelNumbers
    {
      get { return _showChannelNumbers; }
    }

    /// <summary>Whether a row's head carries the channel's name.</summary>
    public bool ShowChannelNames
    {
      get { return _showChannelNames; }
    }

    /// <summary>
    /// Whether a row's head carries the channel's own picture. Only ever true for a guide that was handed somewhere
    /// to get pictures from; a grid without one says so here so that a screen does not leave a column empty.
    /// </summary>
    public bool ShowChannelLogos
    {
      get { return _showChannelLogos && _logos != null; }
    }

    /// <summary>Whether a programme is coloured by what it is.</summary>
    public bool ShowGenreColors
    {
      get { return _showGenreColors; }
    }

    /// <summary>Whether a programme carries its "S07 E03" line.</summary>
    public bool ShowSeriesInfo
    {
      get { return _showSeriesInfo; }
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
      get { return _start.AddMinutes(WindowMinutes); }
    }

    /// <summary>The words above the grid: the stretch this window covers, on the viewer's own clock.</summary>
    public string WindowText
    {
      get
      {
        return _start.LocalDateTime.ToString("t", CultureInfo.CurrentCulture) + " - "
            + End.LocalDateTime.ToString("t", CultureInfo.CurrentCulture)
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
            (long) Math.Round((now - _start).TotalMinutes / _columnMinutes, MidpointRounding.AwayFromZero));
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
        // Cleared through the same door an answer goes through, so that a reload started from anywhere but the
        // screen's own thread cannot empty a grid somebody is looking at from the wrong thread.
        _marshal(delegate
        {
          ClearRows();
          Hours.Clear();
          Groups.Clear();
          SelectedGroup = null;
          Status = "No TV backend has been written down yet. Say where the television comes from on the "
              + "television settings, then read this again.";
        });
        return;
      }

      InFlight = LoadProgramsAsync(settings);
      await InFlight.ConfigureAwait(false);
    }

    /// <summary>Moves the window half a screen back and reads it again.</summary>
    public void ScrollBackward()
    {
      Move(-(int) Math.Round(WindowMinutes / 2));
    }

    /// <summary>Moves the window half a screen on.</summary>
    public void ScrollForward()
    {
      Move((int) Math.Round(WindowMinutes / 2));
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

      // A press on a box moves the cursor the arrows walk as well. Without it the pointer and the remote disagree
      // about which channel is being stood on, and the next arrow answers from the row the guide happened to open
      // on rather than from the one the viewer just touched.
      TvGuideRow? pressed = RowOf(block);
      if (pressed != null && !ReferenceEquals(pressed, _selectedRow))
        SelectedRow = pressed;
      if (pressed != null)
        _pickedChannel = pressed.Channel.Id ?? "";

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
    /// back over what is already built; it walks every channel of the bouquet rather than the page, because a
    /// programme the viewer was told about stays on the same channel whether or not that channel is on the screen
    /// at the moment. A block that has been rebuilt away has no row and says so.
    /// </summary>
    private TvGuideRow? RowOf(TvGuideBlock block)
    {
      foreach (TvGuideRow row in _allRows)
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
        _marshal(delegate { Status = "No TV backend has been written down yet."; });
        return;
      }

      // The grid follows its viewer. A guide that is already open can be sitting on the screen while somebody
      // changes the settings screen, and the reference read its settings once per grid - never per programme - so
      // this is the same: the numbers are picked up when a guide is (re)built, and what is on the screen while it
      // is being looked at does not change shape under the viewer's hand. Handed over rather than done here,
      // because this is also the first thing that happens after a recording plan has been asked for, and by then
      // the work is on the thread the answer arrived on.
      _marshal(FollowSettings);

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

      _marshal(delegate
      {
        IsLoading = true;
        Status = "Reading " + group.Name + "...";
        BuildHours();
      });

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

          // The whole evening is put on the screen in one go and on the thread the screen lives on. Half an hour
          // of waiting for an answer ends on a thread of the thread pool, and the rows a grid is bound to belong
          // to the thread that built the grid: filling them where the answer arrived is how the guide threw
          // "a different thread owns it" across its own status line and drew the evening collapsed against the
          // headers. What is drawn and what the screen says about it also have to be the same moment, or the
          // bottom line counts programmes the grid has not been given yet.
          _marshal(delegate
          {
            if (mine != _generation)
              return;

            string? rowsSaid = BuildRows(channels, programs);
            Raise(nameof(Rows));

            // Counted over the whole bouquet rather than over what is on the screen, because "8 channels" about a
            // bouquet of eighty would be a lie about the television; and said with the page in it, because a
            // viewer who sees eight rows and hears about eighty channels should know the other seventy-two are
            // walked rather than missing.
            Status = (programsMissing ?? rowsSaid
                ?? string.Format("{0} channels, {1} programmes between {2}", _allRows.Count, programs.Count,
                    WindowText))
                + (_allRows.Count > Rows.Count
                    ? string.Format(" - {0} of them on the screen, the rest walked with the arrows", Rows.Count)
                    : "");

            // A guide full of invented programmes has to say so. The channel names are made up well enough to pass
            // for a real bouquet at a glance, and a viewer who later wonders why their television never shows
            // "Aurora Krankenhaus" is a viewer who was never told.
            if (askWith.DemoData && !string.IsNullOrEmpty(Status))
              Status += " (" + DemoTvBackend.NOTICE + ")";
          });
        }
      }
      catch (OperationCanceledException)
      {
        // A press arrived while this was out. The newer question says what it says.
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation)
          _marshal(delegate { Status = ex.Message; });
      }
      catch (Exception ex)
      {
        if (mine == _generation)
          _marshal(delegate { Status = "The programme guide went wrong: " + ex.Message; });
      }
      finally
      {
        if (mine == _generation)
          _marshal(delegate { IsLoading = false; });
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
      _marshal(delegate
      {
        IsLoading = true;
        Status = "Asking " + settings.Describe() + " which channel groups it has...";
      });
      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          IReadOnlyList<TvChannelGroup> groups = await client.GetChannelGroupsAsync(cancellation)
              .ConfigureAwait(false);

          if (mine != _generation)
            return false;

          List<TvChannelGroup> ofThisKind = groups.Where(g => !LooksWrongKind(g.Name) && !HiddenGroup(g.Name))
              .ToList();

          // Which bouquet to open on is worked out from what the backend said rather than from what is on the
          // screen, because this answer arrives on a thread of the thread pool and it is the screen's own list of
          // bouquets that holds the answer.
          TvChannelGroup? picked = ofThisKind.Count == 0
              ? null
              : ofThisKind[PreferredGroupIndex(ofThisKind)];
          if (mine == _generation)
            _selectedGroup = picked;

          _marshal(delegate
          {
            if (mine != _generation)
              return;

            Groups.Clear();
            foreach (TvChannelGroup group in ofThisKind)
              Groups.Add(group);

            if (picked == null)
            {
              ClearRows();
              Hours.Clear();
              Status = string.Format("{0} answered but offered no channel groups, so there is no guide to read. "
                  + "The groups are made in the backend's own configuration.", settings.Describe());
              return;
            }

            Raise(nameof(SelectedGroup));
          });

          // The bouquet a guide opens on is the one whose own name says what the guide is for, when one of them
          // does. Whoever opens the radio guide wants the radio stations, not an empty grid made of whichever
          // television bouquet the backend happens to list first. The picker is assigned here rather than gone
          // through, because going through it would start the next question on its own and this one has to
          // answer with the finished grid.
          return picked != null && mine == _generation;
        }
      }
      catch (OperationCanceledException)
      {
        return false;
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation)
          _marshal(delegate { Status = ex.Message; });
        return false;
      }
      finally
      {
        if (mine == _generation)
          _marshal(delegate { IsLoading = false; });
      }
    }

    /// <summary>
    /// Which bouquet to open on. A television guide takes the backend's first bouquet, which on every backend
    /// this was tried against is the full one. The radio guide looks for the bouquet whose name says radio,
    /// because a radio guide that opens on the first bouquet opens on a grid of television channels and shows
    /// somebody their radio as an empty evening. Names are the only thing a backend says about a bouquet besides
    /// what is in it, and guessing wrong costs a viewer one press of the bouquet button.
    /// </summary>
    private int PreferredGroupIndex(List<TvChannelGroup> candidates)
    {
      if (_kind != TvMediaType.Radio)
        return 0;

      string[] hints = { "radio", "audio", "hören", "hoeren" };
      for (int i = 0; i < candidates.Count; i++)
      {
        string said = (candidates[i].Name ?? "").ToLowerInvariant();
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

      // Which channel the viewer was standing on, remembered by name rather than by row: the rows are about to be
      // thrown away and built again, and a guide that jumped back to its first channel every time the window moved
      // thirty minutes on could not be read while being walked.
      string standingOn = _selectedRow == null ? _pickedChannel : _selectedRow.Channel.Id ?? "";

      _allRows.Clear();
      foreach (TvChannel channel in ofThisBouquet)
      {
        TvGuideRow row = new TvGuideRow(channel);
        row.WantsLogo = ShowChannelLogos && channel.HasLogo;
        foreach (TvProgram program in byChannel[channel.Id].OrderBy(p => p.Start))
        {
          TvGuideBlock? block = Place(program);
          if (block != null)
          {
            block.IsRunning = program.IsOnAt(now);
            row.Blocks.Add(block);
          }
        }
        _allRows.Add(row);
      }
      _pickedChannel = standingOn;

      // The page is worked out of that, and only the page goes onto the screen.
      _viewOffset = 0;
      ShowPageIndex(IndexOfChannel(standingOn));
      FillPage();

      SelectedBlock = FindAgain(previous) ?? FirstBlockStartingNow();
      SelectedRow = SelectedBlock == null
          ? (Rows.Count > 0 ? Rows[0] : null)
          : RowOf(SelectedBlock);
      return rowsSaid;
    }

    #region Walking the channels

    /// <summary>
    /// How many channels the bouquet has, which is more than the screen shows: the grid holds one page of them and
    /// the viewer walks the rest. The reference's guide was the same shape - its <c>EpgGrid</c> drew
    /// <c>EpgNumberOfRows</c> rows out of a list it kept an offset into
    /// (<c>TV/SlimTvClient/Controls/EpgGrid.cs:494-506</c>) - and the shape is not decoration: a household with
    /// three hundred channels would otherwise be carrying twenty-seven thousand controls on a screen that has room
    /// for eight rows.
    /// </summary>
    public int RowCount
    {
      get { return _allRows.Count; }
    }

    /// <summary>
    /// Which channel of the bouquet stands at the top of the grid. The reference called the same thing
    /// <c>_channelViewOffset</c>.
    /// </summary>
    public int ViewOffset
    {
      get { return _viewOffset; }
    }

    /// <summary>The last page that has a full screen of channels under it, which is where End goes.</summary>
    private int LastViewOffset
    {
      get { return Math.Max(_allRows.Count - Math.Max(1, _visibleRows), 0); }
    }

    /// <summary>Whether there is a channel above the top of the grid.</summary>
    public bool CanScrollRowsUp
    {
      get { return _viewOffset > 0; }
    }

    /// <summary>Whether there is a channel below the bottom of the grid.</summary>
    public bool CanScrollRowsDown
    {
      get { return _viewOffset < LastViewOffset; }
    }

    /// <summary>
    /// Whether the arrow can walk to the channel above. Said rather than left to be guessed because the screen
    /// has to know when to hand the key back: at the first channel of a bouquet the Up arrow belongs to whatever
    /// is above the grid, which is how a viewer gets out of a guide with a remote
    /// (<c>EpgGrid.cs:831-842</c> answered the same question for its scrolling).
    /// </summary>
    public bool CanMoveRowUp
    {
      get { return IndexOfRow(_selectedRow) > 0; }
    }

    /// <summary>Whether the arrow can walk to the channel below.</summary>
    public bool CanMoveRowDown
    {
      get
      {
        int at = IndexOfRow(_selectedRow);
        return at >= 0 && at < _allRows.Count - 1;
      }
    }

    /// <summary>Walks the channels one row up, which is what the reference's wheel and repeat-key did.</summary>
    public void ScrollRowsUp()
    {
      ScrollRowsBy(-1);
    }

    /// <summary>Walks the channels one row down.</summary>
    public void ScrollRowsDown()
    {
      ScrollRowsBy(1);
    }

    /// <summary>A screenful of channels back, which is PageUp.</summary>
    public void PageRowsUp()
    {
      ScrollRowsBy(-Math.Max(1, _visibleRows));
    }

    /// <summary>A screenful of channels on, which is PageDown.</summary>
    public void PageRowsDown()
    {
      ScrollRowsBy(Math.Max(1, _visibleRows));
    }

    /// <summary>The first channel of the bouquet, which is Home.</summary>
    public void FirstRow()
    {
      ScrollRowsTo(0);
    }

    /// <summary>The last screenful of the bouquet, which is End.</summary>
    public void LastRow()
    {
      ScrollRowsTo(LastViewOffset);
    }

    /// <summary>
    /// Walks to the channel under the one being stood on, and takes the moment along: the programme chosen in the
    /// new row is the one covering the time the old one began at, because a viewer walking down a guide is asking
    /// "what is on at half past eight over there", not "what is the first thing this backend happened to list".
    /// That is the reference's own habit of restoring the focus by the time it was standing at
    /// (<c>EpgGrid.cs:1216-1240</c>, <c>_focusTime</c>).
    /// </summary>
    public void MoveRowDown()
    {
      MoveRow(1);
    }

    /// <summary>Walks to the channel above.</summary>
    public void MoveRowUp()
    {
      MoveRow(-1);
    }

    /// <summary>
    /// The next programme on the row being stood on. At the right-hand end of a row there is no next one, and the
    /// reference did not stop there: it moved the window half an hour on, which is the only way a guide of three
    /// hours can be walked with two buttons (<c>EpgGrid.cs:965-986</c>).
    /// </summary>
    public void MoveProgramForward()
    {
      TvGuideRow? row = SelectedRow;
      TvGuideBlock? here = SelectedBlock;
      if (row == null || row.Blocks.Count == 0)
        return;

      int at = here == null ? -1 : row.Blocks.IndexOf(here);
      if (at >= 0 && at < row.Blocks.Count - 1)
      {
        SelectedBlock = row.Blocks[at + 1];
        return;
      }

      Move(MINUTES_PER_COLUMN * 15);
    }

    /// <summary>The programme before, and the window half an hour back when the row has none left.</summary>
    public void MoveProgramBackward()
    {
      TvGuideRow? row = SelectedRow;
      TvGuideBlock? here = SelectedBlock;
      if (row == null || row.Blocks.Count == 0)
        return;

      int at = here == null ? -1 : row.Blocks.IndexOf(here);
      if (at > 0)
      {
        SelectedBlock = row.Blocks[at - 1];
        return;
      }

      Move(-(MINUTES_PER_COLUMN * 15));
    }

    private void ScrollRowsBy(int rows)
    {
      ScrollRowsTo(_viewOffset + rows);
    }

    private void ScrollRowsTo(int offset)
    {
      int to = Math.Max(0, Math.Min(offset, LastViewOffset));
      if (to == _viewOffset)
        return;

      _viewOffset = to;
      FillPage();
    }

    /// <summary>
    /// Slides the page so that the channel at that index of the bouquet is on the screen, at the edge it came in
    /// from: a channel above the top becomes the top row and one below the bottom becomes the bottom row, rather
    /// than the page centring itself and the viewer losing the end of the list they were walking towards. The
    /// reference did the same in <c>SkipToChannelIndex</c> (<c>EpgGrid.cs:998-1032</c>) and its initial offset
    /// followed the same rule (<c>EpgGrid.cs:461-476</c>).
    /// </summary>
    private void ShowPageIndex(int index)
    {
      if (index < 0)
        return;

      if (index < _viewOffset)
        _viewOffset = index;
      else if (index >= _viewOffset + Math.Max(1, _visibleRows))
        _viewOffset = index - Math.Max(1, _visibleRows) + 1;

      _viewOffset = Math.Max(0, Math.Min(_viewOffset, LastViewOffset));
    }

    private void MoveRow(int step)
    {
      int at = IndexOfRow(_selectedRow);
      if (at < 0)
        at = _viewOffset;

      int to = Math.Max(0, Math.Min(at + step, _allRows.Count - 1));
      if (to == at && _selectedRow != null)
        return;

      TvGuideRow? into = _allRows[to];
      DateTimeOffset standingAt = _selectedBlock == null ? DateTimeOffset.UtcNow : _selectedBlock.Program.Start;

      SelectedRow = into;
      SelectedBlock = StandingBlock(into, standingAt);
      _pickedChannel = into.Channel.Id ?? "";
      ShowPageIndex(to);
      FillPage();
    }

    /// <summary>
    /// The programme a row is stood on at a given moment: the one covering it, or the first one said after it. When
    /// a row's evening ran out before the moment - which a real backend's few days of data almost never does, but a
    /// window at the end of what a backend has listed easily can - the last one is the answer, because it is the
    /// programme nearest the moment the viewer was standing at. The row's first programme is not: being sent back to
    /// the beginning of an evening while walking between channels is the thing this whole lookup exists to avoid.
    /// </summary>
    private static TvGuideBlock? StandingBlock(TvGuideRow row, DateTimeOffset moment)
    {
      TvGuideBlock? covering = row.Blocks.FirstOrDefault(b => b.Program.Start <= moment && b.Program.Stop > moment);
      if (covering != null)
        return covering;

      TvGuideBlock? after = row.Blocks.FirstOrDefault(b => b.Program.Start >= moment);
      if (after != null)
        return after;

      return row.Blocks.LastOrDefault();
    }

    /// <summary>Puts the page onto the collection the screen is bound to.</summary>
    private void FillPage()
    {
      int from = Math.Max(0, Math.Min(_viewOffset, LastViewOffset));
      int taken = Math.Max(1, _visibleRows);

      _viewOffset = from;
      Rows.Clear();
      for (int i = from; i < _allRows.Count && i < from + taken; i++)
        Rows.Add(_allRows[i]);

      Raise(nameof(Rows));
      Raise(nameof(ViewOffset));
      Raise(nameof(CanScrollRowsUp));
      Raise(nameof(CanScrollRowsDown));
      Raise(nameof(CanMoveRowUp));
      Raise(nameof(CanMoveRowDown));

      // The pictures of what is on the screen, and of nothing else. Asked here rather than while the rows were
      // built, because a real bouquet has three hundred channels and asking for three hundred pictures to show
      // eight of them is a question nobody watching television needs to ask.
      FetchLogosOfPage();
    }

    /// <summary>Raised when one row's picture has arrived, so that the screen refills that one image.</summary>
    public event Action<TvGuideRow>? LogoReady;

    /// <summary>
    /// Asks for the pictures of the channels on the screen. A row whose picture is already in memory is answered
    /// there and then, which is what walking back to a page the viewer looked at a minute ago does; only a picture
    /// nobody has asked for yet goes onto the wire.
    /// </summary>
    private void FetchLogosOfPage()
    {
      ChannelLogos? pictures = _logos;
      if (pictures == null || !_showChannelLogos || _disposed)
        return;

      foreach (TvGuideRow row in Rows)
      {
        if (!row.WantsLogo || row.Logo != null)
          continue;

        if (pictures.TryPeek(row.Channel, out byte[]? here) && here != null)
        {
          row.Logo = here;
          continue;
        }

        AskLogoAsync(row, pictures);
      }
    }

    /// <summary>
    /// Fetches one row's picture and hands it to the screen when it arrives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>async void</c> on purpose and safely: everything it waits for is inside its own <c>try</c>, so nothing an
    /// answer can throw escapes into the thread that happened to complete it. A picture is the one thing on this
    /// screen that is allowed to fail without saying anything - the guide said whatever the machine complained
    /// about when it was asked for the evening, and repeating that at every row of the grid would be a guide nobody
    /// can read.
    /// </para>
    /// <para>
    /// A row whose picture arrived after the viewer had walked on to another page is thrown away, which is what the
    /// generation count is for everywhere else in this model.
    /// </para>
    /// </remarks>
    private async void AskLogoAsync(TvGuideRow row, ChannelLogos pictures)
    {
      int mine = _generation;
      byte[]? picture;

      try
      {
        picture = await pictures.PictureOfAsync(row.Channel).ConfigureAwait(false);
      }
      catch (Exception)
      {
        return;
      }

      if (picture == null || _disposed || mine != _generation)
        return;

      _marshal(delegate
      {
        if (_disposed || row.Logo != null)
          return;

        row.Logo = picture;
        LogoReady?.Invoke(row);
      });
    }

    private int IndexOfRow(TvGuideRow? row)
    {
      return row == null ? -1 : _allRows.IndexOf(row);
    }

    private int IndexOfChannel(string channelId)
    {
      if (string.IsNullOrEmpty(channelId))
        return -1;

      for (int i = 0; i < _allRows.Count; i++)
      {
        if (string.Equals(_allRows[i].Channel.Id, channelId, StringComparison.Ordinal))
          return i;
      }
      return -1;
    }

    /// <summary>
    /// Empties both the grid and the list behind it, which is what a guide with nothing to show needs: the details
    /// along the bottom belong to a programme the grid no longer has, so they go with it.
    /// </summary>
    private void ClearRows()
    {
      _allRows.Clear();
      _viewOffset = 0;
      _pickedChannel = "";
      Rows.Clear();
      SelectedRow = null;
      SelectedBlock = null;
      Raise(nameof(Rows));
      Raise(nameof(CanScrollRowsUp));
      Raise(nameof(CanScrollRowsDown));
    }

    #endregion

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

      int column = (int) Math.Round((shown - Start).TotalMinutes / _columnMinutes,
          MidpointRounding.AwayFromZero);
      int span = (int) Math.Round((until - shown).TotalMinutes / _columnMinutes, MidpointRounding.AwayFromZero);

      column = Math.Max(0, Math.Min(COLUMNS - 1, column));
      span = Math.Max(1, Math.Min(COLUMNS - column, span));

      return new TvGuideBlock(program, column, span, before, after, _showGenreColors, _showSeriesInfo);
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
        int column = (int) Math.Round((hour - _start).TotalMinutes / _columnMinutes,
            MidpointRounding.AwayFromZero);
        if (column < 0 || column >= COLUMNS)
          continue;
        Hours.Add(new TvGuideHour(hour, column,
            hour.LocalDateTime.ToString("t", CultureInfo.CurrentCulture)));
      }
    }

    /// <summary>
    /// Which bouquets a viewer said they never want to be offered - the same rule the tuner follows, for the same
    /// reason, and read once per grid like everything else the guide takes from the settings.
    /// </summary>
    private bool HiddenGroup(string? name)
    {
      return _hideAllChannelsGroup && TvBackendSettings.IsAllChannelsGroup(name);
    }

    /// <summary>
    /// The moment a window opens when it is moved onto the grid: the column edge at or before the moment asked
    /// for, so that a programme beginning on a column line always stands on a column line and never across two.
    /// The count is kept from the epoch, which is what the columns are counted from too.
    /// </summary>
    private DateTimeOffset Floor(DateTimeOffset moment)
    {
      double columnSeconds = _columnMinutes * 60.0;
      long step = columnSeconds >= 1
          ? Math.Max(1, (long) Math.Round(columnSeconds))
          : 1;

      long seconds = (long) Math.Floor((moment - DateTimeOffset.FromUnixTimeSeconds(0)).TotalSeconds);
      long floored = seconds - (((seconds % step) + step) % step);
      return DateTimeOffset.FromUnixTimeSeconds(0).AddSeconds(floored);
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
    /// the switch off, every second press asks the question the dialog answers. It comes from the viewer's own
    /// settings, which the guide picks up whenever it builds a grid.
    /// </summary>
    public bool ZapFromGuide
    {
      get { return _zapFromGuide; }
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

      // The answers are collected off the screen's thread - a question about tomorrow's evening is nothing a
      // screen wants to be held up for - and put up through the way onto the screen's thread, because the list and
      // the events that open and close the dialog are what a screen is built of.
      IReadOnlyList<TvProgramAction> answers = await ProgramActionsForAsync(block.Program,
          moment ?? DateTimeOffset.Now).ConfigureAwait(false);

      if (_disposed)
        return;

      _marshal(delegate
      {
        ProgramActions.Clear();
        foreach (TvProgramAction action in answers)
          ProgramActions.Add(action);

        Raise(nameof(ActionsAbout));
        Raise(nameof(ActionsOpen));
        Raise(nameof(ActionsWords));
        ActionsRequested?.Invoke();
      });
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
        _marshal(delegate
        {
          Status = said;
          // The block the grid was redrawn under, so that the mark on it says what just happened.
          _selectedBlock = FindAgain(about);
          Raise(nameof(SelectedBlock));
        });
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
