#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels;

/// <summary>
/// The photographs, folder by folder, from the server's disks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why folders and not titles.</b> Every other screen of this front end asks the library what it knows, and for
/// films and episodes that question has an answer because the importer decided what the file was. A household's
/// photographs usually have nothing for that decision to hold on to - no title, no year, no season - so a pictures
/// screen built on titles shows a beautiful empty shelf over thousands of photographs. MP2's own pictures screen
/// walked the folders, and this one does the same: it starts at the registered shares and goes down from there.
/// </para>
/// <para>
/// <b>The shares are the root.</b> The screen opens on the folders somebody told the server about, because that is
/// both the honest answer to "where are my pictures?" and the only place the server is willing to look. There is no
/// "whole disk" view behind it and there will not be one: the boundary the file server keeps - inside a share or
/// not - is the same boundary this screen walks.
/// </para>
/// <para>
/// <b>Going back without a parent path.</b> The server lists a folder's contents and does not name the folder above
/// it, on purpose: a parent is one more thing a stranger could ask about for any path. So the screen remembers where
/// it came from, which costs a list of strings and works because nobody arrives in a folder except by walking into
/// it.
/// </para>
/// <para>
/// <b>Small pictures.</b> There is no way to ask the server for a resized image, so a tile's small picture is the
/// whole photograph fetched over the network. That is worth doing for the first <see cref="TILES_WITH_A_PICTURE"/>
/// tiles of a folder and not worth doing for all four thousand, and it is why a tile whose bytes never came is still
/// a tile with a name rather than a broken image.
/// </para>
/// <para>
/// The awaits keep their context, as everywhere else in this front end: what follows them writes properties a bound
/// screen is listening to.
/// </para>
/// </remarks>
public partial class PicturesViewModel : ObservableObject, IDisposable
{
  /// <summary>How many tiles of a folder are fetched a picture for.</summary>
  public const int TILES_WITH_A_PICTURE = 24;

  /// <summary>
  /// Above this size a photograph is not fetched for a tile. A folder's phone snapshot is a megabyte; a
  /// thirty-megabyte scan of a receipt is not what a television wants twenty of at once.
  /// </summary>
  public const long TILE_SIZE_CEILING = 8L * 1024 * 1024;

  /// <summary>How long the viewer waits before moving on, when a slideshow is running.</summary>
  public static TimeSpan SLIDESHOW_TIME = TimeSpan.FromSeconds(7);

  private readonly ServerApiClient? _client;
  private readonly List<string> _cameFrom = new List<string>();
  private FolderListing? _current;
  private List<PictureTile> _viewerTiles = new List<PictureTile>();
  private CancellationTokenSource? _fetching;
  private bool _disposed;

  public PicturesViewModel()
      : this(null)
  {
  }

  public PicturesViewModel(ServerApiClient? client)
  {
    _client = client;
  }

  /// <summary>The tiles on screen: subfolders first, then the pictures.</summary>
  public ObservableCollection<PictureTile> Tiles { get; } = new ObservableCollection<PictureTile>();

  [ObservableProperty]
  private string _heading = "Pictures";

  [ObservableProperty]
  private string _status = "Not read yet.";

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanOpen))]
  private PictureTile? _selected;

  /// <summary>What the folder is, as a person spells it, under the title.</summary>
  [ObservableProperty]
  private string _where = string.Empty;

  [ObservableProperty]
  private bool _isViewing;

  [ObservableProperty]
  private PictureTile? _viewing;

  [ObservableProperty]
  private byte[]? _viewingBytes;

  [ObservableProperty]
  private bool _slideshow;

  /// <summary>The line under a picture being looked at: which one of how many.</summary>
  [ObservableProperty]
  private string _caption = string.Empty;

  /// <summary>Whether the screen is looking at a folder at all - the root of shares has nothing to go up from.</summary>
  public bool CanGoUp
  {
    get { return _cameFrom.Count > 0; }
  }

  public bool CanOpen
  {
    get { return Selected != null; }
  }

  /// <summary>Whether the screen is at the list of shares rather than inside one.</summary>
  public bool IsAtRoot
  {
    get { return _current == null; }
  }

  /// <summary>The folders and pictures of the folder being looked at, for the tests and the caption.</summary>
  internal FolderListing? Current
  {
    get { return _current; }
  }

  /// <summary>
  /// Reads the folder the screen is standing in, or the shares when it is not standing in one yet.
  /// </summary>
  [RelayCommand]
  public async Task RefreshAsync()
  {
    if (_client == null)
    {
      Status = "No server connected";
      return;
    }

    CancellationTokenSource cancellation = RestartFetching();
    try
    {
      if (_current == null)
        await ReadRootAsync(cancellation.Token);
      else
        await ReadFolderAsync(_current.Path, cancellation.Token);
    }
    catch (OperationCanceledException)
    {
      // The viewer moved on while the folder was being read. The screen is already showing the next answer.
    }
    catch (ServerApiException failure)
    {
      Status = DescribeFailure(failure);
    }
  }

  /// <summary>
  /// Walks into the selected folder, or looks at the selected picture.
  /// </summary>
  [RelayCommand]
  public async Task OpenAsync()
  {
    if (Selected == null)
      return;

    await OpenAsync(Selected);
  }

  /// <summary>
  /// Walks into one tile, or looks at it.
  /// </summary>
  public async Task OpenAsync(PictureTile tile)
  {
    if (!tile.IsFolder)
    {
      Show(tile);
      return;
    }

    if (_client == null)
      return;

    _cameFrom.Add(_current == null ? string.Empty : _current.Path);
    OnPropertyChanged(nameof(CanGoUp));
    try
    {
      await ReadFolderAsync(tile.Path, CancellationToken.None);
    }
    catch (OperationCanceledException)
    {
    }
    catch (ServerApiException failure)
    {
      // The way back is still where it was, so the viewer is not stranded: the folder stays on screen and the
      // status line says what went wrong.
      _cameFrom.RemoveAt(_cameFrom.Count - 1);
      OnPropertyChanged(nameof(CanGoUp));
      Status = DescribeFailure(failure);
    }
  }

  /// <summary>
  /// Back to the folder the viewer came from, or to the list of shares.
  /// </summary>
  [RelayCommand]
  public void GoUp()
  {
    if (_cameFrom.Count == 0)
      return;

    string previous = _cameFrom[_cameFrom.Count - 1];
    _cameFrom.RemoveAt(_cameFrom.Count - 1);

    if (previous.Length == 0)
    {
      // Back from inside a share to the list of shares.
      _current = null;
      OnPropertyChanged(nameof(IsAtRoot));
      _ = RefreshAsync();
      OnPropertyChanged(nameof(CanGoUp));
      return;
    }

    _ = ReadFolderAsync(previous, CancellationToken.None);
    OnPropertyChanged(nameof(CanGoUp));
  }

  /// <summary>
  /// Ends the look at a picture. A slideshow that was running stops with it, because a screen that carries on
  /// changing after the viewer left it is a screen that is not listening.
  /// </summary>
  [RelayCommand]
  public void CloseViewer()
  {
    Slideshow = false;
    IsViewing = false;
    Viewing = null;
    ViewingBytes = null;
    Caption = string.Empty;
  }

  /// <summary>The next picture of this folder; stops at the last one rather than wrapping.</summary>
  [RelayCommand]
  public void Next()
  {
    Move(1);
  }

  /// <summary>The previous picture of this folder.</summary>
  [RelayCommand]
  public void Previous()
  {
    Move(-1);
  }

  [RelayCommand]
  public void ToggleSlideshow()
  {
    Slideshow = !Slideshow;
  }

    /// <summary>
    /// Where the screen stands, as a line: the shares, or the folder's own path.
    /// </summary>
    private void SayWhere()
    {
      if (_current == null)
      {
        Heading = "Pictures";
        Where = "the folders the server was told about";
        return;
      }

      Heading = _current.Name;
      Where = _current.LocalPath + " - " + _current.Summary;
    }

  private CancellationTokenSource RestartFetching()
  {
    CancellationTokenSource? previous = _fetching;
    CancellationTokenSource cancellation = new CancellationTokenSource();
    _fetching = cancellation;
    previous?.Cancel();
    previous?.Dispose();
    return cancellation;
  }

  /// <summary>
  /// The list of shares, as folders. The screen's root, and built out of the same tile type as everything else so
  /// that the screen has one shape.
  /// </summary>
  private async Task ReadRootAsync(CancellationToken cancellation)
  {
    SharesAnswer shares = await _client!.GetSharesAsync(cancellation);

    Tiles.Clear();
    _current = null;
    List<FolderEntry> roots = new List<FolderEntry>();
    foreach (ShareInfo share in shares.Shares ?? Array.Empty<ShareInfo>())
      roots.Add(new FolderEntry
      {
          Name = share.Name,
          Path = share.ResourcePath,
          Size = null
      });

    foreach (FolderEntry entry in roots)
      Tiles.Add(new PictureTile(entry));

    SayWhere();
    Status = Tiles.Count == 0
        ? "The server has no folders of its own yet - add one under Folders."
        : Tiles.Count + (Tiles.Count == 1 ? " folder to look in." : " folders to look in.");
    Selected = Tiles.Count > 0 ? Tiles[0] : null;
    OnPropertyChanged(nameof(IsAtRoot));
  }

  private async Task ReadFolderAsync(string path, CancellationToken cancellation)
  {
    FolderListing listing = await _client!.GetFolderAsync(path, cancellation);

    CancellationTokenSource fetching = RestartFetching();
    Tiles.Clear();
    _current = listing;
    _viewerTiles = new List<PictureTile>();

    foreach (FolderEntry folder in listing.Folders)
      Tiles.Add(new PictureTile(folder));
    foreach (FolderEntry picture in listing.Pictures)
    {
      PictureTile tile = new PictureTile(picture);
      Tiles.Add(tile);
      _viewerTiles.Add(tile);
    }

    SayWhere();
    Status = listing.Summary;
    Selected = Tiles.Count > 0 ? Tiles[0] : null;
    OnPropertyChanged(nameof(IsAtRoot));
    OnPropertyChanged(nameof(CanGoUp));

    _ = FetchPicturesAsync(_viewerTiles, fetching.Token);
  }

  /// <summary>
  /// Fetches the picture of as many tiles as are worth it, in the background of the folder that is on screen.
  /// </summary>
  private async Task FetchPicturesAsync(List<PictureTile> tiles, CancellationToken cancellation)
  {
    int asked = 0;
    foreach (PictureTile tile in tiles)
    {
      if (cancellation.IsCancellationRequested)
        return;
      if (asked >= TILES_WITH_A_PICTURE)
      {
        tile.PictureNote = "no preview";
        continue;
      }
      if (tile.BytesOf != null && tile.BytesOf > TILE_SIZE_CEILING)
      {
        tile.PictureNote = "too big to preview";
        continue;
      }

      asked++;
      try
      {
        byte[]? bytes = await _client!.GetResourceBytesAsync(tile.Path, cancellation);
        if (cancellation.IsCancellationRequested)
          return;
        tile.SetPicture(bytes);
      }
      catch (OperationCanceledException)
      {
        return;
      }
      catch (ServerApiException)
      {
        // One unreadable file in a folder of two thousand is a fact about that file, not a reason to say the
        // folder is empty; the tile keeps its name and says it has no preview.
        tile.PictureNote = "no preview";
      }
    }
  }

  private void Show(PictureTile tile)
  {
    int index = _viewerTiles.IndexOf(tile);
    if (index < 0)
      // A folder tile, or a tile from a folder that is no longer on screen.
      return;

    Viewing = tile;
    IsViewing = true;
    Caption = (index + 1) + " of " + _viewerTiles.Count + "  -  " + tile.Name;
    _ = FetchViewingAsync(tile);
  }

  private void Move(int step)
  {
    if (Viewing == null)
      return;

    int index = _viewerTiles.IndexOf(Viewing);
    if (index < 0)
      return;

    index += step;
    if (index < 0 || index >= _viewerTiles.Count)
    {
      // The end of the folder. A slideshow stops there rather than looping forever on a television nobody is
      // watching, and a person pressing the key again gets the same picture, which is what pressing it means.
      Slideshow = false;
      return;
    }

    Show(_viewerTiles[index]);
  }

  private async Task FetchViewingAsync(PictureTile tile)
  {
    try
    {
      byte[]? bytes = tile.Picture ?? await _client!.GetResourceBytesAsync(tile.Path, CancellationToken.None);
      if (tile.Picture == null)
        tile.SetPicture(bytes);
      if (!ReferenceEquals(Viewing, tile))
        return;
      ViewingBytes = bytes;
    }
    catch (OperationCanceledException)
    {
    }
    catch (ServerApiException failure)
    {
      ViewingBytes = null;
      Caption = tile.Name + " - " + DescribeFailure(failure);
    }
  }

  private static string DescribeFailure(ServerApiException failure)
  {
    if (failure.StatusCode == 403)
      // The server keeps its folders to itself, and there are two quite different reasons for that answer: this
      // folder was never registered as a share, or the server wants to know who is asking and this front end has
      // not signed in. Saying both is better than guessing one.
      return "The server would not show this folder. It is either not one of its shares, or the server wants you "
          + "to sign in first.";
    return failure.Message;
  }

  public void Dispose()
  {
    if (_disposed)
      return;
    _disposed = true;
    _fetching?.Cancel();
    _fetching?.Dispose();
  }
}

/// <summary>
/// One tile: a folder to walk into, or a picture to look at.
/// </summary>
public partial class PictureTile : ObservableObject
{
  public PictureTile()
  {
  }

  public PictureTile(FolderEntry entry)
  {
    Name = entry.Name;
    Path = entry.Path;
    BytesOf = entry.Size;
    Detail = entry.Detail;
    Changed = entry.LastChanged;
  }

  public string Name { get; set; } = string.Empty;

  public string Path { get; set; } = string.Empty;

  /// <summary>Bytes of the file behind it, or null when the tile is a folder.</summary>
  public long? BytesOf { get; set; }

  public string Detail { get; set; } = string.Empty;

  public DateTime? Changed { get; set; }

  public bool IsFolder
  {
    get { return BytesOf == null; }
  }

  [ObservableProperty]
  private byte[]? _picture;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(HasNoPicture))]
  private string _pictureNote = string.Empty;

  /// <summary>Whether to explain that this tile has no picture to show.</summary>
  public bool HasNoPicture
  {
    get { return !IsFolder && Picture == null; }
  }

  internal void SetPicture(byte[]? bytes)
  {
    Picture = bytes;
    PictureNote = string.Empty;
    OnPropertyChanged(nameof(HasNoPicture));
  }
}