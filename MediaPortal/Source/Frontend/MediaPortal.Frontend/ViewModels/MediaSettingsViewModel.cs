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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// What this machine does with the pictures of the library, in the shape every settings screen here uses: a
/// row per answer, a checkbox for the two-answer switch and a button for everything with a list of answers,
/// each row saving in the same breath it changes and saying so in the status line.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why these rows and not a page about the library in general.</b> Every picture on every screen is read
/// from the server, and for a backdrop that is the picture lying next to the media - around 650 KB on an
/// ordinary library, up to two megabytes on a big one. A wall of tiles is tens of megabytes, and a machine
/// that forgets everything at the end of the run pays for all of it at every start. What a viewer can be asked
/// is how much of it this box is allowed to remember, and that is what the rows answer. Nothing here offers a
/// switch the front end does not honour: a row that changed nothing but the file would be a lie with a label.
/// </para>
/// <para>
/// <b>When an answer takes effect.</b> The shelf on the disk answers immediately - it is asked again for the
/// next picture either way. The two memory answers belong to the screens that fetch pictures, and those are
/// built when a screen opens, so they follow from the next shelf the viewer walks into. The status line says
/// which of the two a row is, because "I changed it and nothing happened" is the one thing a settings screen
/// must not leave anybody with.
/// </para>
/// <para>
/// <b>Why the server's picture answers are answered here too.</b> The reference filed everything about a
/// title's pictures under one node of its configuration tree - <c>/Configuration/Settings/Media</c>, which
/// held both "how many of each kind does one title keep" and every extractor's "cache the pictures on a local
/// share" - so the questions a household has about pictures have one place, whatever machine holds the answer.
/// This screen is that place: the rows read from the server when the screen opens and are written back to it
/// when they are answered, and each says in the status line that the server now holds it. Everything else here
/// stays this machine's own and is saved to its own file, as it always was.
/// </para>
/// </remarks>
public partial class MediaSettingsViewModel : ObservableObject
{
  /// <summary>How long the row waits between looks at a pass the server is running.</summary>
  private static readonly TimeSpan BETWEEN_LOOKS = TimeSpan.FromSeconds(2);

  /// <summary>How many times the row looks, so that an open screen stops following a pass eventually.</summary>
  private const int LOOKS_AT_A_PASS = 30;

  private readonly PictureCache? _cache;
  private readonly string? _settingsPath;
  private readonly ServerApiClient? _api;

  /// <summary>
  /// The door to the server's picture answers. The client this screen was handed is that door in the running
  /// application; a caller that hands a stand-in for it names one here.
  /// </summary>
  private readonly IPluginSettingsApi? _settingsApi;

  /// <summary>
  /// What the server last said about the pictures it has ready. Not this machine's answer, which is why the
  /// row says so - the same is true of the rows the server hands out under <see cref="_serverAnswers"/>.
  /// </summary>
  private string _warming = "not asked yet";

  /// <summary>The following of a pass the server is running, if the row pressed one.</summary>
  private CancellationTokenSource? _following;

  /// <summary>The reading of the server's answer that is on its way, for the screen that wants to wait for it.</summary>
  private Task _warmingRead = Task.CompletedTask;

  /// <summary>
  /// What this screen read when it opened, and what its rows show. Replaced by what the file says every time
  /// an answer is written, so the faces and the file cannot drift apart.
  /// </summary>
  private MediaSettings _settings;

  /// <summary>What the shelf was last found to hold, in words, for the row that shows it.</summary>
  private string _kept = "counting";

  /// <summary>The emptying that is on its way, if the row was pressed.</summary>
  private Task _clearing = Task.CompletedTask;

  /// <summary>
  /// The server's own picture answers, by "section/field". Read when the screen opens, and every row of them
  /// writes itself back the way the machine's own rows write theirs.
  /// </summary>
  private readonly Dictionary<string, ServerAnswer> _serverAnswers =
      new Dictionary<string, ServerAnswer>(StringComparer.Ordinal);

  /// <summary>
  /// Which of the server's sections already have their name on the screen. A section is named once, because the
  /// screen is read again every time it opens and a heading added twice is a screen filling up with nothing.
  /// </summary>
  private readonly HashSet<string> _serverHeaders = new HashSet<string>(StringComparer.Ordinal);

  /// <summary>Where the server's rows go: after this machine's answers, before the row that presses the server.</summary>
  private int _serverRowsAt;

  /// <summary>The reading of the server's picture answers that is on its way.</summary>
  private Task _serverRead = Task.CompletedTask;

  /// <summary>
  /// @param cache The shelf this machine is using, so that an answer reaches it without a restart and so that
  /// the row about room can say what is on it. <c>null</c> is a run that only has memory, which shows the same
  /// answers and writes them to the same file for the next run that has a server.
  /// </summary>
  /// <param name="settingsPath">The media answers to show, or <c>null</c> for this machine's own file.</param>
  /// <param name="api">
  /// The server this machine reads its pictures from, so that the answers here that are the <b>server's</b> -
  /// how much it has learned about where its pictures lie, and what it does with the ones lying beside the
  /// files - can be asked and not guessed. <c>null</c> is a run without a server, whose rows say that plainly.
  /// </param>
  /// <param name="settings">
  /// The door the server's picture answers come through, for a caller that hands a stand-in rather than the
  /// whole client. Left out, the client above is that door, because it is one.
  /// </param>
  public MediaSettingsViewModel(PictureCache? cache = null, string? settingsPath = null,
      ServerApiClient? api = null, IPluginSettingsApi? settings = null)
  {
    _cache = cache;
    _settingsPath = settingsPath;
    _api = api;
    _settingsApi = settings ?? api;
    _settings = MediaSettings.Load(settingsPath, out string? problem);
    Status = problem ?? "";

    Options.Add(new SkinSettingOption("Keep pictures on this machine",
        "Whether a picture once fetched is kept, so the next start does not ask for it again",
        () => _settings.CachePictures, value => _settings.CachePictures = value,
        option => Changed(option, file => file.CachePictures = _settings.CachePictures)));

    Options.Add(new SkinSettingOption("Room the kept pictures may take",
        "Past this room the oldest kept pictures go",
        RoomText, AdvanceRoom,
        option => Changed(option, file => file.CacheSizeMb = _settings.CacheSizeMb)));

    Options.Add(new SkinSettingOption("Pictures a screen keeps in mind",
        "Per screen and kind of picture. Applies from the next screen opened",
        MindText, AdvanceMind,
        option => Changed(option, file => file.RememberedPictures = _settings.RememberedPictures)));

    Options.Add(new SkinSettingOption("Pictures on the wire at once",
        "Fewer answer sooner one by one on a slow link, more fill a wall faster. Applies from the next "
        + "screen opened",
        AtOnceText, AdvanceAtOnce,
        option => Changed(option, file => file.PicturesAtOnce = _settings.PicturesAtOnce)));

    Options.Add(new SkinSettingOption("Throw the kept pictures away",
        "Empties the shelf on this machine's disk. Pressing the row is the doing",
        KeptText, delegate { _clearing = ThrowAwayAsync(); }, RefreshOnly));

    // What the server answers about its own pictures goes between this machine's rows and the row that sends
    // the server over its library: the machine's answers first, then the server's, then the doing.
    _serverRowsAt = Options.Count;

    Options.Add(new SkinSettingOption("On the server: pictures it has ready",
        "The server remembers which file holds which title's picture, so the first wall of a new shelf costs "
        + "file opens and no folder walking. It does that by itself after an import; pressing this row sends it "
        + "over its library now. The answer is the server's, not this machine's",
        WarmingText, delegate { _warmingRead = PressWarmingAsync(); }, RefreshOnly));

    _ = ReloadUsageAsync();
    _warmingRead = AskServer(false);
    _serverRead = ReadServerAnswersAsync();
  }

  /// <summary>
  /// The emptying the row started, which is how the screen that wants to wait for its own status line can.
  /// </summary>
  public Task Clearing => _clearing;

  public ObservableCollection<SkinSettingOption> Options { get; } = new ObservableCollection<SkinSettingOption>();

  public string Heading => "Media settings";

  public string Subtitle
  {
    get
    {
      string folder = _settings.CacheFolderPath;
      return "The pictures of the library - posters, banners, backdrops - are read from the server, and a "
          + "backdrop is the picture lying next to the media, so a wall of tiles is tens of megabytes. This "
          + "machine can keep what it has once fetched on its own disk instead of asking again at every "
          + "start; it keeps " + folder + ", and it keeps what the server said about a title that has no "
          + "picture too, because proving that costs the same as fetching. What is answered here belongs to "
          + "this machine and is saved at once; the rows underneath ask the server what it does with its own "
          + "pictures - how many of each kind one title keeps, and whether the ones lying beside the files are "
          + "copied - and those answers are the server's, read when this screen opens and sent back the moment "
          + "they are answered. A shelf that was emptied is felt as a slower next start, never as a missing "
          + "picture.";
    }
  }

  [ObservableProperty]
  private string _status;

  /// <summary>The row the highlight stands on; Enter and the remote's OK work it like its control.</summary>
  [ObservableProperty]
  private SkinSettingOption? _selected;

  public void PressSelected()
  {
    Selected?.Press();
  }

  /// <summary>
  /// What the shelf holds now, read off the disk. The screen starts this when it opens and again after the
  /// shelf was emptied; nobody waits for a folder listing to see a settings screen.
  /// </summary>
  public async Task ReloadUsageAsync()
  {
    try
    {
      PictureCache shelf = _cache ?? new PictureCache(_settingsPath);
      (long bytes, int pictures, int without) = await shelf.MeasureAsync().ConfigureAwait(true);

      _kept = pictures == 0 && without == 0
          ? "nothing yet"
          : string.Format("{0} pictures, {1}{2}", pictures, Megabytes(bytes),
              without == 0 ? "" : string.Format(", and {0} titles without one", without));

      if (_cache != null)
      {
        string? trouble = _cache.Problem;
        if (trouble != null)
          Status = trouble;
      }
    }
    catch (Exception)
    {
      // A shelf that cannot be counted is not worth an exception on a settings screen; the row says what it
      // last knew.
      _kept = "not counted";
    }

    foreach (SkinSettingOption option in Options)
      option.Refresh();
  }

  /// <summary>The room the shelf was given, in the words a person picks by.</summary>
  private string RoomText()
  {
    return SpokenMegabytes(_settings.CacheSizeMb);
  }

  private void AdvanceRoom()
  {
    _settings.CacheSizeMb = Next(MediaSettings.CACHE_SIZES_MB, _settings.CacheSizeMb);
  }

  private string MindText()
  {
    return string.Format("{0} pictures", _settings.RememberedPictures);
  }

  private void AdvanceMind()
  {
    _settings.RememberedPictures = Next(MediaSettings.REMEMBERED_PICTURES, _settings.RememberedPictures);
  }

  private string AtOnceText()
  {
    return _settings.PicturesAtOnce == 1
        ? "one at a time"
        : string.Format("{0} at a time", _settings.PicturesAtOnce);
  }

  private void AdvanceAtOnce()
  {
    _settings.PicturesAtOnce = Next(MediaSettings.PICTURES_AT_ONCE, _settings.PicturesAtOnce);
  }

  private string KeptText()
  {
    return _settings.CachePictures ? _kept : "kept pictures are off";
  }

  /// <summary>
  /// Empties the shelf. The screens that are open keep what they are holding; from the next screen opened the
  /// pictures come from the server again and are kept afresh.
  /// </summary>
  private async Task ThrowAwayAsync()
  {
    PictureCache shelf = _cache ?? new PictureCache(_settingsPath);
    string says = await shelf.ClearAsync().ConfigureAwait(true);
    _kept = "nothing yet";
    Status = says;

    foreach (SkinSettingOption option in Options)
      option.Refresh();
  }

  /// <summary>What the row about the server's pictures says.</summary>
  private string WarmingText()
  {
    return _warming;
  }

  /// <summary>
  /// The asking of the server that is on its way, which is how the screen that wants to wait for its own row can.
  /// </summary>
  public Task WarmingRead => _warmingRead;

  /// <summary>
  /// Pressing the row: telling the server to go over its library and learn where its pictures lie, and then
  /// following the pass while this screen stays open.
  /// </summary>
  /// <remarks>
  /// The following is what keeps the row honest. A pass over a big library takes a minute of the server's time, and
  /// a row that said "the server is going through its library" and never changed again would be a stopped clock -
  /// which on the one answer here the viewer cannot check by looking at anything else is worse than useless.
  /// </remarks>
  private async Task PressWarmingAsync()
  {
    FanArtWarming? said = await AskServer(true).ConfigureAwait(true);
    if (said != null && said.Running)
      await FollowPassAsync().ConfigureAwait(true);
  }

  /// <summary>
  /// Asking the server once. <paramref name="pressed"/> tells the two questions apart: the row's own reading only
  /// looks, while a press starts a pass - or finds one already running, which is an answer and not a failure.
  /// </summary>
  private async Task<FanArtWarming?> AskServer(bool pressed)
  {
    if (_api == null)
    {
      _warming = "no server to ask";
      RefreshRows();
      return null;
    }

    try
    {
      FanArtWarming said = pressed
          ? await _api.WarmFanArtAsync().ConfigureAwait(true)
          : await _api.GetFanArtWarmingAsync().ConfigureAwait(true);

      _warming = said.Describe();
      if (pressed)
      {
        Status = said.Running
            ? "The server is going over its library. This row says how far it got."
            : "The server did not start a second pass - " + said.Describe() + ".";
      }

      RefreshRows();
      return said;
    }
    catch (ServerApiException ex)
    {
      // A server that will not talk about its memory still hands out its pictures, so this is a row without an
      // answer rather than a screen that stopped working. The reason goes into the status line, where a viewer who
      // wants it can read it.
      _warming = "the server would not say";
      if (pressed)
        Status = "The server would not say anything about its pictures: " + ex.Message;
      RefreshRows();
      return null;
    }
  }

  /// <summary>
  /// Looking at a running pass every so often, so that a screen left open fills up instead of sitting on the
  /// sentence it began with. Bounded by count and time: a pass that outlasts them is said again the next time the
  /// screen is opened, which is the only promise this row means to make.
  /// </summary>
  private async Task FollowPassAsync()
  {
    CancellationTokenSource? previous = _following;
    CancellationTokenSource cancellation = new CancellationTokenSource();
    _following = cancellation;
    previous?.Cancel();

    try
    {
      for (int look = 0; look < LOOKS_AT_A_PASS && !cancellation.IsCancellationRequested; look++)
      {
        await Task.Delay(BETWEEN_LOOKS, cancellation.Token).ConfigureAwait(true);

        FanArtWarming? said = await AskServer(false).ConfigureAwait(true);
        if (said == null || !said.Running)
          return;
      }
    }
    catch (OperationCanceledException)
    {
      // A newer press took over, or the screen went away.
    }
    finally
    {
      // The source belongs to this run alone, and taking the field away first is what keeps a newer run from
      // being cancelled by this one's cleanup.
      if (ReferenceEquals(_following, cancellation))
        _following = null;
      cancellation.Dispose();
    }
  }

  /// <summary>Giving every row's face back to the answers behind it.</summary>
  private void RefreshRows()
  {
    foreach (SkinSettingOption option in Options)
      option.Refresh();
  }

  /// <summary>
  /// The reading of the server's picture answers that the screen started when it opened, so that a test - or a
  /// screen that wants to speak after them - can wait for them.
  /// </summary>
  public Task ServerAnswersRead => _serverRead;

  /// <summary>
  /// Asking the server what it does with the library's pictures and putting one row per answer in front of the
  /// viewer. Asked again every time the screen opens, because the answers are the server's and another
  /// television may have answered differently since.
  /// </summary>
  public async Task ReadServerAnswersAsync()
  {
    IPluginSettingsApi? settings = _settingsApi;
    if (settings == null)
    {
      // A run with no server has no server answers to show and says nothing about them; the machine's own rows
      // are still there and still true.
      return;
    }

    try
    {
      SettingSectionsAnswer sections =
          await settings.GetSettingSectionsAsync(SettingSectionAreas.Media).ConfigureAwait(true);

      foreach (SettingSectionSummary summary in sections.Sections)
      {
        SettingSectionState state = await settings.GetSettingSectionAsync(summary.Key).ConfigureAwait(true);
        AddServerHeader(summary.Key, summary.Title);
        foreach (SettingFieldState field in state.Fields)
          AddServerAnswer(state.Key, field);
      }
    }
    catch (ServerApiException ex)
    {
      // A server that will not talk about its pictures still hands them out, so this is a screen with fewer
      // rows rather than a broken one, and the reason goes where a viewer who wants it can read it.
      Status = "The server would not say what it does with its pictures: " + ex.Message;
    }
    catch (Exception ex)
    {
      Status = "The server's picture answers could not be read: " + ex.Message;
    }
  }

  /// <summary>
  /// The name of one of the server's sections, as a row of its own. The server answers its settings in sections
  /// - what the film reader takes, what the series reader takes, how many pictures a title keeps - and on a
  /// screen that shows all of them at once a row like "The actors of a film" only reads next to the heading that
  /// says whose reader it is.
  /// </summary>
  private void AddServerHeader(string sectionKey, string title)
  {
    if (!_serverHeaders.Add(sectionKey))
      return;
    Options.Insert(_serverRowsAt++, SkinSettingOption.Headed(title));
  }

  /// <summary>
  /// One server answer as a row of this screen, put where the server's rows go. A field this screen already
  /// wears a row for is worn again with the new value rather than added twice, which is what lets the screen be
  /// read again without filling up.
  /// </summary>
  private void AddServerAnswer(string sectionKey, SettingFieldState field)
  {
    string id = sectionKey + "/" + field.Key;
    if (_serverAnswers.TryGetValue(id, out ServerAnswer known))
    {
      known.Wear(field);
      return;
    }

    ServerAnswer answer = new ServerAnswer(sectionKey, field);
    SkinSettingOption row;
    if (field.Kind == SettingSectionViewModelKinds.Toggle)
      row = new SkinSettingOption(field.Label, field.Help, () => answer.Flag, value => answer.Flag = value,
          option => _ = WriteServerAnswerAsync(answer));
    else if (field.Kind == SettingSectionViewModelKinds.Number)
      row = new SkinSettingOption(field.Label, field.Help, () => answer.Number.ToString(),
          () => answer.Number = NextNumber(answer, answer.Number),
          option => _ = WriteServerAnswerAsync(answer));
    else if (field.Kind == SettingSectionViewModelKinds.Choice)
      row = new SkinSettingOption(field.Label, field.Help, () => answer.Words,
          () => answer.Words = NextName(answer, answer.Words), option => _ = WriteServerAnswerAsync(answer));
    else
      // A word the viewer types, which is what a row for an answer that is a word has to wear.
      row = SkinSettingOption.Written(field.Label, field.Help, () => answer.Words,
          value => answer.Words = value, option => _ = WriteServerAnswerAsync(answer));

    answer.Row = row;
    _serverAnswers[id] = answer;
    Options.Insert(_serverRowsAt++, row);
  }

  /// <summary>
  /// Sending one answered row to the server. The server stores it and answers with the whole section, and every
  /// row of that section is set to what the server says back - which is also how an answer the server could not
  /// take comes off the face again instead of sitting there looking stored.
  /// </summary>
  private async Task WriteServerAnswerAsync(ServerAnswer answer)
  {
    object? outgoing = answer.Outgoing;
    IPluginSettingsApi? settings = _settingsApi;
    if (settings == null)
    {
      Status = "There is no server to take that answer.";
      return;
    }
    if (outgoing == null)
    {
      Status = string.Format("{0} has no answer to send.", answer.Row?.Label ?? answer.Field.Label);
      return;
    }

    try
    {
      SettingSectionState state = await settings.PutSettingSectionAsync(answer.SectionKey,
          new SettingSectionWish
          {
            Values = new Dictionary<string, object> { [answer.Field.Key] = outgoing }
          }).ConfigureAwait(true);

      foreach (SettingFieldState field in state.Fields)
      {
        if (_serverAnswers.TryGetValue(state.Key + "/" + field.Key, out ServerAnswer row))
          row.Wear(field);
      }

      string says = string.Format("{0} is now {1} on the server.", answer.Row?.Label ?? answer.Field.Label,
          answer.Row?.ValueText ?? "");
      Status = state.RestartNeededFor.Contains(answer.Field.Key, StringComparer.Ordinal)
          ? says + " The server will act on it when it next starts."
          : says;
    }
    catch (ServerApiException ex)
    {
      Status = "The server would not take that: " + ex.Message;
      await ReadServerAnswersAsync().ConfigureAwait(true);
    }
  }

  /// <summary>
  /// The next number a row walks to, inside the bounds the server gave the field. Walking rather than typing,
  /// because these are small counts and a remote has no keyboard behind it.
  /// </summary>
  private static int NextNumber(ServerAnswer answer, int current)
  {
    int low = answer.Field.Min ?? 0;
    int high = answer.Field.Max ?? 0;
    if (high <= low)
      return current;

    int next = current + 1;
    return next > high || next < low ? low : next;
  }

  /// <summary>The next name a choice offers, wrapping around.</summary>
  private static string NextName(ServerAnswer answer, string current)
  {
    IReadOnlyList<string> names = answer.Field.Options;
    if (names.Count == 0)
      return current;

    for (int i = 0; i < names.Count; i++)
    {
      if (string.Equals(names[i], current, StringComparison.Ordinal))
        return names[(i + 1) % names.Count];
    }
    return names[0];
  }

  /// <summary>
  /// What every row does after its answer changed: write that one answer, tell the shelf, say it.
  /// </summary>
  /// <param name="option">The row that was answered, whose face is refreshed and named in the status line.</param>
  /// <param name="answer">Writing this row's answer onto the settings that were just read from the file.</param>
  private void Changed(SkinSettingOption option, Action<MediaSettings> answer)
  {
    // Written into a fresh read of the file rather than into what this screen read when it opened: the trail
    // keeps a screen the viewer walked back over, and while an older copy of this screen is still alive a
    // whole file written from it would quietly set back whatever a newer one answered. One answer put into
    // what the file says now cannot do that.
    MediaSettings file = MediaSettings.Load(_settingsPath, out _);
    answer(file);
    string says = file.Save(_settingsPath, out bool saved);
    _settings = file;

    foreach (SkinSettingOption row in Options)
      row.Refresh();

    _cache?.Reload();

    Status = saved
        ? string.Format("{0} is now {1}. {2}", option.Label, option.ValueText, says)
        : string.Format("{0} is now {1} for this session; saving failed - {2}",
            option.Label, option.ValueText, says);
  }

  /// <summary>
  /// What the doing's row does: nothing was answered, so nothing is saved and nothing is said yet - the doing
  /// itself has the last word, in the sentence it writes when it is through.
  /// </summary>
  private void RefreshOnly(SkinSettingOption option)
  {
    option.Refresh();
  }

  private static int Next(int[] answers, int current)
  {
    for (int i = 0; i < answers.Length; i++)
      if (answers[i] == current)
        return answers[(i + 1) % answers.Length];

    return answers[0];
  }

  /// <summary>A room in megabytes as a person says it: 512 MB, 2 GB.</summary>
  private static string SpokenMegabytes(int megabytes)
  {
    return megabytes < 1024
        ? string.Format("{0} MB", megabytes)
        : string.Format("{0} GB", megabytes / 1024);
  }

  /// <summary>A number of bytes the way the row reads it: 1,4 GB rather than 1468006400.</summary>
  private static string Megabytes(long bytes)
  {
    if (bytes < 1024)
      return string.Format("{0} bytes", bytes);

    if (bytes < 1024L * 1024L)
      return string.Format("{0:0.#} KB", bytes / 1024.0);

    if (bytes < 1024L * 1024L * 1024L)
      return string.Format("{0:0.#} MB", bytes / (1024.0 * 1024.0));

    return string.Format("{0:0.#} GB", bytes / (1024.0 * 1024.0 * 1024.0));
  }

  /// <summary>
  /// One answer the server holds, with the row that shows it.
  /// </summary>
  /// <remarks>
  /// The value is kept here between the press and the server's answer, so that the face of the row is right in
  /// the same moment it is worked - the way every other row on this screen works - and is set straight by
  /// whatever the server says back. Nothing here knows what the answer means; the server named the field, its
  /// kind and its bounds, and this only carries what it said.
  /// </remarks>
  private sealed class ServerAnswer
  {
    private object? _answer;

    public ServerAnswer(string sectionKey, SettingFieldState field)
    {
      SectionKey = sectionKey;
      Field = field;
      _answer = Read(field);
    }

    /// <summary>The section the answer is stored in, and the one the row writes back to.</summary>
    public string SectionKey { get; }

    /// <summary>What the server last said about this field, bounds and all.</summary>
    public SettingFieldState Field { get; private set; }

    /// <summary>The row wearing this answer, once it has been built.</summary>
    public SkinSettingOption? Row { get; set; }

    public bool Flag
    {
      get { return _answer as bool? ?? false; }
      set { _answer = value; }
    }

    public int Number
    {
      get { return _answer as int? ?? 0; }
      set { _answer = value; }
    }

    public string Words
    {
      get { return _answer as string ?? ""; }
      set { _answer = value; }
    }

    /// <summary>What to send the server: the answer the row is wearing.</summary>
    public object? Outgoing => _answer;

    /// <summary>What the server says now. The row's face follows it.</summary>
    public void Wear(SettingFieldState field)
    {
      Field = field;
      _answer = Read(field);
      Row?.Refresh();
    }

    private static object? Read(SettingFieldState field)
    {
      switch (field.Value.ValueKind)
      {
        case JsonValueKind.True:
          return true;
        case JsonValueKind.False:
          return false;
        case JsonValueKind.Number:
          return field.Value.TryGetInt32(out int number) ? number : 0;
        case JsonValueKind.String:
          return field.Value.GetString();
        default:
          return null;
      }
    }
  }
}
