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
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// The folders the library is built from, and the import that fills it.
/// </summary>
/// <remarks>
/// <para>
/// This is the screen without which a media centre is a database nobody can feed. Until the server's API grew a
/// shares endpoint the only way to put a folder into MP2's library was the configuration tool's grid, a
/// Windows-only editor, so a fresh install on any other platform could serve a library but never gain one.
/// Everything here is what the HTTP API offers: list, register, change, forget, re-import, and the importer's
/// queue.
/// </para>
/// <para>
/// <b>A path is the server's, not the viewer's.</b> The front end is often on the television and the server in the
/// cupboard, so what is typed here is a path on the <i>server's</i> filesystem: a Windows server wants
/// <c>D:\Films</c>, a Linux one <c>/srv/films</c>. Nothing here validates it - a client cannot know what a server
/// three rooms away can see - and the server's own refusal, with its reason, is what the status line says.
/// </para>
/// <para>
/// Deleting asks twice. Forgetting a folder also drops the titles that came out of it, which is the one thing on
/// this screen that a wrong press cannot walk back; a confirmation dialog would need keyboard focus to behave on a
/// television, so the second press of the same button is the confirmation, and the row says so in between.
/// </para>
/// <para>
/// The awaits keep their context, as everywhere else in this front end: what follows them writes properties a
/// bound screen is listening to.
/// </para>
/// </remarks>
public partial class FoldersViewModel : ObservableObject, IDisposable
{
  /// <summary>How long the queue is followed after an import was asked for, and how often it is asked.</summary>
  private const int POLL_LIMIT = 40;
  private const int POLL_DELAY_MS = 1500;

  /// <summary>
  /// How often the queue is asked. A person watching a folder of four thousand files being read does not need a
  /// faster answer than this, and a server that is importing has better things to do than answer every second.
  /// Internal so that the test suite, whose queue is scripted to drain on the second read, does not have to wait a
  /// and-a-half seconds per test to find that out.
  /// </summary>
  internal int PollDelayMilliseconds { get; set; } = POLL_DELAY_MS;

  private readonly ServerApiClient? _client;
  private CancellationTokenSource? _running;
  private CancellationTokenSource? _watching;
  private bool _queueWasBusy;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanAdd))]
  [NotifyCanExecuteChangedFor(nameof(AddCommand))]
  private string _newPath = string.Empty;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AddCommand))]
  [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
  [NotifyCanExecuteChangedFor(nameof(ReimportCommand))]
  [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
  private bool _isBusy;

  [ObservableProperty]
  private string _status = "Not read yet.";

  [ObservableProperty]
  private string _queue = string.Empty;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanRemove))]
  [NotifyPropertyChangedFor(nameof(CanReimport))]
  [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
  [NotifyCanExecuteChangedFor(nameof(ReimportCommand))]
  private FolderShare? _selected;

  /// <summary>
  /// The queue watcher of the last "add" or "re-import", for the tests and for nothing else. Watching is
  /// deliberately not part of the command's own task - a screen must not sit disabled for as long as an import
  /// takes - which means a test needs the handle to know when the watching is done.
  /// </summary>
  internal Task? QueueWatch { get; private set; }

  /// <param name="client">The server whose folders to show, or null for a screen that says it has none.</param>
  public FoldersViewModel(ServerApiClient? client = null)
  {
    _client = client;
  }

  /// <summary>The folders, in the order the server named them.</summary>
  public ObservableCollection<FolderShare> Shares { get; } = new ObservableCollection<FolderShare>();

  /// <summary>
  /// The kinds of media this server can import, each with its tick box. They come from the server rather than from
  /// a list written here, because they are what its extractors registered themselves under; a server with no video
  /// plugin has no reason to offer "Movies".
  /// </summary>
  public ObservableCollection<CategoryChoice> Categories { get; } = new ObservableCollection<CategoryChoice>();

  /// <summary>Whether the list has anything in it.</summary>
  public bool HasShares => Shares.Count > 0;

  /// <summary>How many categories are ticked for the folder being added.</summary>
  public int ChosenCount => Categories.Count(category => category.IsOn);

  /// <summary>
  /// The line above the list. "No folders" and "not read yet" are different sentences on purpose: the first is a
  /// media centre that has never been told where anything is, the second is a screen that has not asked.
  /// </summary>
  public string Heading
  {
    get
    {
      if (_client == null)
        return "No server connected.";
      return Shares.Count == 0
          ? "No folders yet - the library stays empty until one is added below"
          : Shares.Count == 1
              ? "1 folder feeds this library:"
              : Shares.Count + " folders feed this library:";
    }
  }

  /// <summary>Whether a folder can be added from what is on screen.</summary>
  public bool CanAdd => _client != null && !IsBusy && !string.IsNullOrWhiteSpace(NewPath);

  private bool CanRemove => _client != null && !IsBusy && Selected != null;

  private bool CanReimport => _client != null && !IsBusy && Selected != null;

  private bool CanSave => _client != null && !IsBusy && Selected != null;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
  private string _editName = string.Empty;

  [ObservableProperty]
  private string _editPath = string.Empty;

  [ObservableProperty]
  private bool _editWatcher = true;

  /// <summary>
  /// The tick boxes of the editor: the server's categories, the selected folder's own ones ticked. A category
  /// the folder carries but the server no longer offers is listed too and stays ticked - a name from an older
  /// server is the folder's own history, not this screen's to erase silently.
  /// </summary>
  public ObservableCollection<CategoryChoice> EditCategories { get; } = new ObservableCollection<CategoryChoice>();

  /// <summary>
  /// Reads the folders and the importer's queue again.
  /// </summary>
  [RelayCommand]
  public async Task RefreshAsync()
  {
    if (_client == null)
    {
      Status = "No server connected. Pick one on the Server screen.";
      return;
    }

    CancellationTokenSource cancellation = new CancellationTokenSource();
    CancellationTokenSource? previous = _running;
    _running = cancellation;
    previous?.Cancel();
    IsBusy = true;
    try
    {
      SharesAnswer answer = await _client.GetSharesAsync(cancellation.Token);
      Fill(answer);
      await ReadQueueAsync(cancellation.Token);
      Status = answer.Count == 0
          ? "This server has no folders to import yet."
          : answer.Count + (answer.Count == 1 ? " folder on " : " folders on ") + _client.BaseUrl.Host;
    }
    catch (ServerApiException ex)
    {
      Status = ex.NotReady
          ? "The server cannot answer about folders yet: " + ex.Message
          : ex.Message;
    }
    catch (OperationCanceledException)
    {
      // Replaced by a newer read, which reports its own status.
    }
    catch (Exception ex)
    {
      // A command that throws past here dies on an async void continuation, and what dies with it is the front
      // end. Everything the viewer needs is a sentence.
      Status = "Reading the folders failed: " + ex.Message;
    }
    finally
    {
      if (ReferenceEquals(_running, cancellation))
      {
        _running = null;
        IsBusy = false;
      }
      cancellation.Dispose();
    }
  }

  /// <summary>
  /// Registers the typed folder. Registering a folder on the server's own machine queues its import by itself,
  /// which is why this does not ask for one as well - and why the queue is watched afterwards.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanAdd))]
  public async Task AddAsync()
  {
    if (_client == null)
      return;

    List<string> chosen = Categories.Where(category => category.IsOn)
        .Select(category => category.Name)
        .ToList();
    if (chosen.Count == 0)
    {
      // Refused here rather than left to the server, which would take the folder under every category it knows:
      // a music folder read by the video extractors turns into noise in the film list, and the boxes are right
      // there to tick.
      Status = "Say what kind of media is in that folder - tick at least one of the choices.";
      return;
    }

    string path = NewPath.Trim();
    try
    {
      IsBusy = true;
      ShareInfo added = await _client.AddShareAsync(path, chosen);
      Status = string.Format("Added \"{0}\" as {1}.", added.Name ?? path, string.Join(", ", chosen));
      NewPath = string.Empty;
      Fill(await _client.GetSharesAsync());
      WatchQueue();
    }
    catch (ServerApiException ex)
    {
      Status = DescribeAddFailure(ex);
    }
    catch (Exception ex)
    {
      Status = "Adding that folder failed: " + ex.Message;
    }
    finally
    {
      IsBusy = false;
    }
  }

  /// <summary>
  /// Forgets the selected folder and drops the titles that came out of it. Pressing twice on the same row is the
  /// confirmation.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanRemove))]
  public async Task RemoveAsync()
  {
    FolderShare? row = Selected;
    if (_client == null || row == null)
      return;

    if (!row.AskedToRemove)
    {
      foreach (FolderShare other in Shares)
        other.Confirmation = null;
      row.Confirmation = "Press remove again to forget this folder and every title imported from it";
      Status = row.Confirmation + ".";
      return;
    }

    try
    {
      IsBusy = true;
      await _client.RemoveShareAsync(row.Id);
      Status = string.Format("\"{0}\" is no longer imported.", row.Name);
      Fill(await _client.GetSharesAsync());
    }
    catch (ServerApiException ex)
    {
      Status = ex.StatusCode == 404
          ? "That folder was already gone from the server."
          : ex.Message;
    }
    catch (Exception ex)
    {
      Status = "Forgetting that folder failed: " + ex.Message;
    }
    finally
    {
      IsBusy = false;
    }
  }

  /// <summary>
  /// Queues a fresh walk of the selected folder - what to press after files were added to it and the watcher did
  /// not notice.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanReimport))]
  public async Task ReimportAsync()
  {
    FolderShare? row = Selected;
    if (_client == null || row == null)
      return;

    try
    {
      IsBusy = true;
      ImportStatus after = await _client.ImportShareAsync(row.Id);
      Show(after);
      Status = string.Format("Re-importing \"{0}\".", row.Name);
      WatchQueue();
    }
    catch (ServerApiException ex)
    {
      Status = ex.StatusCode == 404
          ? "The server no longer knows that folder."
          : ex.Message;
    }
    catch (Exception ex)
    {
      Status = "Asking for that import failed: " + ex.Message;
    }
    finally
    {
      IsBusy = false;
    }
  }

  /// <summary>
  /// The editor follows the selection: name, path, watcher and the category boxes show the folder that is
  /// picked, so what "change" saves is what the viewer was just looking at.
  /// </summary>
  partial void OnSelectedChanged(FolderShare? value)
  {
    EditName = value?.Name ?? string.Empty;
    EditPath = value?.Path ?? string.Empty;

    EditCategories.Clear();
    if (value == null)
    {
      EditWatcher = true;
      return;
    }

    EditWatcher = value.Share.UseShareWatcher;

    string[] carried = value.Share.MediaCategories?.ToArray() ?? Array.Empty<string>();
    foreach (CategoryChoice known in Categories)
    {
      CategoryChoice box = new CategoryChoice(known.Name,
          carried.Contains(known.Name, StringComparer.OrdinalIgnoreCase));
      EditCategories.Add(box);
    }

    foreach (string carriedName in carried)
    {
      if (!EditCategories.Any(box => string.Equals(box.Name, carriedName, StringComparison.OrdinalIgnoreCase)))
        EditCategories.Add(new CategoryChoice(carriedName, true));
    }
  }

  /// <summary>
  /// Sends the editor's name, categories and watcher (and the path, if it was edited because the folder moved)
  /// to the server.
  /// </summary>
  /// <remarks>
  /// The path is only sent when it actually differs from the row's own - an unchanged path typed into the box
  /// must not read as "this folder moved" and relocate the share's items to where they already are.
  /// </remarks>
  [RelayCommand(CanExecute = nameof(CanSave))]
  public async Task SaveChangesAsync()
  {
    FolderShare? row = Selected;
    if (_client == null || row == null)
      return;

    List<string> chosen = EditCategories.Where(box => box.IsOn)
        .Select(box => box.Name)
        .ToList();
    if (chosen.Count == 0)
    {
      // A share without categories is read by nobody and imports nothing - the server would take it, and the
      // empty shelf would only explain itself weeks later.
      Status = "Leave at least one kind of media ticked - a folder without categories is imported by nobody.";
      return;
    }

    string? movedPath = null;
    if (!string.IsNullOrWhiteSpace(EditPath)
        && !string.Equals(EditPath.Trim(), row.Path, StringComparison.Ordinal))
      movedPath = EditPath.Trim();

    try
    {
      IsBusy = true;
      ShareInfo updated = await _client.UpdateShareAsync(row.Id, EditName, chosen, EditWatcher, movedPath);
      Status = movedPath == null
          ? string.Format("\"{0}\" is changed.", updated.Name ?? row.Name)
          : string.Format("\"{0}\" now reads {1}. What moved into the old place stays until it is read again.",
              updated.Name ?? EditName, movedPath);
      Fill(await _client.GetSharesAsync());
    }
    catch (ServerApiException ex)
    {
      Status = ex.StatusCode switch
      {
        400 => "The server will not take those changes. " + ex.Message,
        404 => "The server no longer knows that folder.",
        409 => ex.Message,
        _ => ex.Message
      };
    }
    catch (Exception ex)
    {
      Status = "Changing that folder failed: " + ex.Message;
    }
    finally
    {
      IsBusy = false;
    }
  }

  /// <summary>
  /// The server's refusal of a folder, in words a viewer can act on. Only the refusals that can be fixed from this
  /// screen are translated, and a server that is not listening is described as that; everything else is the
  /// server's own sentence, which is already specific.
  /// </summary>
  public static string DescribeAddFailure(ServerApiException failure)
  {
    if (failure.StatusCode == 409)
      return "That folder is already in the library. " + failure.Message;
    if (failure.StatusCode == 400)
      return "The server will not take that folder. " + failure.Message;
    if (failure.NotReady)
      // Saying "503" on a television is saying nothing. What the viewer can do about it is wait and press again.
      return "The server is not ready to take folders. Try again once it has finished starting up.";
    return failure.Message;
  }

  /// <summary>
  /// Puts an answer on screen: the rows, the selection, the categories.
  /// </summary>
  private void Fill(SharesAnswer answer)
  {
    Shares.Clear();
    foreach (ShareInfo share in answer.Shares ?? Array.Empty<ShareInfo>())
      Shares.Add(new FolderShare(share));
    // Whatever was picked stays picked, because a refresh that jumps the selection to the top of the list makes
    // the second press of "remove" delete a different folder than the one that was named.
    if (Selected == null || !Shares.Any(row => row.Id == Selected.Id))
      Selected = Shares.FirstOrDefault();

    SyncCategories(answer.MediaCategories);
    OnPropertyChanged(nameof(HasShares));
    OnPropertyChanged(nameof(Heading));
  }

  /// <summary>
  /// Offers the server's categories, keeping what the viewer had ticked. They are only rebuilt when the list
  /// really changed, because rebuilding on every read would clear the ticks someone had just set.
  /// </summary>
  private void SyncCategories(IReadOnlyList<string>? names)
  {
    string[] wanted = (names ?? Array.Empty<string>())
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToArray();
    if (Categories.Count == wanted.Length
        && Categories.Select(category => category.Name).SequenceEqual(wanted, StringComparer.Ordinal))
      return;

    string[] wasOn = Categories.Where(category => category.IsOn).Select(category => category.Name).ToArray();
    Categories.Clear();
    foreach (string name in wanted)
    {
      CategoryChoice choice = new CategoryChoice(name,
          wasOn.Contains(name, StringComparer.OrdinalIgnoreCase));
      choice.PropertyChanged += delegate { OnPropertyChanged(nameof(ChosenCount)); };
      Categories.Add(choice);
    }
    OnPropertyChanged(nameof(ChosenCount));
  }

  private async Task ReadQueueAsync(CancellationToken cancellation)
  {
    if (_client == null)
      return;
    try
    {
      Show(await _client.GetImportStatusAsync(cancellation));
    }
    catch (ServerApiException)
    {
      // The folders are what the screen is for. A queue the server would not describe leaves this line empty
      // rather than hiding the list that did arrive.
      Queue = string.Empty;
    }
  }

  /// <summary>
  /// Telling the server to learn where the pictures of what was just imported lie.
  /// </summary>
  /// <remarks>
  /// Quiet when the server will not say. The pictures arrive either way - an unlearned one costs the server a walk
  /// through its folders the first time somebody asks - and a screen that has just told the viewer their folder is
  /// imported should not make the server's memory the loud complaint on it. What the pass is doing is on the media
  /// answers screen, where the pictures are configured.
  /// </remarks>
  private async Task WarmPicturesAsync(CancellationToken cancellation)
  {
    try
    {
      await _client!.WarmFanArtAsync(cancellation);
    }
    catch (ServerApiException)
    {
    }
    catch (OperationCanceledException)
    {
    }
  }

  /// <summary>
  /// Follows the queue until it empties, so that "importing" on the screen means what it means on the server
  /// instead of being a word that never changes.
  /// </summary>
  /// <summary>
  /// Puts the importer's queue on screen and remembers that it had work in it.
  /// </summary>
  /// <remarks>
  /// The remembering is what keeps the finishing line honest: a queue that was never seen busy has not finished
  /// anything, and the sentence it would overwrite is the one that tells the viewer what their button did.
  /// </remarks>
  private void Show(ImportStatus status)
  {
    Queue = status.Describe();
    if (status.Pending > 0)
      _queueWasBusy = true;
  }

  private void WatchQueue()
  {
    CancellationTokenSource? previous = _watching;
    CancellationTokenSource cancellation = new CancellationTokenSource();
    _watching = cancellation;
    previous?.Cancel();
    QueueWatch = WatchAsync(cancellation);
  }

  /// <remarks>
  /// The cancellation source is handed over rather than its token, so that the run that owns it disposes it and
  /// only ever its own: a source disposed while another run is still between awaits turns that run's next
  /// <c>Token</c> into an <c>ObjectDisposedException</c>, which <see cref="HomeViewModel"/> measured the hard way.
  /// </remarks>
  private async Task WatchAsync(CancellationTokenSource cancellation)
  {
    if (_client == null)
    {
      cancellation.Dispose();
      return;
    }

    try
    {
      for (int attempt = 0; attempt < POLL_LIMIT && !cancellation.IsCancellationRequested; attempt++)
      {
        ImportStatus status = await _client.GetImportStatusAsync(cancellation.Token);
        Show(status);
        if (status.Pending == 0)
        {
          // Only said when the screen watched something being done. A queue that was empty the whole time would
          // otherwise erase the sentence about the folder that was just added, which is the sentence the viewer
          // pressed the button for.
          if (_queueWasBusy)
          {
            Status = "Import finished.";

            // Whatever the import just filed brought pictures with it, in folders the server has never listed.
            // Asking it to learn where they lie costs the viewer nothing at this moment - the pass waits for the
            // importer, and that one is through - and the first wall of the new titles draws without stopping to
            // search. The server starts the same pass by itself when a folder is added, so this is often only the
            // second knock on the same door, which it answers with the pass it is already running.
            await WarmPicturesAsync(cancellation.Token);
          }
          return;
        }
        await Task.Delay(POLL_DELAY_MS, cancellation.Token);
      }
      Status = "Still importing - press Refresh to ask again.";
    }
    catch (ServerApiException)
    {
      // The watcher stops quietly. What is on screen is still what the server said, and a Refresh says more than a
      // second failure from the same watcher would.
    }
    catch (OperationCanceledException)
    {
      // A newer import started, or the screen went away.
    }
    finally
    {
      // The source belongs to this run alone, and taking the field away first is what keeps a later
      // <see cref="Dispose"/> from cancelling a source that has just been disposed.
      if (ReferenceEquals(_watching, cancellation))
        _watching = null;
      cancellation.Dispose();
    }
  }

  /// <summary>
  /// Stops reading. A screen that closed while an import was being watched would otherwise keep its view model,
  /// its client and its loop alive.
  /// </summary>
  /// <remarks>
  /// Safe to call twice, because two owners is the normal case here: the screen calls this when it goes away and
  /// whatever made the view model may well call it too. A run that finished on its own disposed its own source
  /// already, and cancelling a disposed source throws - which is the last thing a screen that is leaving needs.
  /// </remarks>
  public void Dispose()
  {
    CancellationTokenSource? watching = _watching;
    CancellationTokenSource? running = _running;
    _watching = null;
    _running = null;
    CancelQuietly(watching);
    CancelQuietly(running);
  }

  private static void CancelQuietly(CancellationTokenSource? source)
  {
    try
    {
      source?.Cancel();
    }
    catch (ObjectDisposedException)
    {
      // Its own run is over, which is all this wanted to say.
    }
  }
}

/// <summary>
/// One row of the folder list.
/// </summary>
public sealed class FolderShare : ObservableObject
{
  private string? _confirmation;

  internal FolderShare(ShareInfo share)
  {
    Share = share;
  }

  /// <summary>What the server said about this folder.</summary>
  public ShareInfo Share { get; }

  public Guid Id => Share.Id;

  public string Name => string.IsNullOrWhiteSpace(Share.Name) ? Path : Share.Name!;

  /// <summary>
  /// The folder as the server's filesystem spells it - or, for a folder that is not on a filesystem this server
  /// reaches, its resource path, because that is the only true thing that can be printed about it.
  /// </summary>
  public string Path => string.IsNullOrWhiteSpace(Share.Path) ? Share.ResourcePath ?? "?" : Share.Path!;

  /// <summary>The categories it is filed under, or the sentence for a folder that has none.</summary>
  public string CategoriesText =>
      Share.MediaCategories == null || Share.MediaCategories.Count == 0
          ? "no media type set - nothing can be imported from it"
          : string.Join(", ", Share.MediaCategories);

  /// <summary>Whether this server can read the folder itself.</summary>
  public bool Local => Share.Local;

  /// <summary>The row's small print: where the folder is, and how it is kept up to date.</summary>
  public string Detail
  {
    get
    {
      string where = Local ? "on this server" : "on another system - this server cannot import it";
      return Share.UseShareWatcher ? where + ", watched for changes" : where + ", imported on demand";
    }
  }

  /// <summary>The first letter, for the same reason the tiles use one: there is no icon font.</summary>
  public string Badge => string.IsNullOrEmpty(Name) ? "?" : Name.Substring(0, 1);

  /// <summary>Whether this row is waiting for the second press.</summary>
  public bool AskedToRemove => Confirmation != null;

  /// <summary>
  /// What the row says once remove has been pressed once; null when it is not waiting for a confirmation.
  /// </summary>
  public string? Confirmation
  {
    get { return _confirmation; }
    internal set
    {
      if (SetProperty(ref _confirmation, value))
        OnPropertyChanged(nameof(AskedToRemove));
    }
  }
}

/// <summary>
/// One media category and its tick box.
/// </summary>
public sealed class CategoryChoice : ObservableObject
{
  private bool _isOn;

  internal CategoryChoice(string name, bool isOn)
  {
    Name = name;
    _isOn = isOn;
  }

  /// <summary>The category's name, as the server's extractors registered it.</summary>
  public string Name { get; }

  /// <summary>Whether the folder being added holds this kind of media.</summary>
  public bool IsOn
  {
    get { return _isOn; }
    set { SetProperty(ref _isOn, value); }
  }
}
