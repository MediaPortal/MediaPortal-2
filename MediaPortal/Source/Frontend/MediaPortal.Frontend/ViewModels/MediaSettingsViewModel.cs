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
using System.Collections.ObjectModel;
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
  /// What the server last said about the pictures it has ready. The one answer on this screen that is not this
  /// machine's, which is why the row says so.
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
  /// @param cache The shelf this machine is using, so that an answer reaches it without a restart and so that
  /// the row about room can say what is on it. <c>null</c> is a run that only has memory, which shows the same
  /// answers and writes them to the same file for the next run that has a server.
  /// </summary>
  /// <param name="settingsPath">The media answers to show, or <c>null</c> for this machine's own file.</param>
  /// <param name="api">
  /// The server this machine reads its pictures from, so that the one answer here that is the <b>server's</b> -
  /// how much it has learned about where its pictures lie - can be asked and not guessed. <c>null</c> is a run
  /// without a server, whose row says that plainly.
  /// </param>
  public MediaSettingsViewModel(PictureCache? cache = null, string? settingsPath = null, ServerApiClient? api = null)
  {
    _cache = cache;
    _settingsPath = settingsPath;
    _api = api;
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

    Options.Add(new SkinSettingOption("On the server: pictures it has ready",
        "The server remembers which file holds which title's picture, so the first wall of a new shelf costs "
        + "file opens and no folder walking. It does that by itself after an import; pressing this row sends it "
        + "over its library now. The answer is the server's, not this machine's",
        WarmingText, delegate { _warmingRead = PressWarmingAsync(); }, RefreshOnly));

    _ = ReloadUsageAsync();
    _warmingRead = AskServer(false);
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
          + "picture too, because proving that costs the same as fetching. Everything here is answered for "
          + "this machine alone and is saved at once, except the one row that says what the server has "
          + "learned - that answer is the server's and is marked as such. A shelf that was emptied is felt as "
          + "a slower next start, never as a missing picture.";
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
}
