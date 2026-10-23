using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// Which of the backend's three recording tables a schedules screen is looking at.
  /// </summary>
  /// <remarks>
  /// The reference kept these apart by screen rather than by a switch: its schedules screen read the plans out of
  /// the TV service and its recordings screen read the finished ones out of the media library
  /// (<c>TV/SlimTvClient/Skin/Nereus/screens/SlimTvClient-schedulelist.xaml</c> and
  /// <c>RecordingsShowItems.xaml</c>). A TVHeadend keeps both in one table and says which is which on the entry, so
  /// one screen reads the table a viewer asks for. The third table is here because a household that wonders why
  /// something is missing wants the failed and called-off ones too, and no fourth screen would have been built for
  /// that question.
  /// </remarks>
  public sealed class TvTableChoice
  {
    public TvTableChoice(string Word, TvRecordingTable Table)
    {
      this.Word = Word;
      this.Table = Table;
    }

    /// <summary>The word on the switch.</summary>
    public string Word { get; }

    /// <summary>The table it stands for.</summary>
    public TvRecordingTable Table { get; }

    public override string ToString()
    {
      return Word;
    }
  }

  /// <summary>
  /// One recording as the schedules screen draws it.
  /// </summary>
  /// <remarks>
  /// The reference's row was four columns wide - the channel, the name with its series line under it, the two ends
  /// of the time in a column of their own, and a picture saying whether a single programme or a whole series was
  /// asked for (<c>Skin/Nereus/includes/SlimTvClient-schedulelist-*.xaml</c>, a row of fifty pixels with no header
  /// above it). Every one of those four is a string here, and the picture is a word because this front end carries
  /// no recording pictures.
  /// </remarks>
  public sealed class TvScheduleRow
  {
    public TvScheduleRow(TvRecording recording)
    {
      Recording = recording;
    }

    /// <summary>The entry itself, which every question about it is asked with.</summary>
    public TvRecording Recording { get; }

    /// <summary>Its title, with the episode after it when the backend named one.</summary>
    public string Heading
    {
      get { return Recording.Heading; }
    }

    /// <summary>The channel it is on, which the reference drew first because it is what a viewer looks for.</summary>
    public string Channel
    {
      get { return Recording.ChannelName; }
    }

    /// <summary>The two ends of its time.</summary>
    public string TimeRange
    {
      get { return Recording.TimeRange; }
    }

    /// <summary>The day it begins, spelled out, because a plan two Thursdays ahead has to say so.</summary>
    public string Day
    {
      get { return Recording.DayNameLocal; }
    }

    /// <summary>
    /// What the backend is doing about it. Its own state word is not shown as it stands - "conflict" is not
    /// something a viewer reads as a problem - so the states a screen has to distinguish are named in words and
    /// anything else is said as the backend said it.
    /// </summary>
    public string State
    {
      get
      {
        if (Recording.IsRecording)
          return "recording now";
        if (Recording.IsFailed)
          return "did not happen";
        if (Recording.IsCancelled)
          return "called off";
        if (Recording.IsCompleted)
          return "recorded";
        if (Recording.IsWaiting)
          return "waiting";
        return Recording.State;
      }
    }

    /// <summary>Whether a whole series is being kept, which is the reference's series picture in words.</summary>
    public string SeriesMark
    {
      get { return Recording.IsSeries ? "series" : "single"; }
    }

    /// <summary>Whether the backend is writing it this minute.</summary>
    public bool IsRecording
    {
      get { return Recording.IsRecording; }
    }

    /// <summary>Whether it went wrong, which the row says in its own colour.</summary>
    public bool IsFailed
    {
      get { return Recording.IsFailed; }
    }
  }

  /// <summary>
  /// What the household has asked the backend to keep, and what it kept.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is the reference's schedules screen (<c>TV/SlimTvClient/Skin/Nereus/screens/
  /// SlimTvClient-schedulelist.xaml</c>) over one of a TVHeadend's three tables: the plans down the left, the words
  /// about whichever one is stood on in a band at the right, and the series switch above them. Two of the
  /// reference's answers are missing here on purpose and both are written down in the roadmap: the "New schedule"
  /// button, because a manual plan needs a date and an editor a television does not have, and a way of playing a
  /// finished recording, because this front end plays channels rather than files.
  /// </para>
  /// <para>
  /// The series switch is the reference's <c>ScheduleSeriesMode</c> checkbox, and it changes only what "Delete"
  /// means - as it did there, where the same checkbox decided whether a delete was answered by
  /// <c>RemoveScheduleForProgramAsync</c> on one programme or on the series behind it. Standing on a series with the
  /// switch on, a delete first stops the backend renewing it and then takes the entry away; with the switch off it
  /// takes that one entry and leaves the series standing.
  /// </para>
  /// <para>
  /// Everything a viewer does here is answered by the backend in words, and those words are what the line at the
  /// bottom is for. A refusal - "an identical timer already exists" - is an answer rather than a fault, and
  /// <see cref="TvBackendAnswer"/> keeps the two apart, so the screen says the refusal rather than the sentence it
  /// would have invented for it.
  /// </para>
  /// </remarks>
  public sealed class TvSchedulesViewModel : IDisposable, System.ComponentModel.INotifyPropertyChanged
  {
    private readonly Func<TvBackendSettings> _settingsProvider;
    private readonly Func<TvBackendSettings, TvHeadendApiClient> _buildClient;
    private readonly Action<Action> _marshal;
    private readonly Func<bool> _mayDelete;
    private CancellationTokenSource? _inFlight;
    private int _generation;
    private bool _disposed;

    private TvTableChoice? _selectedTable;
    private TvScheduleRow? _selectedRow;
    private bool _seriesMode;
    private string _status = "";
    private bool _isLoading;
    private Task _inFlightWork = Task.CompletedTask;

    /// <summary>
    /// Builds the screen. The settings and the client come in as functions for the same reason the guide's do: a
    /// screen left open while somebody changes the backend's address must not go on asking the machine it stopped
    /// talking to.
    /// </summary>
    /// <param name="finishedFirst">
    /// Whether the screen opens on what has been recorded rather than on what is planned. The home menu has one
    /// tile for each, and the reference had one screen for each, so the two tiles open the same screen turned to
    /// the table each of them means.
    /// </param>
    /// <param name="marshal">
    /// How to get an answer onto the thread the screen lives on, or null to apply it where it arrives. A backend
    /// across the network answers on a thread of its own, and the rows a screen is bound to belong to the thread
    /// that made them, so a screen with controls on it has to hand this over - see <c>Views/ScreenThread.cs</c>.
    /// </param>
    /// <param name="mayDelete">
    /// Whether the person watching may throw a recording away - the reference's
    /// <c>SlimTvClient.DeleteRecording</c> group, answered by the application's viewer state. The press says
    /// what it thinks when the answer is no, and the button stands the answer out in its own words.
    /// </param>
    public TvSchedulesViewModel(bool finishedFirst = false, Func<TvBackendSettings>? settingsProvider = null,
        Func<TvBackendSettings, TvHeadendApiClient>? clientFactory = null, Action<Action>? marshal = null,
        Func<bool>? mayDelete = null)
    {
      _settingsProvider = settingsProvider ?? delegate { return TvBackendSettings.Load(null, out _); };
      _buildClient = clientFactory ?? delegate (TvBackendSettings settings)
      {
        return DemoTvBackend.Open(settings);
      };
      _marshal = marshal ?? delegate (Action action) { action(); };
      _mayDelete = mayDelete ?? delegate { return true; };

      Tables.Add(new TvTableChoice("Planned", TvRecordingTable.Upcoming));
      Tables.Add(new TvTableChoice("Recorded", TvRecordingTable.Finished));
      Tables.Add(new TvTableChoice("Everything", TvRecordingTable.Everything));
      _selectedTable = finishedFirst ? Tables[1] : Tables[0];
    }

    /// <summary>The three tables a viewer can look at, one of them picked.</summary>
    public ObservableCollection<TvTableChoice> Tables { get; } = new ObservableCollection<TvTableChoice>();

    /// <summary>The rows, oldest beginning first.</summary>
    public ObservableCollection<TvScheduleRow> Entries { get; } = new ObservableCollection<TvScheduleRow>();

    /// <summary>Which table is being looked at.</summary>
    public TvTableChoice? SelectedTable
    {
      get { return _selectedTable; }
      set
      {
        if (ReferenceEquals(_selectedTable, value))
          return;

        _selectedTable = value;
        Raise(nameof(SelectedTable));
        _inFlightWork = ReloadAsync();
      }
    }

    /// <summary>The row the words at the right are about.</summary>
    public TvScheduleRow? SelectedRow
    {
      get { return _selectedRow; }
      set
      {
        if (ReferenceEquals(_selectedRow, value))
          return;

        _selectedRow = value;
        Raise(nameof(SelectedRow));
        Raise(nameof(HasSelection));
        Raise(nameof(CanStop));
        Raise(nameof(DetailsHeading));
        Raise(nameof(DetailsChannel));
        Raise(nameof(DetailsTime));
        Raise(nameof(DetailsWords));
        Raise(nameof(DetailsSize));
        Raise(nameof(DetailsSeries));
        Raise(nameof(DetailsProblem));
      }
    }

    /// <summary>
    /// Whether deletes mean whole series. The reference's checkbox, whose label was "Manage series schedules" -
    /// and which is remembered between visits for the same reason it was remembered there.
    /// </summary>
    public bool SeriesMode
    {
      get { return _seriesMode; }
      set
      {
        if (_seriesMode == value)
          return;

        _seriesMode = value;
        Raise(nameof(SeriesMode));
        Raise(nameof(DeleteWord));
      }
    }

    /// <summary>
    /// What the delete button says, which is the question the reference asked separately with a picker of its own
    /// ("How to delete schedule?"). One word here, because the switch above already says which of the two is meant.
    /// </summary>
    public string DeleteWord
    {
      get { return _seriesMode ? "Delete series schedule" : "Delete"; }
    }

    /// <summary>What the screen is called.</summary>
    public string Heading
    {
      get { return _selectedTable != null && _selectedTable.Table == TvRecordingTable.Finished
          ? "Recordings"
          : "Schedules"; }
    }

    /// <summary>Whether the screen is waiting for the backend.</summary>
    public bool IsLoading
    {
      get { return _isLoading; }
      private set
      {
        if (_isLoading == value)
          return;

        _isLoading = value;
        Raise(nameof(IsLoading));
        Raise(nameof(CanAct));
      }
    }

    /// <summary>The backend's own sentence about what was just asked, or the reason nothing can be asked.</summary>
    public string Status
    {
      get { return _status; }
      private set
      {
        if (_status == value)
          return;

        _status = value;
        Raise(nameof(Status));
      }
    }

    /// <summary>Whether anything is stood on.</summary>
    public bool HasSelection
    {
      get { return _selectedRow != null; }
    }

    /// <summary>Whether the row stood on is being written this minute, which is the only case for stopping.</summary>
    public bool CanStop
    {
      get { return _selectedRow != null && _selectedRow.IsRecording; }
    }

    /// <summary>Nothing can be asked while an answer is out; one question at a time, as on the guide.</summary>
    public bool CanAct
    {
      get { return !_isLoading; }
    }

    /// <summary>The title of what is stood on.</summary>
    public string DetailsHeading
    {
      get { return _selectedRow == null ? "" : _selectedRow.Heading; }
    }

    /// <summary>The channel of what is stood on.</summary>
    public string DetailsChannel
    {
      get { return _selectedRow == null ? "" : _selectedRow.Channel; }
    }

    /// <summary>The day and both ends of the time of what is stood on.</summary>
    public string DetailsTime
    {
      get
      {
        TvScheduleRow? row = _selectedRow;
        return row == null ? "" : row.Day + "  " + row.TimeRange;
      }
    }

    /// <summary>What the backend said the programme was about.</summary>
    public string DetailsWords
    {
      get
      {
        TvScheduleRow? row = _selectedRow;
        if (row == null)
          return "";

        return string.IsNullOrWhiteSpace(row.Recording.Description)
            ? "The backend said nothing about this one."
            : row.Recording.Description;
      }
    }

    /// <summary>How big the file got, which only exists once it is written.</summary>
    public string DetailsSize
    {
      get { return _selectedRow == null ? "" : _selectedRow.Recording.Size; }
    }

    /// <summary>Whether a single programme or a whole series stands behind what is picked.</summary>
    public string DetailsSeries
    {
      get
      {
        TvScheduleRow? row = _selectedRow;
        if (row == null)
          return "";

        return row.Recording.IsSeries
            ? "Renewed as a series"
            : "A single programme";
      }
    }

    /// <summary>
    /// The backend's own complaint about the entry, which is the one thing here a viewer cannot find out any other
    /// way: a plan that is switched off or that clashes with another says why in this field and nowhere else.
    /// </summary>
    public string DetailsProblem
    {
      get
      {
        TvScheduleRow? row = _selectedRow;
        if (row == null)
          return "";

        string said = row.Recording.Problem ?? "";
        if (!string.IsNullOrWhiteSpace(said))
          return said;

        return row.Recording.Enabled ? "" : "This plan is switched off.";
      }
    }

    /// <summary>Reads the table over.</summary>
    public Task ReloadAsync()
    {
      _inFlightWork = LoadAsync(_settingsProvider());
      return _inFlightWork;
    }

    /// <summary>
    /// Calls off the writing of what is stood on and leaves the entry where it is - the reference's "Stop current
    /// recording '{title}'", which was the same pair of thoughts: the recording ends, the plan stays readable.
    /// </summary>
    public Task StopAsync()
    {
      return ActAsync(delegate (TvHeadendApiClient client, TvRecording entry)
      {
        return client.StopRecordingAsync(entry);
      }, "Stopped", "stop");
    }

    /// <summary>
    /// Whether the delete press would do anything for the person watching. The button reads it; the model
    /// checks it again when it is answered, because a screen that only hides a thing it also has to refuse
    /// has one rule and not two.
    /// </summary>
    public bool MayDelete
    {
      get { return _mayDelete(); }
    }

    /// <summary>
    /// Takes the entry away, or the series and the entry both when the series switch is on. The file a finished
    /// recording wrote is the backend's to keep or lose, which is what a TVHeadend does with it when its own
    /// retention says so; nothing here deletes from a disk it cannot see.
    /// </summary>
    public Task DeleteAsync()
    {
      if (!_mayDelete())
      {
        Status = "You are not allowed to throw recordings away.";
        return Task.CompletedTask;
      }

      TvScheduleRow? row = SelectedRow;
      if (row == null)
      {
        Status = "Nothing is picked.";
        return Task.CompletedTask;
      }

      if (!SeriesMode || !row.Recording.IsSeries)
      {
        return ActAsync(delegate (TvHeadendApiClient client, TvRecording entry)
        {
          return client.RemoveRecordingAsync(entry);
        }, "Deleted", "delete");
      }

      return ActAsync(async delegate (TvHeadendApiClient client, TvRecording entry)
      {
        // The series first, so that a backend still renewing the event cannot answer the delete by putting the
        // entry back. Both questions are asked of the same answer's client, and the renewal's own refusal is the
        // one that matters: if the series would not let go, the entry is left standing and said.
        TvBackendAnswer series = await client.SetSeriesRenewalAsync(entry, false).ConfigureAwait(false);
        if (!series.Succeeded)
          return series;

        return await client.RemoveRecordingAsync(entry).ConfigureAwait(false);
      }, "Deleted, with the series behind", "delete");
    }

    /// <summary>
    /// Turns the keeping of a series on or off for what is stood on. The reference asked this of the schedule's
    /// type, which it had chosen itself at the time; a TVHeadend's series is the renewal switch on the entry, so
    /// this is the whole of the question.
    /// </summary>
    public Task RenewSeriesAsync()
    {
      TvScheduleRow? row = SelectedRow;
      if (row == null)
      {
        Status = "Nothing is picked.";
        return Task.CompletedTask;
      }

      bool renew = !row.Recording.IsSeries;
      return ActAsync(delegate (TvHeadendApiClient client, TvRecording entry)
      {
        return client.SetSeriesRenewalAsync(entry, renew);
      }, renew ? "Started keeping the series of" : "Stopped keeping the series of",
          renew ? "start keeping the series of" : "stop keeping the series of");
    }

    /// <summary>
    /// Reads one table. Written as the guide's read is: one question at a time, the answer thrown away if a newer
    /// question has been asked meanwhile, and a failure left as a sentence rather than as an exception, because a
    /// television's screen has nothing to do with an exception but to say what happened.
    /// </summary>
    private async Task LoadAsync(TvBackendSettings settings)
    {
      if (_disposed)
        return;

      Cancel();
      CancellationTokenSource token = new CancellationTokenSource();
      _inFlight = token;
      int mine = ++_generation;

      TvTableChoice? table = SelectedTable;
      if (table == null)
        return;

      if (!settings.IsConfigured)
      {
        _marshal(delegate
        {
          Status = "No TV backend has been written down yet.";
          Entries.Clear();
          SelectedRow = null;
        });
        return;
      }

      _marshal(delegate
      {
        IsLoading = true;
        Status = "Reading " + table.Word.ToLowerInvariant() + " recordings...";
      });

      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          IReadOnlyList<TvRecording> found = await client.GetRecordingsAsync(table.Table, token.Token)
              .ConfigureAwait(false);

          if (mine != _generation || _disposed)
            return;

          // The table is filled on the thread the screen made it on, in one go: an answer from a box in another
          // room arrives on a thread of the thread pool, and the rows belong to the screen's own thread.
          _marshal(delegate
          {
            if (mine != _generation || _disposed)
              return;

            TvScheduleRow? wasPicked = SelectedRow;
            Entries.Clear();
            foreach (TvRecording entry in found)
              Entries.Add(new TvScheduleRow(entry));

            // The same entry is stood on again if it is still there, which it usually is - a screen that jumped the
            // selection back to the top every time it read the table over could not be read while a recording ended.
            TvScheduleRow? again = null;
            if (wasPicked != null)
            {
              foreach (TvScheduleRow candidate in Entries)
              {
                if (candidate.Recording.Uuid == wasPicked.Recording.Uuid)
                {
                  again = candidate;
                  break;
                }
              }
            }

            SelectedRow = again ?? (Entries.Count > 0 ? Entries[0] : null);
            Status = Entries.Count == 0
                ? "Nothing is " + (table.Table == TvRecordingTable.Finished ? "recorded" : "planned") + " yet."
                : Entries.Count + (Entries.Count == 1 ? " entry." : " entries.");
          });
        }
      }
      catch (OperationCanceledException)
      {
        // A question taken back because a newer one was asked. Nothing to say.
      }
      catch (TvBackendException ex)
      {
        if (mine == _generation && !_disposed)
          _marshal(delegate { Status = "The backend would not answer: " + ex.Message; });
      }
      catch (Exception ex)
      {
        if (mine == _generation && !_disposed)
          _marshal(delegate { Status = "Reading the recordings went wrong: " + ex.Message; });
      }
      finally
      {
        if (mine == _generation && !_disposed)
        {
          _marshal(delegate
          {
            IsLoading = false;
            Raise(nameof(Heading));
          });
        }

        token.Dispose();
        if (ReferenceEquals(_inFlight, token))
          _inFlight = null;
      }
    }

    /// <summary>
    /// Asks one thing of the entry that is stood on, then reads the table back. The read is not a courtesy: every
    /// one of these questions changes what the backend says about the entry, and a screen that kept its own idea of
    /// the state after asking would be the one place in the house where a recording was still planned after it had
    /// been deleted.
    /// </summary>
    /// <param name="did">
    /// The words for what was asked, once it was done - "Stopped", "Deleted", "Started renewing the series of" -
    /// which are put in front of the entry's name, so a viewer reads which of the rows a sentence is about.
    /// </param>
    /// <param name="dos">
    /// The same words for a backend that refused, which needs them in the other tense. A refusal is passed over in
    /// the backend's own words rather than explained here, because "an identical timer already exists" says more
    /// than "that did not work" ever would.
    /// </param>
    private async Task ActAsync(Func<TvHeadendApiClient, TvRecording, Task<TvBackendAnswer>> ask, string did,
        string dos)
    {
      if (_disposed)
        return;

      TvScheduleRow? row = SelectedRow;
      if (row == null)
      {
        Status = "Nothing is picked.";
        return;
      }

      TvBackendSettings settings = _settingsProvider();
      if (!settings.IsConfigured)
      {
        Status = "No TV backend has been written down yet.";
        return;
      }

      string about = "'" + row.Heading + "'";
      string said;
      IsLoading = true;

      try
      {
        using (TvHeadendApiClient client = _buildClient(settings))
        {
          TvBackendAnswer answer = await ask(client, row.Recording).ConfigureAwait(false);
          said = answer.Succeeded
              ? did + " " + about + "."
              : "The backend would not " + dos + " " + about + ": " + answer.Complaint;
        }
      }
      catch (Exception ex)
      {
        said = "The backend would not answer: " + ex.Message;
      }

      await LoadAsync(settings).ConfigureAwait(false);

      // Said after the read, because the read has a line of its own and the viewer's question was not about it.
      if (!_disposed)
        _marshal(delegate { Status = said; });
    }

    private void Cancel()
    {
      CancellationTokenSource? pending = _inFlight;
      if (pending == null)
        return;

      _inFlight = null;
      pending.Cancel();
    }

    /// <summary>Waits for the question the screen has last asked, which is what a test asks before it looks.</summary>
    public Task LastWork
    {
      get { return _inFlightWork; }
    }

    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      Cancel();
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name)
    {
      PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
  }
}
