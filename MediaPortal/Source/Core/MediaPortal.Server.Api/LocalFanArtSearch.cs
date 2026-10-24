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
using System.IO;
using System.Linq;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The pictures that lie next to the media itself - the ones a tagging tool puts into the film's or the
  /// series' folder - and what the server <em>remembers</em> about where they are.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>Why this is a memory and not a cache.</b> The reference's fanart cache copies pictures into the server's
  /// own folder, which is right for pictures fetched from the internet: they exist nowhere else. A picture that
  /// already lies on the disc beside the film needs no second copy - a library of a few thousand titles would
  /// double its size for nothing, and the copy would go stale the day the viewer replaces the original. So this
  /// class keeps the <em>answer to the question</em> rather than the bytes: which file answers "the banner of
  /// this title", and when was that file last written. The next question then costs one file open instead of a
  /// database search for which item this is, an accessor for its folder, and a listing of up to three folders
  /// three levels up - which is what the same question used to cost, once per kind and per tile on the screen.
  /// </para>
  /// <para>
  /// <b>A whole title is answered at once.</b> One folder listing holds every kind that lies in that folder, so
  /// the first question about a title sweeps all seven kinds into the memory, and the seven questions a wall
  /// asks about that title - poster, banner, backdrop, thumbnail, logo, clear art, disc art - are seven file
  /// opens. Kinds the folders have nothing for are remembered as "nothing here" too, because a title without a
  /// banner is the normal case on a big wall, and asking the folders again for every one of them is exactly the
  /// work this memory was added to skip.
  /// </para>
  /// <para>
  /// <b>What an old answer is worth.</b> A remembered file is looked at before it is handed out: it has to
  /// exist, and its write time and size have to still be what they were when it was remembered. A replaced or
  /// deleted picture is therefore noticed at the next question about it, and the folders are searched again.
  /// "Nothing here" is believed for <see cref="NOTHING_LASTS"/>, so a viewer who drops a poster.jpg into a
  /// folder while the server runs sees it without restarting anything.
  /// </para>
  /// <para>
  /// The search itself is the reference's, unchanged: the fanart cache first (see <see cref="IFanArtCache"/>,
  /// which the caller asks before this class), then the pictures next to the media, walking up from the title's
  /// own folder to its parent and grandparent because an episode's banner lives with its series.
  /// </para>
  /// </remarks>
  public sealed class LocalFanArtSearch
  {
    /// <summary>
    /// The kinds this search answers for, in the reference's words. The endpoint's list and the warming pass'
    /// list are this one list, so that a kind added to one cannot be forgotten by the other.
    /// </summary>
    internal static readonly string[] KINDS =
    {
        FanArtTypes.Poster, FanArtTypes.Banner, FanArtTypes.FanArt, FanArtTypes.Thumbnail,
        FanArtTypes.Logo, FanArtTypes.ClearArt, FanArtTypes.DiscArt
    };

    /// <summary>How long "this title has no picture of that kind" is believed.</summary>
    private static readonly TimeSpan NOTHING_LASTS = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How many answers the memory holds. Seven per title across a library of five thousand titles is thirty
    /// five thousand answers, and an answer is one path and three numbers, so the ceiling is about the size of a
    /// poster. Past it the answers nobody asked about lately are given up.
    /// </summary>
    private const int DEFAULT_ROOM = 40000;

    /// <summary>How many answers are given up at once when the memory is over its room.</summary>
    private const int DEFAULT_GIVING_UP_WHEN_FULL = 4000;

    /// <summary>The one memory of this server's run.</summary>
    public static readonly LocalFanArtSearch Worn = new LocalFanArtSearch();

    private readonly Dictionary<string, Answer> _answers = new Dictionary<string, Answer>();
    private int _room = DEFAULT_ROOM;
    private int _givingUpWhenFull = DEFAULT_GIVING_UP_WHEN_FULL;

    /// <summary>Grows with every question, so that the answers nobody asks about any more are the ones given up.</summary>
    private long _asked;

    private LocalFanArtSearch()
    {
    }

    /// <summary>
    /// How many answers the memory holds. Setting it lower than what is already held throws nothing away at once;
    /// the next answer added trims the memory down to it.
    /// </summary>
    public int Room
    {
      get { return _room; }
      set { _room = Math.Max(1, value); }
    }

    /// <summary>
    /// How many answers are given up at the moment <see cref="Room"/> is over. Given up in bunches rather than one
    /// at a time, because sorting the memory is the expensive part of making room and doing it per answer added
    /// would make filling the memory cost as much as the walking it was added to skip.
    /// </summary>
    public int GivingUpWhenFull
    {
      get { return _givingUpWhenFull; }
      set { _givingUpWhenFull = Math.Max(1, value); }
    }

    /// <summary>
    /// One remembered answer: the title and kind it belongs to, the file it settled on - null for "nothing of
    /// that kind lies next to this title" - what that file looked like when it was settled on, and when the
    /// answer was last of use.
    /// </summary>
    private sealed class Answer
    {
      internal string Key;
      internal string File;
      internal DateTime Written;
      internal long Length;
      internal DateTimeOffset SaidAt;
      internal long Asked;
    }

    /// <summary>What the memory knows about one kind of one title.</summary>
    private enum Known
    {
      /// <summary>It has never been asked, or its answer stopped standing.</summary>
      NotRemembered,

      /// <summary>It was asked and the folders had nothing - an answer worth believing for a while.</summary>
      NothingHere,

      /// <summary>It was asked and the file it named is still the file it named.</summary>
      Found
    }
    /// <summary>
    /// The picture of one kind for one title, out of the memory while the memory stands, out of the folders
    /// when it does not - and null when the library has nothing of that kind. Never throws: a title whose folder
    /// cannot be read is a title with no picture in it as far as a shelf is concerned.
    /// </summary>
    public byte[] Picture(Guid id, string fanArtType)
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>(false);
      if (library == null)
        return null;

      string remembered;
      Known known = Recall(id, fanArtType, out remembered);
      if (known == Known.NothingHere)
        return null;

      if (known == Known.Found)
      {
        byte[] bytes = ReadPicture(remembered);
        if (bytes != null)
          return bytes;

        // The file the memory named is gone or unreadable after all, which is worth one search - not a lie.
        Forget(id, fanArtType);
      }

      return ReadPicture(Search(library, id, fanArtType));
    }

    /// <summary>
    /// Answering all seven kinds of one title at once, which is what a wall is going to ask for anyway. Called by
    /// the warming pass after an import, so that the first look at a new shelf costs file opens and no searching.
    /// </summary>
    /// <returns>How many of the seven kinds the folders had.</returns>
    internal int Warm(Guid id)
    {
      IMediaLibrary library = ServiceRegistration.Get<IMediaLibrary>(false);
      if (library == null)
        return 0;

      return Sweep(library, id).Count;
    }

    /// <summary>
    /// How many answers the memory holds, which is what the warming pass reports and what a viewer with a big
    /// library wants to see growing.
    /// </summary>
    public int RememberedCount
    {
      get { lock (this) return _answers.Count; }
    }

    /// <summary>
    /// Whether the memory already has a word about every kind of one title, found or "nothing". The warming pass
    /// asks this before it walks a title, so that the pass after three new films costs three titles and not the
    /// whole library.
    /// </summary>
    internal bool Learned(Guid id)
    {
      lock (this)
      {
        return KINDS.All(kind => _answers.ContainsKey(Key(id, kind)));
      }
    }

    /// <summary>
    /// Forgetting every answer, so that a warming pass after an import starts from the folders and not from what
    /// the folders said before it.
    /// </summary>
    public void ForgetEverything()
    {
      lock (this)
      {
        _answers.Clear();
      }
    }

    /// <summary>
    /// Searching one kind of one title, filing all seven kinds on the way, and answering with the file that was
    /// found for the kind asked for - or null.
    /// </summary>
    private string Search(IMediaLibrary library, Guid id, string fanArtType)
    {
      Dictionary<string, string> found = Sweep(library, id);
      string file;
      return found.TryGetValue(fanArtType, out file) ? file : null;
    }

    /// <summary>
    /// Walking the folders of one title the way the reference walks them and answering every kind on the way.
    /// </summary>
    /// <returns>The kinds the folders had, with the file each was found in.</returns>
    /// <remarks>
    /// The walk is the reference's: the title's own folder first, then its parent and its grandparent, because
    /// an episode's banner lives with its series one or two levels up. A title that is a series or a season has
    /// no file of its own, so <see cref="FanArtAnchors"/> hands over one of its episodes to start from. The
    /// first folder that has a picture of a kind wins, which is the reference's priority: whatever lies with the
    /// episode itself outranks whatever lies with the series.
    /// </remarks>
    private Dictionary<string, string> Sweep(IMediaLibrary library, Guid id)
    {
      Dictionary<string, string> found = new Dictionary<string, string>();

      foreach (MediaItem anchor in FanArtAnchors(library, id))
      {
        IResourceLocator locator = anchor.GetResourceLocator();
        if (locator == null)
          continue;

        ResourcePath mediaFile = locator.NativeResourcePath;
        ResourcePath directory = ResourcePathHelper.Combine(mediaFile, "../");

        // The film's own file name is half of the reference's naming rule: a tagging tool that keeps one
        // folder per film files its pictures as "<file>-poster.jpg" and "<file>-fanart.jpg", and the reference
        // reads those as the film's poster and backdrop. Whether the item is itself a picture belongs to the
        // same question, because a picture would otherwise be its own thumbnail.
        string mediaStem = ResourcePath.GetFileNameWithoutExtension(mediaFile).ToLowerInvariant();
        bool wearsItsOwnName = !LocalFanartHelper.EXTENSIONS.Contains(
            ResourcePathHelper.GetExtension(mediaFile.ToString()).ToLowerInvariant());

        for (int hop = 0; hop < 3 && directory != null; hop++)
        {
          ListPicturesIn(directory, hop == 0 ? mediaStem : null, wearsItsOwnName, found);

          try
          {
            directory = ResourcePathHelper.Combine(directory, "../");
          }
          catch (ArgumentException)
          {
            // The walk climbed past the root of the provider; there is no folder above this one.
            break;
          }
        }
      }

      Remember(id, found);
      return found;
    }

    /// <summary>
    /// Filing what one folder holds, for every kind that no folder below it answered yet.
    /// </summary>
    private static void ListPicturesIn(ResourcePath directory, string mediaStem, bool wearsItsOwnName,
        Dictionary<string, string> found)
    {
      IResourceAccessor accessor = TryOpenLocalFile(directory);
      if (accessor == null)
        return;

      try
      {
        IFileSystemResourceAccessor folder = accessor as IFileSystemResourceAccessor;
        if (folder == null || folder.IsFile || !folder.Exists)
          return;

        IList<ResourcePath> potential = LocalFanartHelper.GetPotentialFanArtFiles(folder);

        foreach (string kind in KINDS)
        {
          if (found.ContainsKey(kind))
            continue;

          IList<ResourcePath> matching = MatchFiles(potential, kind, mediaStem, wearsItsOwnName);

          // A backdrop may also lie below an "ExtraFanArt" folder, which is where the reference's providers keep
          // the second and third backdrops of a title.
          if (matching.Count == 0 && kind == FanArtTypes.FanArt && folder.ResourceExists("ExtraFanArt/"))
          {
            using (IResourceAccessor extra = folder.GetResource("ExtraFanArt/"))
            {
              IFileSystemResourceAccessor extraFolder = extra as IFileSystemResourceAccessor;
              if (extraFolder != null)
              {
                matching = MatchFiles(LocalFanartHelper.GetPotentialFanArtFiles(extraFolder), kind, mediaStem,
                    wearsItsOwnName);
              }
            }
          }

          foreach (ResourcePath candidate in matching)
          {
            // The reference took the first picture it could read, so "can be read" is settled here, before the
            // file is remembered - a poster of no bytes is no poster.
            if (HasBytes(candidate))
            {
              found[kind] = candidate.Serialize();
              break;
            }
          }
        }
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
      finally
      {
        accessor.Dispose();
      }
    }

    /// <summary>
    /// The pictures of one kind among the images of one folder, in the reference's priority order: the exact
    /// names it lists for the kind ("banner", "poster" ...), then the same names behind the media file's own
    /// name, because a tagging tool that files one folder per film calls its pictures
    /// "<file>-poster.jpg" and "<file>-fanart.jpg" - and for a backdrop finally whatever merely starts with
    /// "fanart" or "backdrop", since "fanart-2.jpg" is a backdrop of the same title.
    /// </summary>
    /// <remarks>
    /// The reference asks its <c>LocalFanartHelper</c> once per kind with both the names and the name-behind-
    /// the-media-file as prefixes, and that helper sorts by a priority number it hands to
    /// <c>Dictionary.Add</c>. Two pictures answering one prefix ("fanart.jpg" and "fanart-2.jpg", which a
    /// library with three backdrops per film writes) are two files for one number, and <c>Add</c> answers that
    /// with an exception that would reach the viewer as a failed picture. So the walking is done here, file by
    /// file, in the same order: the same answers, the same priority, no exception.
    /// </remarks>
    private static IList<ResourcePath> MatchFiles(IList<ResourcePath> potential, string fanArtType,
        string mediaStem, bool wearsItsOwnName)
    {
      string[] names = NamesFor(fanArtType);
      if (names == null)
        return new List<ResourcePath>();

      List<ResourcePath> found = new List<ResourcePath>();
      AddMatching(found, potential, name => names.Contains(name));

      if (mediaStem != null)
      {
        AddMatching(found, potential,
            name => names.Any(one => name.StartsWith(mediaStem + "-" + one, StringComparison.Ordinal)));
      }

      if (fanArtType == FanArtTypes.FanArt)
        AddMatching(found, potential, name => names.Any(one => name.StartsWith(one, StringComparison.Ordinal)));

      // A picture carrying the title's own file name is its poster and its screenshot too - but only while the
      // title is not itself a picture, which would make every picture its own thumbnail.
      if (mediaStem != null && wearsItsOwnName &&
          (fanArtType == FanArtTypes.Poster || fanArtType == FanArtTypes.Thumbnail))
        AddMatching(found, potential, name => name == mediaStem);

      return found;
    }

    /// <summary>
    /// The names the reference lists for one kind of picture, or <c>null</c> for a kind no folder answers for.
    /// </summary>
    private static string[] NamesFor(string fanArtType)
    {
      switch (fanArtType)
      {
        case FanArtTypes.Poster:
          return LocalFanartHelper.POSTER_FILENAMES;
        case FanArtTypes.Banner:
          return LocalFanartHelper.BANNER_FILENAMES;
        case FanArtTypes.Logo:
          return LocalFanartHelper.LOGO_FILENAMES;
        case FanArtTypes.ClearArt:
          return LocalFanartHelper.CLEARART_FILENAMES;
        case FanArtTypes.DiscArt:
          return LocalFanartHelper.DISCART_FILENAMES;
        case FanArtTypes.FanArt:
          return LocalFanartHelper.BACKDROP_FILENAMES;
        case FanArtTypes.Thumbnail:
          return LocalFanartHelper.THUMB_FILENAMES;
        default:
          return null;
      }
    }

    /// <summary>
    /// Adding the pictures whose name answers <paramref name="matches"/>, in the order the folder listed them
    /// and skipping the ones an earlier round already claimed.
    /// </summary>
    private static void AddMatching(List<ResourcePath> found, IEnumerable<ResourcePath> potential,
        Func<string, bool> matches)
    {
      foreach (ResourcePath candidate in potential)
      {
        if (matches(NameOf(candidate)) && !found.Contains(candidate))
          found.Add(candidate);
      }
    }

    /// <summary>The name of one picture without its extension, spelled the way the reference spells names.</summary>
    private static string NameOf(ResourcePath path)
    {
      return ResourcePathHelper.GetFileNameWithoutExtension(path.ToString()).ToLowerInvariant();
    }
    /// <summary>
    /// The titles whose folders may hold this title's fanart: the title itself, and - when it is a series or a
    /// season, which have no file of their own - one episode of it.
    /// </summary>
    /// <remarks>
    /// This is the reference's own detour, read from LocalSeriesFanartProvider: asked for a series' banner it
    /// does not ask the series, it searches for one episode of the series and derives the series folder from
    /// that episode's path. A series is a shelf of episodes and has no file; the pictures lie with its episodes,
    /// so that is where the search has to start.
    /// </remarks>
    private static IList<MediaItem> FanArtAnchors(IMediaLibrary mediaLibrary, Guid id)
    {
      IList<MediaItem> anchors = new List<MediaItem>();
      try
      {
        MediaItemQuery query = new MediaItemQuery(
            new List<Guid> { MediaAspect.ASPECT_ID },
            new List<Guid> { ProviderResourceAspect.ASPECT_ID, SeriesAspect.ASPECT_ID, SeasonAspect.ASPECT_ID,
                AudioAlbumAspect.ASPECT_ID },
            new MediaItemIdFilter(id));
        IList<MediaItem> items = mediaLibrary.Search(query, false, null, false);
        MediaItem item = items == null ? null : items.FirstOrDefault();

        if (item == null)
          return anchors;
        anchors.Add(item);

        if (item.GetResourceLocator() != null)
        {
          // A title with its own file needs no detour.
          return anchors;
        }

        MediaItem episode = FirstEpisodeOf(mediaLibrary, item);
        if (episode != null)
        {
          anchors.Add(episode);
          return anchors;
        }

        // An album is the same shape of shelf: it holds tracks and has no file of its own, and the music
        // fanart lies above the album folder - the artists' own folder - so the walk starts from a track.
        MediaItem track = FirstTrackOf(mediaLibrary, item);
        if (track != null)
          anchors.Add(track);
      }
      catch (IllegalCallException)
      {
        anchors.Clear();
      }
      return anchors;
    }

    /// <summary>
    /// One episode of a title that has no file of its own - a series or a season - or null when it holds none.
    /// </summary>
    /// <remarks>
    /// The reference's detour (see <see cref="FanArtAnchors"/>): a series is a shelf of episodes and has no file,
    /// so whatever lies next to its episodes' files is found by starting from one of them. The metadata search
    /// next to the pictures walks the same road, so it is one method and not two.
    /// </remarks>
    internal static MediaItem FirstEpisodeOf(IMediaLibrary mediaLibrary, MediaItem item)
    {
      Guid relation;
      SingleMediaItemAspect heldAspect;
      if (MediaItemAspect.TryGetAspect(item.Aspects, SeriesAspect.Metadata, out heldAspect))
        relation = SeriesAspect.ROLE_SERIES;
      else if (MediaItemAspect.TryGetAspect(item.Aspects, SeasonAspect.Metadata, out heldAspect))
        relation = SeasonAspect.ROLE_SEASON;
      else
        return null;

      MediaItemQuery episodes = new MediaItemQuery(
          new List<Guid> { EpisodeAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID },
          new RelationshipFilter(EpisodeAspect.ROLE_EPISODE, relation, item.MediaItemId));
      episodes.Limit = 1;
      IList<MediaItem> held = mediaLibrary.Search(episodes, false, null, false);
      return held == null || held.Count == 0 ? null : held[0];
    }

    /// <summary>
    /// One track of a title that has no file of its own - an album - or null when it holds none.
    /// </summary>
    /// <remarks>
    /// The music shape of the detour <see cref="FirstEpisodeOf"/> is the films' one: the reference's
    /// LocalArtistFanartProvider answered an album's fanart by climbing from its tracks' folders to the
    /// artist's, where the backdrop lies. A disc whose tracks sit in "CD 01" needs those two extra hops,
    /// which the walk's three folders cover from the track itself.
    /// </remarks>
    internal static MediaItem FirstTrackOf(IMediaLibrary mediaLibrary, MediaItem item)
    {
      SingleMediaItemAspect heldAspect;
      if (!MediaItemAspect.TryGetAspect(item.Aspects, AudioAlbumAspect.Metadata, out heldAspect))
        return null;

      MediaItemQuery tracks = new MediaItemQuery(
          new List<Guid> { AudioAspect.ASPECT_ID },
          new List<Guid> { ProviderResourceAspect.ASPECT_ID },
          new RelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, item.MediaItemId));
      tracks.Limit = 1;
      IList<MediaItem> held = mediaLibrary.Search(tracks, false, null, false);
      return held == null || held.Count == 0 ? null : held[0];
    }

    /// <summary>
    /// The accessor for a path the server's own local providers reach, or null - which reads as "not there"
    /// rather than throwing. A path whose provider is not registered at all, or belongs to another system, is as
    /// unreadable here as a deleted file, and a shelf or a details answer must not fail because of it; the plain
    /// <see cref="ResourcePath.TryCreateLocalResourceAccessor"/> throws exactly for that case.
    /// </summary>
    /// <remarks>
    /// The metadata files next to the media are opened the same way (see <see cref="MediaController"/>'s details
    /// answer), so this is one door for both rather than two that can drift apart.
    /// </remarks>
    internal static IResourceAccessor TryOpenLocalFile(ResourcePath path)
    {
      if (path == null || ServiceRegistration.Get<IMediaAccessor>(false) == null)
        return null;
      try
      {
        IResourceAccessor accessor;
        if (!path.TryCreateLocalResourceAccessor(out accessor))
          return null;
        return accessor;
      }
      catch (IllegalCallException)
      {
        return null;
      }
    }

    /// <summary>
    /// The local folder or file a provider path stands for, or null for anything this server cannot open as a
    /// path of its own machine. Only pictures of this kind are ever remembered, which is what lets a remembered
    /// answer be checked with one stat instead of an opened accessor.
    /// </summary>
    private static string LocalPathOf(ResourcePath path)
    {
      if (path == null)
        return null;
      try
      {
        return LocalFsResourceProviderBase.ToDosPath(path);
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// As <see cref="LocalPathOf(ResourcePath)"/>, for a path as it was kept in the memory.
    /// </summary>
    private static string LocalPathOf(string serializedPath)
    {
      try
      {
        return LocalPathOf(ResourcePath.Deserialize(serializedPath));
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    /// <summary>
    /// Whether the file one path names is there and has something in it, without reading it.
    /// </summary>
    private static bool HasBytes(ResourcePath path)
    {
      string local = LocalPathOf(path);
      if (local == null)
        return false;

      try
      {
        FileInfo info = new FileInfo(local);
        return info.Exists && info.Length > 0;
      }
      catch (IOException)
      {
        return false;
      }
      catch (UnauthorizedAccessException)
      {
        return false;
      }
    }

    /// <summary>
    /// The bytes of one remembered picture, or null when it is not there, is not a file, holds nothing, or
    /// cannot be read.
    /// </summary>
    private static byte[] ReadPicture(string serializedPath)
    {
      if (serializedPath == null)
        return null;

      ResourcePath path;
      try
      {
        path = ResourcePath.Deserialize(serializedPath);
      }
      catch (ArgumentException)
      {
        return null;
      }

      IResourceAccessor accessor = TryOpenLocalFile(path);
      if (accessor == null)
        return null;

      try
      {
        IFileSystemResourceAccessor file = accessor as IFileSystemResourceAccessor;
        if (file == null || !file.Exists || !file.IsFile)
          return null;

        using (Stream body = file.OpenRead())
        using (MemoryStream copy = new MemoryStream())
        {
          body.CopyTo(copy);
          byte[] bytes = copy.ToArray();
          return bytes.Length > 0 ? bytes : null;
        }
      }
      catch (IOException)
      {
        return null;
      }
      finally
      {
        accessor.Dispose();
      }
    }
    /// <summary>
    /// Filing the seven answers of one title. Kinds the folders had nothing for are filed as "nothing", which is
    /// the answer a wall needs most, and kinds it did have are filed with what their file looked like at the
    /// time, so that a later question can tell whether the answer still stands.
    /// </summary>
    private void Remember(Guid id, Dictionary<string, string> found)
    {
      lock (this)
      {
        foreach (string kind in KINDS)
        {
          string file;
          found.TryGetValue(kind, out file);

          Answer answer = new Answer
          {
            Key = Key(id, kind),
            File = file,
            SaidAt = DateTimeOffset.UtcNow,
            Asked = ++_asked
          };

          // A picture this server cannot look at is not an answer worth keeping - the next question about it
          // searches again, which is what it would have done anyway.
          if (file != null && !LooksLike(answer, file))
            continue;

          _answers[answer.Key] = answer;
        }

        GiveRoom();
      }
    }

    /// <summary>
    /// Filing how a picture looks right now - when it was last written and how big it is - so that a later
    /// question can notice a replaced or deleted file. False when the file cannot be looked at at all.
    /// </summary>
    private static bool LooksLike(Answer answer, string serializedPath)
    {
      string local = LocalPathOf(serializedPath);
      if (local == null)
        return false;

      try
      {
        FileInfo info = new FileInfo(local);
        if (!info.Exists)
          return false;

        answer.Written = info.LastWriteTimeUtc;
        answer.Length = info.Length;
        return true;
      }
      catch (IOException)
      {
        return false;
      }
      catch (UnauthorizedAccessException)
      {
        return false;
      }
    }

    /// <summary>
    /// What the memory says about one kind of one title, and - when it says there is a picture - which file that
    /// is. An answer whose file went away or changed is given up here, so that the caller searches again.
    /// </summary>
    private Known Recall(Guid id, string kind, out string file)
    {
      lock (this)
      {
        file = null;

        Answer answer;
        if (!_answers.TryGetValue(Key(id, kind), out answer))
          return Known.NotRemembered;

        answer.Asked = ++_asked;

        if (answer.File == null)
        {
          if (DateTimeOffset.UtcNow - answer.SaidAt > NOTHING_LASTS)
          {
            _answers.Remove(answer.Key);
            return Known.NotRemembered;
          }

          return Known.NothingHere;
        }

        if (!LooksLike(answer, answer.File))
        {
          _answers.Remove(answer.Key);
          return Known.NotRemembered;
        }

        file = answer.File;
        return Known.Found;
      }
    }

    /// <summary>Dropping one answer, because the file it named turned out to be unreadable after all.</summary>
    private void Forget(Guid id, string kind)
    {
      lock (this)
      {
        _answers.Remove(Key(id, kind));
      }
    }

    private static string Key(Guid id, string kind)
    {
      return id.ToString("N") + "@" + kind;
    }

    /// <summary>
    /// Giving the memory room again by letting the answers nobody has asked about lately go. Titles a viewer is
    /// looking at are asked about and therefore stay; the ones a wall scrolled past an hour ago are the ones
    /// that leave.
    /// </summary>
    private void GiveRoom()
    {
      // Given up in bunches rather than one at a time, because sorting the memory is the expensive part of making
      // room and doing it per answer added would cost more than the walking the memory was added to skip.
      while (_answers.Count > Room)
      {
        List<string> going = _answers.Values
            // A guess that a title has nothing is the cheapest thing here to lose: it stands for fifteen minutes of
            // a folder listing, while the picture it would push out stands for a whole walk. So the guesses go first.
            .OrderBy(answer => answer.File == null ? 0 : 1)
            .ThenBy(answer => answer.Asked)
            .Take(GivingUpWhenFull)
            .Select(answer => answer.Key)
            .ToList();

        if (going.Count == 0)
          return;

        foreach (string key in going)
          _answers.Remove(key);
      }
    }
  }
}