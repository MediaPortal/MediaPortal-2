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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Frontend.Settings;

namespace MediaPortal.Frontend.Caching
{
  /// <summary>
  /// Where the bytes of one kind of picture come from when this machine has been asked for them before.
  /// </summary>
  /// <remarks>
  /// Handed to <see cref="ViewModels.Artwork"/> instead of the server call. An implementation is asked for one
  /// title at a time and decides whether that means a disk, a server, or both.
  /// </remarks>
  public interface IPictureStore
  {
    /// <summary>
    /// The picture of one title, fetched through <paramref name="fetch"/> when this store has never seen it.
    /// </summary>
    Task<byte[]?> LoadAsync(Guid id, Func<Task<byte[]?>> fetch);
  }

  /// <summary>
  /// The pictures this machine has fetched once, kept on its own disk so that the next start does not ask the
  /// server for them again.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this exists.</b> Every picture on every screen of the library is a read over the network, and the
  /// server's answer for a backdrop is the picture lying next to the media - around 650 KB on an ordinary
  /// library and up to two megabytes on a large one. A wall of tiles is therefore tens of megabytes, and a
  /// second start of the same television asks for all of it again, because the only memory this front end had
  /// was the one that ends when the process does. On a wired box that is invisible; behind wifi, or with a
  /// library of a few thousand titles, it is a wall of empty frames for as long as the link takes.
  /// </para>
  /// <para>
  /// <b>What is kept.</b> The bytes exactly as the server sent them, in one file per title under a folder per
  /// kind of picture and per server, so that a poster is never handed out as a backdrop and a second server's
  /// titles never answer for the first. A title the server answered "none" for is kept as an empty marker file,
  /// because proving that a title has no poster is exactly as expensive as fetching one, and a library where
  /// half the titles have none would otherwise spend its bandwidth proving it over and over.
  /// </para>
  /// <para>
  /// <b>Room, and what happens at the end of it.</b> The viewer says how much room the shelf may take
  /// (<see cref="MediaSettings.CacheSizeMb"/>); past that the oldest pictures go, oldest being the ones this
  /// machine has not handed out for the longest. Nothing is deleted to make room for a picture that was not
  /// asked for, and a shelf that was emptied on the settings screen stays empty until it is used again.
  /// </para>
  /// <para>
  /// <b>Never a reason to wait, and never a reason to fail.</b> Reading happens off the caller's thread,
  /// writing is started and not waited for, and every failure - a disk that is full, a folder on a network
  /// drive that went away, a file another program is holding - ends in the same answer as a shelf that has
  /// never been used: the picture comes from the server, and <see cref="Problem"/> says what went wrong, once,
  /// for the screen to show. A cache that cannot write is a cache that is not helping, not a broken library.
  /// </para>
  /// </remarks>
  public sealed class PictureCache
  {
    /// <summary>The file behind one title's absence, so that "none" is remembered like a picture is.</summary>
    private const string ABSENT_SUFFIX = ".none";

    private readonly string? _settingsPath;
    private MediaSettings _settings;
    private string _root;

    /// <summary>
    /// Guards the room the shelf takes and the writing of every picture, so that two answers arriving together
    /// cannot both decide to clear room and end up deleting each other's work.
    /// </summary>
    private readonly SemaphoreSlim _writer = new SemaphoreSlim(1, 1);

    /// <summary>
    /// What the shelf holds, counted as it is written. <c>null</c> means "not counted yet", which is what a
    /// folder that has never been touched - or one whose answers changed - starts as.
    /// </summary>
    private long? _held;

    private string? _problem;

    /// <summary>
    /// The room the shelf keeps itself inside, when a test named one. The media answers give it in megabytes,
    /// which is a size nobody wants to write to disk to prove that the oldest pictures go first.
    /// </summary>
    private long? _roomOverride;

    /// <summary>
    /// The room of the shelf, in bytes. Opened to the front end's own test assembly, which is the only place
    /// that asks for a shelf small enough to fill in a test.
    /// </summary>
    internal long RoomBytes
    {
      get { return _roomOverride ?? _settings.CacheSizeBytes; }
      set { _roomOverride = value; }
    }

    /// <summary>
    /// @param settingsPath The media answers, or <c>null</c> for this machine's own file. A test hands in a file
    /// in a temporary folder so that it never reads what a viewer answered.
    /// </summary>
    public PictureCache(string? settingsPath = null)
    {
      _settingsPath = settingsPath;
      _settings = MediaSettings.Load(settingsPath, out _);
      _root = _settings.CacheFolderPath;
    }

    /// <summary>
    /// The answers as they stand now, after the last <see cref="Reload"/>.
    /// </summary>
    public MediaSettings Settings => _settings;

    /// <summary>
    /// The folder the shelf lives in, whether the viewer named it or the machine did.
    /// </summary>
    public string Folder => _root;

    /// <summary>
    /// Whether this machine keeps pictures at all. Turning it off leaves the files where they are; it only
    /// stops the shelf from being read and written.
    /// </summary>
    public bool Enabled => _settings.CachePictures;

    /// <summary>
    /// How many pictures a screen should hold in memory, which the same file answers because a viewer who says
    /// "my disk is small" almost always means the machine is small.
    /// </summary>
    public int RememberedPictures => _settings.RememberedPictures;

    /// <summary>
    /// How many pictures a screen may have on the wire at one moment.
    /// </summary>
    public int PicturesAtOnce => _settings.PicturesAtOnce;

    /// <summary>
    /// The last thing this shelf could not do, in the words the screen shows. Reading it clears it, so that one
    /// complaint is not repeated under every row.
    /// </summary>
    public string? Problem
    {
      get { return Interlocked.Exchange(ref _problem, null); }
    }

    /// <summary>
    /// The shelf for one kind of picture of one server, which is what a screen hands to its pictures.
    /// </summary>
    /// <param name="kind">What the screen asks for, in the server's words: <c>Poster</c>, <c>FanArt</c>, ...</param>
    /// <param name="server">Which machine the bytes came from, because two servers do not share answers.</param>
    public IPictureStore Store(string kind, Uri server)
    {
      return new Shelf(this, Path.Combine(_root, kind, Authority(server)));
    }

    /// <summary>
    /// One screen's holder of one kind of picture, told with this machine's answers: which shelf to ask first,
    /// how much of it to keep in memory, and how much to have on the wire at a time. This is the one place the
    /// media settings reach the screens, which is why a screen that has no shelf here - every test, and every
    /// screen opened without a server - is left with exactly the memory-only behaviour it had before.
    /// </summary>
    /// <param name="kind">What the screen asks for, in the server's words.</param>
    /// <param name="server">The machine the bytes come from.</param>
    /// <param name="ask">How to ask that machine for one title's picture.</param>
    public ViewModels.Artwork ArtworkFor(string kind, Uri server, Func<Guid, Task<byte[]?>> ask)
    {
      return new ViewModels.Artwork(ask, Store(kind, server), _settings.RememberedPictures,
          _settings.PicturesAtOnce);
    }

    /// <summary>
    /// Reads the answers again. The settings screen calls this the breath after it saved, so that turning the
    /// shelf off or giving it more room takes effect without a restart.
    /// </summary>
    public void Reload()
    {
      _settings = MediaSettings.Load(_settingsPath, out _);
      string folder = _settings.CacheFolderPath;
      if (!string.Equals(folder, _root, StringComparison.OrdinalIgnoreCase))
      {
        // A different folder is a different shelf, and what this run counted about the old one says nothing
        // about the new one.
        _root = folder;
        _held = null;
      }
    }

    /// <summary>
    /// Waits until the shelf has finished writing what it was writing. A picture is written on the side, so this
    /// is how a run that is about to end - or a test that is about to look at the disk - makes sure nothing is
    /// still on its way to it.
    /// </summary>
    public async Task FlushAsync()
    {
      await _writer.WaitAsync().ConfigureAwait(false);
      _writer.Release();
    }

    /// <summary>
    /// What the shelf holds now: the room it takes, and how many pictures and how many absences are in it.
    /// </summary>
    public Task<(long bytes, int pictures, int without)> MeasureAsync()
    {
      return Task.Run(delegate
      {
        long bytes = 0;
        int pictures = 0;
        int absent = 0;
        try
        {
          if (!Directory.Exists(_root))
            return (0L, 0, 0);

          foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
          {
            try
            {
              long length = new FileInfo(file).Length;
              bytes += length;
              if (file.EndsWith(ABSENT_SUFFIX, StringComparison.OrdinalIgnoreCase))
                absent++;
              else if (!file.EndsWith(".new", StringComparison.OrdinalIgnoreCase))
                pictures++;
            }
            catch (IOException)
            {
              // A file that will not say how big it is is not worth stopping the count for.
            }
          }
        }
        catch (Exception ex)
        {
          Say(ex);
        }

        return (bytes, pictures, absent);
      });
    }

    /// <summary>
    /// Empties the shelf. The screens keep what they are holding in memory; only what was written to the disk
    /// goes.
    /// </summary>
    /// <returns>The sentence for the status line.</returns>
    public Task<string> ClearAsync()
    {
      return Task.Run(delegate
      {
        int removed = 0;
        try
        {
          if (Directory.Exists(_root))
          {
            foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
              try
              {
                File.Delete(file);
                removed++;
              }
              catch (Exception ex)
              {
                Say(ex);
              }
            }

            // Deepest folders first, so that a tree of emptied kinds and servers goes with them.
            foreach (string folder in Directory.EnumerateDirectories(_root, "*", SearchOption.AllDirectories)
                     .OrderByDescending(delegate (string path) { return path.Length; }))
            {
              try
              {
                if (!Directory.EnumerateFileSystemEntries(folder).Any())
                  Directory.Delete(folder);
              }
              catch (IOException)
              {
                // An empty folder that will not go away - somebody is looking at it - costs nothing.
              }
            }
          }
        }
        catch (Exception ex)
        {
          Say(ex);
        }

        _held = 0;
        return string.Format("{0} kept pictures were removed from {1}.", removed, _root);
      });
    }

    /// <summary>
    /// One shelf: one kind of picture of one server, which is one folder of files named by title.
    /// </summary>
    private sealed class Shelf : IPictureStore
    {
      private readonly PictureCache _cache;
      private readonly string _folder;

      internal Shelf(PictureCache cache, string folder)
      {
        _cache = cache;
        _folder = folder;
      }

      public async Task<byte[]?> LoadAsync(Guid id, Func<Task<byte[]?>> fetch)
      {
        if (!_cache.Enabled)
          return await fetch().ConfigureAwait(true);

        byte[]? kept = await _cache.ReadAsync(_folder, id).ConfigureAwait(true);
        if (kept != null)
        {
          // An empty file is the kept answer "this title has no picture", handed back as nothing.
          _cache.Touched(_folder, id);
          return kept.Length == 0 ? null : kept;
        }

        byte[]? fetched = await fetch().ConfigureAwait(true);

        // Started, not waited for: a picture that is on its way to the screen must not wait for a disk.
        _ = _cache.WriteAsync(_folder, id, fetched);
        return fetched;
      }
    }

    /// <summary>
    /// The bytes kept for one title, or <c>null</c> when the shelf has never been asked about it. An empty
    /// array is the kept answer "the server has none for this title".
    /// </summary>
    private async Task<byte[]?> ReadAsync(string folder, Guid id)
    {
      try
      {
        string file = Path.Combine(folder, Name(id));
        if (!File.Exists(file))
        {
          string absent = file + ABSENT_SUFFIX;
          return File.Exists(absent) ? Array.Empty<byte>() : null;
        }

        byte[] bytes = await File.ReadAllBytesAsync(file).ConfigureAwait(false);
        Touched(folder, id);
        return bytes;
      }
      catch (Exception ex)
      {
        Say(ex);
      }

      return null;
    }

    /// <summary>
    /// Puts one answer on the shelf and keeps the shelf inside its room.
    /// </summary>
    private async Task WriteAsync(string folder, Guid id, byte[]? picture)
    {
      try
      {
        await _writer.WaitAsync().ConfigureAwait(false);
        try
        {
          if (!Enabled)
            return;

          Directory.CreateDirectory(folder);
          string file = Path.Combine(folder, Name(id));
          string temporary = file + ".new";

          if (picture == null)
          {
            // Nothing to keep but the answer, which is worth keeping: see the class remarks.
            string absent = file + ABSENT_SUFFIX;
            if (!File.Exists(absent))
            {
              await File.WriteAllBytesAsync(temporary, Array.Empty<byte>()).ConfigureAwait(false);
              Move(temporary, absent);
              Count(0);
            }

            return;
          }

          await File.WriteAllBytesAsync(temporary, picture).ConfigureAwait(false);
          long previous = File.Exists(file) ? new FileInfo(file).Length : 0L;
          Move(temporary, file);
          Count(picture.Length - previous);
          Trim();
        }
        finally
        {
          _writer.Release();
        }
      }
      catch (Exception ex)
      {
        Say(ex);
      }
    }

    /// <summary>
    /// Marks a kept picture as just used, so that the room is cleared from the pictures nobody has looked at
    /// for the longest. Only done when the mark is a day old, because touching a file is a write and a shelf
    /// that is read for every tile would spend its life rewriting stamps.
    /// </summary>
    private void Touched(string folder, Guid id)
    {
      try
      {
        string file = Path.Combine(folder, Name(id));
        if (!File.Exists(file))
          return;

        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < TimeSpan.FromDays(1))
          return;

        File.SetLastWriteTimeUtc(file, DateTime.UtcNow);
      }
      catch (Exception ex)
      {
        Say(ex);
      }
    }

    /// <summary>
    /// Adds what one write changed to the room the shelf takes. The count starts from nothing rather than from
    /// a walk of the folder, because a shelf only ever has to stay inside its room from here on - a run that
    /// wrote nothing but inherited a full folder is corrected by the first picture that arrives.
    /// </summary>
    private void Count(long delta)
    {
      long held = (_held ?? 0) + delta;
      _held = held < 0 ? 0 : held;
    }

    /// <summary>
    /// Throws the oldest pictures out until the shelf is back inside its room. Takes the folder's own list
    /// rather than a table of its own, because the disk is the only thing that outlives a restart.
    /// </summary>
    private void Trim()
    {
      long room = RoomBytes;
      if (_held == null || _held.Value <= room)
        return;

      List<string> files = new List<string>();
      try
      {
        if (Directory.Exists(_root))
          files.AddRange(Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories));
      }
      catch (Exception ex)
      {
        Say(ex);
        return;
      }

      foreach (string file in files.OrderBy(delegate (string path)
      {
        try
        {
          return File.GetLastWriteTimeUtc(path);
        }
        catch (Exception)
        {
          return DateTime.MinValue;
        }
      }))
      {
        if (_held!.Value <= room)
          break;

        try
        {
          long length = new FileInfo(file).Length;
          File.Delete(file);
          Count(-length);
        }
        catch (Exception ex)
        {
          Say(ex);
        }
      }
    }

    /// <summary>
    /// Puts the finished file where it belongs, replacing what was there. A move within one folder is the
    /// atomic version of "either the whole picture is there or the old one still is", which is what lets a
    /// television that was pulled out mid-write start without half a picture on its shelf.
    /// </summary>
    private static void Move(string temporary, string target)
    {
      try
      {
        File.Move(temporary, target, true);
      }
      catch (IOException)
      {
        // Something else is holding the file it replaced. The new copy waits for the next time instead.
        try
        {
          File.Delete(temporary);
        }
        catch (IOException)
        {
          // And now the temporary file waits too. Nothing here is worth a complaint to the viewer.
        }
      }
    }

    /// <summary>
    /// The name of one title's file. The id alone, because a title is what the picture belongs to; the kind
    /// and the server are already in the folders above it.
    /// </summary>
    private static string Name(Guid id)
    {
      return id.ToString("N", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The server's address in the shape a folder name allows, so that the answers of two servers can never be
    /// mistaken for each other's.
    /// </summary>
    private static string Authority(Uri server)
    {
      string text = server.GetLeftPart(UriPartial.Authority).ToLowerInvariant();
      char[] characters = text.Select(delegate (char c)
      {
        return char.IsLetterOrDigit(c) || c == '-' || c == '.' ? c : '-';
      }).ToArray();
      return new string(characters);
    }

    /// <summary>
    /// Remembers what went wrong, for the screen to say once. A shelf that cannot be read or written is not a
    /// reason to stop a picture from arriving.
    /// </summary>
    private void Say(Exception ex)
    {
      _problem = string.Format("The picture shelf at {0} could not be used ({1}: {2}).", _root,
          ex.GetType().Name, ex.Message);
    }
  }
}
