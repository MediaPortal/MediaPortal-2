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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One channel of the list that goes over the picture: the channel, what is running on it, and what comes
  /// after.
  /// </summary>
  /// <remarks>
  /// This is the reference's miniguide row (<c>Skin/Nereus/screens/SlimTvClient-miniguide.xaml</c> with
  /// <c>SlimTvStyles.xaml:1392-1398</c>: fifty pixels high, columns <c>Auto | * | *</c>, the channel's own columns
  /// built from 85 pixels of number, 155 of logo and 245 of name), whose two programme cells are filled from the
  /// same now-and-next the strip under the picture uses. A channel the backend says nothing about gets the
  /// reference's own answer rather than a blank cell - it wrote "No program data." and hid the progress bar
  /// (<c>SlimTvClientModel.cs:1151-1171</c>, <c>Language/strings_en.xml:28</c>).
  /// </remarks>
  public sealed class TvWatchRow
  {
    public TvWatchRow(TvChannel channel, TvProgram? now, TvProgram? next)
    {
      Channel = channel;
      Now = now;
      Next = next;
    }

    /// <summary>The channel this row is about, which is what pressing the row tunes.</summary>
    public TvChannel Channel { get; }

    /// <summary>What is running on it, when the backend said.</summary>
    public TvProgram? Now { get; }

    /// <summary>What comes after that, when the backend said.</summary>
    public TvProgram? Next { get; }

    /// <summary>The number, or nothing for a channel that has none.</summary>
    public string Number
    {
      get { return Channel.Number > 0 ? Channel.Number.ToString(CultureInfo.InvariantCulture) : ""; }
    }

    public string Name
    {
      get { return Channel.Name; }
    }

    public string NowTitle
    {
      get { return Now == null ? "No programme data." : Now.Title; }
    }

    public string NextTitle
    {
      get { return Next == null ? "" : Next.Title; }
    }

    /// <summary>How far through the running programme it is, for the little bar in the row.</summary>
    public double Progress
    {
      get { return Now == null ? 0 : Now.ProgressAt(DateTimeOffset.UtcNow); }
    }

    /// <summary>Whether there is a programme to draw a bar for at all.</summary>
    public bool HasProgramme
    {
      get { return Now != null; }
    }
  }

  /// <summary>
  /// The tuning half of the television: which channel is on, what is running on it, and what the digits somebody
  /// is typing on a remote mean.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This model owns no player. It says which channel it wants on the screen and hands the address over -
  /// <see cref="PlayRequested"/> is the whole of the arrangement - because the front end has exactly one player
  /// for everything it plays, and a second one would mean a second picture, a second volume, and a second thing
  /// left ringing when the viewer walks away. Stopping is asked for in the same way.
  /// </para>
  /// <para>
  /// The reference's watching surface is <c>Skin/Nereus/screens/FullScreenContentTv.xaml</c> with its
  /// <c>PrimarySlimTvOSDStyle</c>: the picture, and a two hundred and sixty pixel strip along the bottom holding
  /// the channel, the running programme, the one after it in the one colour the watching screens use
  /// (<c>#009999</c>, <c>SlimTvColors.xaml:7</c>), and a progress bar eighteen pixels high with the time the
  /// programme ends at its right end. The list over the picture is its miniguide, and the digits somebody types
  /// are shown on their own, big, in the middle of the screen, for exactly as long as the timeout lasts - the
  /// reference never showed a channel list while zapping, only the number
  /// (<c>Skin/Nereus/superlayers/ChannelZapOSD.xaml:11-13</c>).
  /// </para>
  /// <para>
  /// Two of the reference's habits are worth saying out loud, because they look like omissions in a port. It never
  /// tells anybody that a zap failed: a channel with no stream, a card that could not be had, or a number that
  /// matches nothing leaves the viewer on the black screen they were taken to with no sentence anywhere
  /// (<c>SlimTVMPExtendedProvider.cs:246-247</c>, <c>SlimTvClientModel.cs:496-511</c>). This one says so, because
  /// a media centre that will not say what went wrong is a media centre that gets unplugged. And it keeps playing
  /// when the viewer leaves the television screens - the picture goes away and the sound does not
  /// (<c>SlimTvClientModel.cs:1023-1031</c>) - whereas here leaving the screen closes this model, which asks for
  /// the stop, because what is playing in a house should not be decided by a window somebody walked out of.
  /// </para>
  /// </remarks>
  public sealed class TvTunerViewModel : IDisposable, System.ComponentModel.INotifyPropertyChanged
  {
    /// <summary>
    /// What the timeout is when nothing else can be said for it - a settings file that cannot be read, or a tuner
    /// asked before it has any. The number a viewer leaves in the settings is what the screen uses
    /// (<see cref="ZapTimeoutSeconds"/>); this is the reference's own default underneath it
    /// (<c>Settings/SlimTvClientSettings.cs:47-49</c>), described in its own settings text as "Configures the delay
    /// until tuning starts." - long enough for two or three digits on a remote, short enough not to feel stuck.
    /// </summary>
    public const double ZAP_TIMEOUT_SECONDS = 2;

    /// <summary>How much of the evening is read to fill the list over the picture.</summary>
    private const int LOOKAHEAD_HOURS = 4;

    private readonly TvMediaType _kind;
    private readonly Func<TvBackendSettings> _settingsProvider;
    private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
    private readonly Action<Action> _marshal;
    private CancellationTokenSource? _inFlight;
    private int _generation;
    private bool _disposed;
    private TvChannelGroup? _selectedGroup;
    private TvChannel? _tuned;
    private TvChannel? _previous;
    private TvNowAndNext? _running;
    private string _digits = "";
    private string _status = "";

    /// <param name="kind">Whether this is the television screen or the radio one.</param>
    /// <param name="settingsProvider">Where the backend's address and the zap timeout come from.</param>
    /// <param name="clientFactory">How to build the backend to ask. A screen under test hands in a scripted one.</param>
    /// <param name="marshal">
    /// How to get an answer onto the thread the screen lives on, or null to apply it where it arrives. A backend
    /// across the network answers on a thread of its own, and the channel list a screen is bound to belongs to the
    /// thread that made it, so a screen with controls on it has to hand this over - see <c>Views/ScreenThread.cs</c>.
    /// </param>
    public TvTunerViewModel(TvMediaType kind, Func<TvBackendSettings>? settingsProvider = null,
        Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null, Action<Action>? marshal = null)
    {
      _kind = kind;
      _settingsProvider = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return DemoTvBackend.Open(settings);
      };
      _marshal = marshal ?? delegate (Action action) { action(); };
    }

    /// <summary>
    /// Asked of whoever owns the player: this channel, now. The address is the backend's own, already made
    /// absolute; the channel travels with it because a screen that names its picture has to say which channel it
    /// is, and the player itself has no idea what a channel is.
    /// </summary>
    public event Action<TvChannel, MediaLocation>? PlayRequested;

    /// <summary>Asked of whoever owns the player when nothing is to be playing any more.</summary>
    public event Action? StopRequested;

    /// <summary>The channels somebody can zap between: the picked bouquet's, in the order they are said.</summary>
    public ObservableCollection<TvChannel> Channels { get; } = new ObservableCollection<TvChannel>();

    /// <summary>The same channels as the list over the picture, with each one's now and next beside it.</summary>
    public ObservableCollection<TvWatchRow> WatchRows { get; } = new ObservableCollection<TvWatchRow>();

    /// <summary>The bouquets the backend offered.</summary>
    public ObservableCollection<TvChannelGroup> Groups { get; } = new ObservableCollection<TvChannelGroup>();

    /// <summary>
    /// How long somebody gets to finish typing a channel number, which is the reference's <c>ZapTimeout</c>
    /// ("Configures the delay until tuning starts.", <c>Settings/SlimTvClientSettings.cs:47-49</c>). A tuner that
    /// has been taken off the screen still answers questions in a test, so a settings file that cannot be read
    /// leaves the two seconds the reference defaulted to rather than throwing at whoever pressed a digit.
    /// </summary>
    public double ZapTimeoutSeconds
    {
      get
      {
        try
        {
          return _settingsProvider().ZapTimeoutSecondsOr;
        }
        catch (Exception)
        {
          return ZAP_TIMEOUT_SECONDS;
        }
      }
    }

    /// <summary>
    /// Whether a typed number is a place in the bouquet or the number printed on a channel. On unless somebody said
    /// otherwise (<c>ZapByChannelIndex</c>, <c>Settings/SlimTvClientSettings.cs:96-97</c>).
    /// </summary>
    public bool ZapByChannelIndex
    {
      get
      {
        try
        {
          return _settingsProvider().ZapByChannelIndex;
        }
        catch (Exception)
        {
          return true;
        }
      }
    }

    /// <summary>Whether this is the television or the radio half.</summary>
    public TvMediaType Kind
    {
      get { return _kind; }
    }

    public string Heading
    {
      get { return _kind == TvMediaType.Radio ? "Radio" : "Television"; }
    }

    /// <summary>
    /// Whether there is a picture to draw. The reference answers this by not using a video surface for radio at
    /// all - <c>FullScreenContentRadio.xaml</c> paints a still photograph behind the strip instead - which is the
    /// right instinct: a black rectangle promising a picture that will never come is worse than no rectangle.
    /// </summary>
    public bool HasPicture
    {
      get { return _kind == TvMediaType.Television; }
    }

    /// <summary>The bouquet the zap list walks.</summary>
    public TvChannelGroup? SelectedGroup
    {
      get { return _selectedGroup; }
      set
      {
        if (ReferenceEquals(_selectedGroup, value))
          return;
        _selectedGroup = value;
        Raise(nameof(SelectedGroup));
        InFlight = LoadChannelsAsync();
      }
    }

    /// <summary>
    /// The question this model has out - the channels of a bouquet, or the programme data of a channel. Whoever
    /// has to wait for the screen to settle waits on this.
    /// </summary>
    public Task InFlight
    {
      get { return _inFlightLoad; }
      private set { _inFlightLoad = value; }
    }

    private Task _inFlightLoad = Task.CompletedTask;

    /// <summary>The channel on the screen, or null while nothing is tuned.</summary>
    public TvChannel? Tuned
    {
      get { return _tuned; }
      private set
      {
        _tuned = value;
        Raise(nameof(Tuned));
        Raise(nameof(ChannelSays));
        Raise(nameof(IsTuned));
      }
    }

    /// <summary>Whether anything is on.</summary>
    public bool IsTuned
    {
      get { return _tuned != null; }
    }

    /// <summary>The channel the digit "0" goes back to, which is the one before the one now on.</summary>
    public TvChannel? Previous
    {
      get { return _previous; }
      private set
      {
        _previous = value;
        Raise(nameof(Previous));
      }
    }

    /// <summary>The number and name of the channel on the screen, as the strip says them.</summary>
    public string ChannelSays
    {
      get
      {
        TvChannel? channel = _tuned;
        if (channel == null)
          return _kind == TvMediaType.Radio ? "nothing tuned" : "nothing on";
        return (channel.Number > 0 ? channel.Number.ToString(CultureInfo.InvariantCulture) + "  " : "")
            + channel.Name;
      }
    }

    /// <summary>What is running and what comes next on the tuned channel.</summary>
    public TvNowAndNext? Running
    {
      get { return _running; }
      private set
      {
        _running = value;
        Raise(nameof(Running));
        Raise(nameof(NowTitle));
        Raise(nameof(NextTitle));
        Raise(nameof(EndsAt));
        Raise(nameof(Progress));
        Raise(nameof(HasProgramme));
      }
    }

    /// <summary>
    /// The running programme's title. A screen with nothing to say about the programme says the reference's own
    /// words for that state ("No program data.", <c>Language/strings_en.xml:28</c>) rather than the backend
    /// model's blunter one, because this is the sentence a viewer reads instead of a programme and it should read
    /// like the sentence beside it in the list over the picture.
    /// </summary>
    public string NowTitle
    {
      get { return _running == null || !_running.HasAnything ? "No programme data." : _running.NowTitle; }
    }

    /// <summary>The one after it, led by the word that says which of the two it is.</summary>
    public string NextTitle
    {
      get
      {
        TvProgram? next = _running?.Next;
        return next == null ? "" : "next:  " + next.Title;
      }
    }

    /// <summary>The time the running programme ends, which the reference put at the bar's right end.</summary>
    public string EndsAt
    {
      get
      {
        TvProgram? now = _running?.Now;
        return now == null ? "" : now.Stop.LocalDateTime.ToString("t", CultureInfo.CurrentCulture);
      }
    }

    /// <summary>How far through the running programme it is, for the strip's bar.</summary>
    public double Progress
    {
      get
      {
        TvProgram? now = _running?.Now;
        return now == null ? 0 : now.ProgressAt(DateTimeOffset.UtcNow);
      }
    }

    /// <summary>Whether the strip has a programme to draw a bar for at all.</summary>
    public bool HasProgramme
    {
      get { return _running?.Now != null; }
    }

    /// <summary>
    /// The digits typed so far, shown big in the middle of the screen for as long as the timeout lasts. This was
    /// all the reference showed while zapping: a two hundred pixel box with the number in it, and no list of
    /// channels (<c>Skin/Nereus/superlayers/ChannelZapOSD.xaml:11-13</c>).
    /// </summary>
    public string Digits
    {
      get { return _digits; }
      private set
      {
        _digits = value;
        Raise(nameof(Digits));
        Raise(nameof(IsTypingANumber));
      }
    }

    /// <summary>Whether the big number is on the screen.</summary>
    public bool IsTypingANumber
    {
      get { return _digits.Length > 0; }
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

    #region Tuning

    /// <summary>
    /// Puts a channel on the screen. The address is the backend's own - a TVHeadend knows which streaming profile
    /// a channel wants better than anything here does - and a channel without one is said to be what it is rather
    /// than being quietly skipped, which is one of the two places the reference left a viewer guessing.
    /// </summary>
    /// <returns>Whether anything was asked of the player.</returns>
    public bool Tune(TvChannel? channel)
    {
      if (channel == null)
        return false;

      if (_tuned != null && (ReferenceEquals(channel, _tuned)
          || (channel.Id.Length > 0 && string.Equals(_tuned.Id, channel.Id, StringComparison.Ordinal))))
      {
        // The reference refused the same tune for the same reason, in its own comment: "Avoid subsequent tune
        // requests to same channel, it will only cause delays." (SlimTvClientModel.cs:513-550). Tuning to the
        // channel somebody is already watching stops the picture for no reason at all.
        Status = "Already on " + channel.Name + ".";
        return false;
      }

      if (channel.StreamUrl == null)
      {
        Status = channel.Name + " has no stream address on " + _settingsProvider().Describe()
            + ", so there is nothing to watch.";
        return false;
      }

      Previous = _tuned;
      Tuned = channel;
      Digits = "";
      Status = "Finding " + ChannelSays + "...";
      PlayRequested?.Invoke(channel, MediaLocation.FromUrl(channel.StreamUrl));
      InFlight = ReadNowAndNextAsync(channel);
      return true;
    }

    /// <summary>The channel up button.</summary>
    public void ZapNext()
    {
      Step(1);
    }

    /// <summary>The channel down button.</summary>
    public void ZapPrevious()
    {
      Step(-1);
    }

    /// <summary>
    /// The digit "0" on its own, which the reference made zap-back rather than channel nought
    /// (<c>ChannelZapModel.cs:167-170</c>), because going back to where you were is what a person does most with
    /// a remote they have just picked up.
    /// </summary>
    public bool ZapBack()
    {
      TvChannel? back = _previous;
      if (back == null)
      {
        Status = "There is no channel before this one yet.";
        return false;
      }
      return Tune(back);
    }

    /// <summary>
    /// A digit pressed on the remote. They accumulate - "1" then "4" then "8" is somebody typing 148 - and
    /// whoever shows the screen is expected to give them <see cref="ZAP_TIMEOUT_SECONDS"/> seconds of quiet before
    /// calling <see cref="CommitDigits"/>.
    /// </summary>
    public void PressDigit(int digit)
    {
      if (digit < 0 || digit > 9)
        return;

      // A zero at the head of a number is not a leading zero, it is the zap-back key; typed after other digits it
      // is just a nought, which is how 101 is spelled.
      if (digit == 0 && _digits.Length == 0)
      {
        Digits = "0";
        return;
      }

      Digits = _digits + digit.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What the digits mean now that nobody is typing any more. Counted as a position in the bouquet, one-based,
    /// when the viewer left <c>ZapByChannelIndex</c> on - which is its default, because that is what the buttons on
    /// a remote mean to the person pressing them (<c>ChannelContext.cs:190-194</c> renumbered the channels 1..n for
    /// exactly this). With the switch off the reference looked the digits up as the number printed on a channel
    /// instead (<c>ChannelZapModel.cs:173-181</c>), which is what a viewer who types 281 because that is what the
    /// station's own website says wants.
    /// </summary>
    /// <returns>Whether a channel was found for them.</returns>
    public bool CommitDigits()
    {
      string typed = _digits;
      Digits = "";

      if (typed.Length == 0)
        return false;

      if (typed == "0")
        return ZapBack();

      if (!int.TryParse(typed, NumberStyles.None, CultureInfo.InvariantCulture, out int typedNumber))
      {
        Status = "'" + typed + "' is not a channel number.";
        return false;
      }

      if (ZapByChannelIndex)
      {
        // The reference said nothing at all when a number matched nothing, which is the second place it left its
        // viewer guessing. Saying it costs nothing and answers the question they are about to ask.
        if (typedNumber < 1 || typedNumber > Channels.Count)
        {
          Status = "There is no channel number " + typed + " in "
              + (_selectedGroup == null ? Heading : "'" + _selectedGroup.Name + "'") + ", which has "
              + Channels.Count.ToString(CultureInfo.InvariantCulture) + " of them.";
          return false;
        }

        return Tune(Channels[typedNumber - 1]);
      }

      TvChannel? matching = Channels.FirstOrDefault(c => c.Number == typedNumber);
      if (matching == null)
      {
        Status = "No channel in "
            + (_selectedGroup == null ? Heading : "'" + _selectedGroup.Name + "'")
            + " is called " + typed + ".";
        return false;
      }

      return Tune(matching);
    }

    /// <summary>Takes the typed number off the screen without tuning anything, which is what Back does.</summary>
    public void ClearDigits()
    {
      Digits = "";
    }

    /// <summary>
    /// Stops the picture. Asked for rather than done, because the player is not this model's to stop; leaving
    /// this screen asks for the same thing (<see cref="Dispose"/>).
    /// </summary>
    public void Stop()
    {
      Tuned = null;
      Running = null;
      Digits = "";
      Status = "Stopped.";
      StopRequested?.Invoke();
    }

    /// <summary>
    /// What whoever owns the player says back when it did not play the channel. The tuner asked for a picture and
    /// was refused; that answer belongs on the screen that asked, because a viewer standing in front of a black
    /// rectangle is about to ask exactly this question.
    /// </summary>
    public void ReportFailure(string words)
    {
      // The channel stays named on the strip. It is the channel somebody chose, and taking its name away with the
      // failure would leave the screen saying nothing at all about what they pressed.
      Status = words;
    }

    /// <summary>
    /// Forgets that this half of the set is on the screen, without asking anybody to stop - whoever took the
    /// player over has already done that. The reference kept two players and had to stop one to start the other
    /// (<c>TVHandler/SlimTvHandler.cs:213-221</c>); with one player there is nothing to stop, only a tuner left
    /// believing it is still showing something.
    /// </summary>
    public void TakenOver()
    {
      if (_tuned == null)
        return;

      Tuned = null;
      Running = null;
      Status = Heading + " gave the picture to the other one.";
    }

    #endregion

    #region What the backend says

    /// <summary>Reads the bouquets and the channels of the one this screen opens on.</summary>
    public async Task ReloadAsync()
    {
      TvBackendSettings settings = _settingsProvider();
      if (!settings.IsConfigured)
      {
        _marshal(delegate
        {
          Channels.Clear();
          WatchRows.Clear();
          Groups.Clear();
          Status = "No TV backend has been written down yet. Say where the "
              + (_kind == TvMediaType.Radio ? "radio" : "television") + " comes from on the " + Heading
              + " settings, then read this again.";
        });
        return;
      }

      InFlight = LoadChannelsAsync(settings);
      await InFlight.ConfigureAwait(false);
    }

    /// <summary>Steps through the bouquets, which is what green and yellow do.</summary>
    public void StepGroup(int step)
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
    /// Reads the channels of the picked bouquet and then the programmes the list over the picture is made of. In
    /// that order on purpose: the channels are what the zap buttons need, and they should work before a picture
    /// of the evening arrives.
    /// </summary>
    private async Task LoadChannelsAsync(TvBackendSettings? settings = null)
    {
      if (_disposed)
        return;

      Cancel();
      CancellationTokenSource token = new CancellationTokenSource();
      _inFlight = token;
      int mine = ++_generation;

      TvBackendSettings askWith = settings ?? _settingsProvider();
      if (!askWith.IsConfigured)
      {
        Status = "No TV backend has been written down yet.";
        return;
      }

      try
      {
        using (TvHeadendApiClient client = _buildClient(askWith))
        {
          if (_selectedGroup == null)
          {
            IReadOnlyList<TvChannelGroup> groups = await client.GetChannelGroupsAsync(token.Token)
                .ConfigureAwait(false);
            if (mine != _generation)
              return;

            List<TvChannelGroup> ofThisKind = groups
                .Where(g => !LooksWrongKind(g.Name) && !askWith.IsHiddenGroup(g.Name))
                .ToList();

            // Which bouquet to open on is decided from what the backend said rather than from what is on the
            // screen, and decided here rather than when the screen hears about it: the channels of that bouquet are
            // asked for next, and an answer from a machine in another room is still on its own thread at this
            // moment, so waiting for the screen to have been told first would walk on with no bouquet at all.
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
                Status = askWith.Describe() + " answered but offered no channel groups.";
                return;
              }

              Raise(nameof(SelectedGroup));
            });

            if (mine != _generation || picked == null)
              return;
          }

          IReadOnlyList<TvChannel> everyChannel = await client.GetChannelsAsync(token.Token)
              .ConfigureAwait(false);
          if (mine != _generation)
            return;

          // The list, the sentence about it, and the picture the viewer asked for when the settings say to give
          // one all happen together, on the thread the screen is on: a viewer walking in halfway would otherwise
          // find a bouquet with no channels in it, or a screen telling them about a channel it has not shown yet.
          _marshal(delegate
          {
            if (mine != _generation)
              return;

            Channels.Clear();
            foreach (TvChannel channel in OfThisBouquet(everyChannel))
              Channels.Add(channel);

            Status = Channels.Count.ToString(CultureInfo.InvariantCulture) + " channels in "
                + (_selectedGroup == null ? Heading : "'" + _selectedGroup.Name + "'");
            if (askWith.DemoData)
              Status += " (" + DemoTvBackend.NOTICE + ")";

            // "Automatically start TV" - the reference's <c>AutoStartTV</c>, which tuned the channel its navigation
            // stood on the moment its television home opened with nothing else playing
            // (<c>Models/SlimTvClientModel.cs:553-565</c>). That home is this screen here, so a viewer who pressed
            // their way to Live TV gets a picture rather than a dark rectangle and an invitation to zap. Whoever
            // already named a channel - out of a guide, say - wins: this only fires while the screen is still dark.
            if (Tuned == null && Channels.Count > 0 && askWith.AutoStartTV)
              Tune(Channels[0]);
          });

          if (mine != _generation)
            return;

          await FillWatchRowsAsync(client, token.Token, mine).ConfigureAwait(false);
        }
      }
      catch (OperationCanceledException)
      {
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation)
          _marshal(delegate { Status = ex.Message; });
      }
      finally
      {
        if (ReferenceEquals(_inFlight, token))
          token.Dispose();
      }
    }

    /// <summary>
    /// Fills the two programme cells of the list over the picture from one question about the whole bouquet.
    /// Asking now-and-next per channel would be two hundred questions to a machine in another room, and the
    /// reference's list is filled from the same guide data the grid uses for exactly that reason.
    /// </summary>
    private async Task FillWatchRowsAsync(TvHeadendApiClient client, CancellationToken cancellation, int mine)
    {
      TvChannelGroup? group = _selectedGroup;
      if (group == null)
        return;

      DateTimeOffset from = DateTimeOffset.UtcNow;
      IReadOnlyList<TvProgram> programs;
      try
      {
        programs = await client.GetProgramsAsync(group, from, from.AddHours(LOOKAHEAD_HOURS),
            cancellation).ConfigureAwait(false);
      }
      catch (TvBackendException ex)
      {
        // A zap list without programme data is still a zap list, and the rows say what the reference's rows said.
        if (mine == _generation)
          _marshal(delegate { Status = Status + " - and no programme data: " + ex.Message; });
        programs = new List<TvProgram>();
      }

      if (mine != _generation)
        return;

      ILookup<string, TvProgram> byChannel = programs.ToLookup(p => (p.ChannelId ?? "").Trim(),
          StringComparer.OrdinalIgnoreCase);

      _marshal(delegate
      {
        if (mine != _generation)
          return;

        WatchRows.Clear();
        foreach (TvChannel channel in Channels)
        {
          List<TvProgram> here = byChannel[channel.Id].Where(p => p.Stop > from).OrderBy(p => p.Start).ToList();
          TvProgram? now = here.FirstOrDefault(p => p.IsOnAt(from));
          TvProgram? next = here.FirstOrDefault(p => p.Start > from);
          WatchRows.Add(new TvWatchRow(channel, now, next));
        }
        Raise(nameof(WatchRows));
      });
    }

    /// <summary>Asks what the tuned channel is showing, which is what the strip under the picture says.</summary>
    private async Task ReadNowAndNextAsync(TvChannel channel)
    {
      if (_disposed)
        return;

      TvBackendSettings settings = _settingsProvider();
      if (!settings.IsConfigured)
        return;

      Cancel();
      CancellationTokenSource token = new CancellationTokenSource();
      _inFlight = token;
      int mine = ++_generation;

      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          TvNowAndNext on = await client.GetNowAndNextAsync(channel, DateTimeOffset.UtcNow, token.Token)
              .ConfigureAwait(false);
          if (mine != _generation)
            return;

          _marshal(delegate
          {
            if (mine != _generation)
              return;

            Running = on;
            Status = on.HasAnything
                ? ChannelSays + " - " + on.NowTitle
                : ChannelSays + ", with no programme data about it";
          });
        }
      }
      catch (OperationCanceledException)
      {
      }
      catch (TvBackendException ex)
      {
        // The picture is the point of this screen; programme data is a guest. A refused question leaves the strip
        // saying what the reference's strip said when there was nothing to say, and says what was refused.
        if (mine == _generation)
        {
          _marshal(delegate
          {
            Running = new TvNowAndNext(null, null);
            Status = ChannelSays + " - no programme data (" + ex.Message + ")";
          });
        }
      }
    }

    /// <summary>Which of the backend's channels belong to the bouquet on the screen.</summary>
    private List<TvChannel> OfThisBouquet(IReadOnlyList<TvChannel> everyChannel)
    {
      TvChannelGroup? group = _selectedGroup;
      if (group != null)
      {
        List<TvChannel> named = everyChannel.Where(c => c.GroupIds.Contains(group.Id)).ToList();
        if (named.Count > 0)
          return named;
      }

      // A backend that never links its channels to its bouquets still has channels to zap, and an empty zap list
      // is a worse answer than one that is too generous. The guide says out loud when it has to guess this; here
      // the list is beside a working picture, so it simply gets on with it.
      return everyChannel.Where(c => c.MediaType == _kind).ToList();
    }

    /// <summary>Whether a bouquet's name says it belongs to the other half of the set.</summary>
    private bool LooksWrongKind(string name)
    {
      string said = (name ?? "").ToLowerInvariant();
      if (_kind == TvMediaType.Radio)
        return said.Contains("tv", StringComparison.Ordinal) || said.Contains("television", StringComparison.Ordinal)
            || said.Contains("fernsehen", StringComparison.Ordinal);
      return said.Contains("radio", StringComparison.Ordinal) || said.Contains("audio", StringComparison.Ordinal);
    }

    /// <summary>Which bouquet to open on - the same rule the guide follows, for the same reason.</summary>
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

    #endregion

    /// <summary>
    /// Closing this screen stops the television. The reference leaves it running - the picture goes away and the
    /// sound carries on behind the rest of the media centre - which is defensible on a machine whose only job is
    /// being a television and not on one that is about to play somebody's records.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      Cancel();
      if (_tuned != null)
        StopRequested?.Invoke();
    }

    private void Step(int direction)
    {
      if (Channels.Count == 0)
      {
        Status = "There is nothing to zap between yet.";
        return;
      }

      int at = _tuned == null ? -1 : Channels.IndexOf(_tuned);
      int next = at < 0
          ? (direction > 0 ? 0 : Channels.Count - 1)
          : (at + direction + Channels.Count) % Channels.Count;
      Tune(Channels[next]);
    }

    private void Cancel()
    {
      CancellationTokenSource? old = _inFlight;
      _inFlight = null;
      if (old == null)
        return;
      try
      {
        old.Cancel();
      }
      catch (ObjectDisposedException)
      {
      }
    }

    private void Raise(string name)
    {
      PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    /// <summary>Said when the channel, the programme or the typed number changed.</summary>
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
  }
}
