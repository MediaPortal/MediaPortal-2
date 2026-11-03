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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Services.Imaging;
using MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TagLib;

namespace MediaPortal.Extensions.MetadataExtractors.AudioTagMetadataExtractor
{
  /// <summary>
  /// Puts the cover of an album into the server's fanart cache, taking it out of the album's own files.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <b>What this is for.</b> A track carries its cover with it - the picture an album ripper pasted into the ID3
  /// <c>APIC</c> frame or the FLAC <c>PICTURE</c> block - and until something reads that frame out and writes it
  /// where the picture endpoints look, a music shelf stays a wall of placeholders however much art the user's
  /// files hold. This is that something. It is the audio half of what the reference's
  /// <c>AudioMetadataExtractor</c> does in its own <c>AudioFanArtHandler</c>, read with the same managed tag
  /// reader this plugin already uses for names and dates, so that it runs wherever the server runs.
  /// </para>
  /// <para>
  /// <b>Why the album, and not the track.</b> A thousand-track library holds forty-odd covers; storing the
  /// picture under every track would write a hundred-odd megabytes of the same JPEG into the data folder to say
  /// what the album already says. So it is stored once, under the album's id - which is what the reference does -
  /// and a caller that asks a track for its cover is answered with the album's, by the <c>Track-&gt;Album</c>
  /// relationship the import wrote for that track. See <c>MediaController.GetFanArtOfType</c> for that step.
  /// </para>
  /// <para>
  /// <b>What it reads.</b> First the picture inside the file, because that one is certainly about this album: of
  /// the pictures in a tag the front cover is taken, along with the "other" type, which is what taggers that do
  /// not know the front-cover type write; a tag holding neither gives its first picture. That is the reference's
  /// own order. A file with nothing in it is asked a second time, from the folder it lies in - <c>folder.jpg</c>,
  /// <c>cover.jpg</c>, <c>poster.jpg</c> and the rest of the names the reference lists for a poster, which is
  /// what a library tagged by an outside tool like MusicBrainz Picard holds. An album with neither gives the
  /// shelf nothing, which is the truth, and is drawn as the skin's placeholder.
  /// </para>
  /// <para>
  /// <b>What it leaves alone.</b> The user's files are opened read-only and never saved, so a tag this plugin
  /// reads comes back unchanged. Nothing is fetched over the network: the reference's online lookups go through
  /// matchers a portable server does not carry, and a cover that has to be downloaded is one somebody has first
  /// to have found. A track that belongs to no album has no cover of its own to store.
  /// </para>
  /// <para>
  /// <b>What it also answers for.</b> The artist of the album. A person is only ever asked for its
  /// <c>Thumbnail</c>, the nfo plugin files an artist picture only when an <c>artist.nfo</c> names it by URL, and
  /// a hand-tagged library carries that picture as <c>folder.jpg</c> in the artist's own folder - so without this
  /// handler asking for it, no artist on a shelf had a face. See
  /// <see cref="ExtractArtistFolderPictures"/> for the reference's order of names.
  /// </para>
  /// </remarks>
  public class AudioTagFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { AudioAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the audio tag FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "6B0F1D1E-9F3C-4C1E-9C4E-2A6D1B7F0A55";

    /// <summary>
    /// Audio tag FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    /// <summary>
    /// The two names a cover is stored under.
    /// </summary>
    /// <remarks>
    /// <see cref="FanArtTypes.Cover"/> is the name music uses for its front picture - the reference renames a
    /// poster to it as soon as it knows the item is an album - and <see cref="FanArtTypes.Poster"/> is the name
    /// the endpoints and the shelves that were written for films already ask for. The same bytes go under both,
    /// which costs one extra file per album and spares every caller the question of which name music happens to
    /// use for the same picture.
    /// </remarks>
    private static readonly string[] COVER_TYPES = { FanArtTypes.Cover, FanArtTypes.Poster };

    /// <summary>
    /// The music reader's settings, read once and kept up to date while the server runs.
    /// </summary>
    private SettingsChangeWatcher<AudioTagMetadataExtractorSettings> _settingsWatcher;

    #endregion

    #region Construction

    public AudioTagFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "Audio Tag FanArt Handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Whether the pictures lying in an album's folder are to be copied into the cache.
    /// </summary>
    /// <param name="locator">Where one of the album's tracks stands.</param>
    /// <returns><c>true</c> when the share the track stands on is one the household has asked to be copied.</returns>
    /// <remarks>
    /// The same question the reference asks before it walks a folder, answered by the two switches the music
    /// reader carries: one for a disk plugged into this machine, one for a share that might be asleep later.
    /// </remarks>
    protected bool CachesFolderPictures(IResourceLocator locator)
    {
      AudioTagMetadataExtractorSettings settings = GetSettings();
      return ShouldCacheLocalFanArt(locator.NativeResourcePath, settings.CacheLocalFanArt,
          settings.CacheOfflineFanArt);
    }

    /// <summary>
    /// The music reader's settings, watched, so that a switch thrown on the settings screen is seen by the next
    /// album the importer reaches rather than waiting for the server to come back.
    /// </summary>
    protected virtual AudioTagMetadataExtractorSettings GetSettings()
    {
      SettingsChangeWatcher<AudioTagMetadataExtractorSettings> watcher = _settingsWatcher;
      if (watcher == null)
      {
        watcher = new SettingsChangeWatcher<AudioTagMetadataExtractorSettings>();
        SettingsChangeWatcher<AudioTagMetadataExtractorSettings> existing =
            Interlocked.CompareExchange(ref _settingsWatcher, watcher, null);
        if (existing != null)
        {
          watcher.Dispose();
          watcher = existing;
        }
      }
      return watcher.Settings;
    }

    #endregion

    #region IMediaFanArtHandler implementation

    /// <summary>
    /// Storing what the album the given track carries and what lies in its folder, unless it is stored already.
    /// </summary>
    /// <param name="mediaItemId">Id of the track the library is asking about.</param>
    /// <param name="aspects">The track's aspects, including the relationship that names its album.</param>
    /// <returns>A <see cref="Task"/> that completes when the album's pictures are in the cache or are known not to
    /// exist.</returns>
    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      if (!aspects.ContainsKey(AudioAspect.ASPECT_ID))
        return;

      // A virtual resource is a title the library only knows about - a stream, or an entry of an online library -
      // so there is no file here that a picture could be taken out of.
      if (BaseInfo.IsVirtualResource(aspects))
        return;

      IResourceLocator locator = GetResourceLocator(aspects);
      if (locator == null)
        return;

      if (!RelationshipExtractorUtils.TryGetLinkedId(AudioAlbumAspect.ROLE_ALBUM, aspects, out Guid albumMediaItemId))
        return;

      // One album is looked at once per run. A folder of a thousand tracks would otherwise open a thousand files
      // to write the same picture, and the nine hundred and ninety nine that follow learn nothing new - including
      // the tracks of an album that turned out to hold no picture at all, which is the case worth remembering.
      if (!AddToCache(albumMediaItemId))
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();

      TrackInfo trackInfo = new TrackInfo();
      trackInfo.FromMetadata(aspects);
      AlbumInfo albumInfo = trackInfo.CloneBasicInstance<AlbumInfo>();
      string albumTitle = albumInfo.ToString();

      // The reference's order: what the file carries, and only if it carries nothing, what the folder it lies in
      // carries.
      bool hasCover = fanArtCache.GetFanArtFiles(albumMediaItemId, FanArtTypes.Cover).Count > 0;
      if (!hasCover)
      {
        await ExtractTagCover(locator, fanArtCache, albumMediaItemId, albumTitle).ConfigureAwait(false);
        hasCover = fanArtCache.GetFanArtFiles(albumMediaItemId, FanArtTypes.Cover).Count > 0;
      }

      // Whatever else the folder holds - the disc of the release, the title's logo without a background, the
      // wide banner - is stored whether the cover came out of the file or not, and whether or not a screen
      // happens to show those today. What a household does with a picture it keeps is its own question; this
      // server's job is to have it there when the question is asked.
      if (CachesFolderPictures(locator))
      {
        ResourcePath albumDirectory = AlbumFolderOf(locator.NativeResourcePath, albumTitle);
        await ExtractAlbumFolderCover(locator, fanArtCache, albumMediaItemId, albumTitle, hasCover)
            .ConfigureAwait(false);
        await ExtractArtistFolderPictures(locator, fanArtCache, aspects, albumDirectory).ConfigureAwait(false);
      }
    }

    /// <summary>
    /// Forgetting that a picture was ever looked for, and dropping what was stored for the id - an album whose
    /// files were retagged holds a different cover now, and an album deleted from the share has no files left.
    /// </summary>
    /// <param name="mediaItemId">Id of the media item whose pictures are gone.</param>
    public override void DeleteFanArt(Guid mediaItemId)
    {
      //base method removes the id from the cache
      base.DeleteFanArt(mediaItemId);
      ServiceRegistration.Get<IFanArtCache>().DeleteFanArtFiles(mediaItemId);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Storing the picture carried inside the file itself.
    /// </summary>
    /// <param name="locator">Where the track's file is.</param>
    /// <param name="fanArtCache">The cache the picture goes into.</param>
    /// <param name="albumMediaItemId">Id of the album the picture belongs to.</param>
    /// <param name="albumTitle">The album's name, which is what the cache writes beside the picture.</param>
    /// <returns>A <see cref="Task"/> that completes when the file has been read.</returns>
    protected virtual async Task ExtractTagCover(IResourceLocator locator, IFanArtCache fanArtCache,
        Guid albumMediaItemId, string albumTitle)
    {
      try
      {
        using (IResourceAccessor accessor = locator.CreateAccessor())
        {
          IPicture picture = ReadTagPicture(accessor as IFileSystemResourceAccessor);
          if (picture == null)
            return;

          string filename = ResourcePath.GetFileNameWithoutExtension(locator.NativeResourcePath);
          await SavePicture(fanArtCache, albumMediaItemId, albumTitle, picture.Data.Data, filename)
              .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        // A file that will not open is a file without a cover in it as far as a shelf is concerned. The import has
        // already read this file's tags for its names and dates; all that is left here is its picture, and a shelf
        // missing one picture is drawn with the skin's placeholder.
        Logger.Warn("{0}: Could not read the picture in '{1}'", ex, Metadata.Name, locator.NativeResourcePath);
      }
    }

    /// <summary>
    /// Storing the pictures lying in the folder the album's files lie in.
    /// </summary>
    /// <param name="locator">Where one of the album's tracks is.</param>
    /// <param name="fanArtCache">The cache the pictures go into.</param>
    /// <param name="albumMediaItemId">Id of the album the pictures belong to.</param>
    /// <param name="albumTitle">The album's name, which is what the cache writes beside the pictures.</param>
    /// <param name="coverAlreadyStored"><c>true</c> when the front picture is already in the cache - from the
    /// tag of a file, or from an earlier run - in which case the walk stores everything the folder holds except
    /// another front picture.</param>
    /// <returns>A <see cref="Task"/> that completes when the folder has been walked.</returns>
    /// <remarks>
    /// The walk is the reference's: the folder the file lies in, unless that folder is one disc of a set - a
    /// folder called <c>CD1</c> holds a disc, while the album's art lies one level up - and whatever it holds
    /// under the names the reference lists for each kind. A poster of an album is called a cover, because that is
    /// what the reference renames it to once it knows the item is an album, and it is filed under both names so
    /// that a shelf written for films finds it too.
    /// <para>
    /// Every kind the folder names is stored, not only the front picture: a media manager leaves
    /// <c>disc.png</c>, <c>clearlogo.png</c> and <c>clearart.png</c> in half of the album folders on the shelves
    /// this was measured on, and whether a household wants a screen to show them is a question it answers for
    /// itself later. Throwing them away during the import is the one answer that cannot be taken back.
    /// </para>
    /// </remarks>
    protected virtual async Task ExtractAlbumFolderCover(IResourceLocator locator, IFanArtCache fanArtCache,
        Guid albumMediaItemId, string albumTitle, bool coverAlreadyStored = false)
    {
      ResourcePath albumDirectory = AlbumFolderOf(locator.NativeResourcePath, albumTitle);

      try
      {
        FanArtPathCollection paths = new FanArtPathCollection();
        using (IResourceAccessor accessor = new ResourceLocator(locator.NativeSystemId, albumDirectory).CreateAccessor())
        {
          IFileSystemResourceAccessor folder = accessor as IFileSystemResourceAccessor;
          if (folder == null || folder.IsFile || !folder.Exists)
            return;

          ExtractAllFanArtImages(LocalFanartHelper.GetPotentialFanArtFiles(folder), paths);
          if (folder.ResourceExists("ExtraFanArt/"))
          {
            using (IResourceAccessor extra = folder.GetResource("ExtraFanArt/"))
            {
              IFileSystemResourceAccessor extraFolder = extra as IFileSystemResourceAccessor;
              if (extraFolder != null)
                paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFolder));
            }
          }
        }

        // The album's front picture is a cover, whatever the file happens to be called.
        if (paths.Paths.TryGetValue(FanArtTypes.Poster, out List<ResourcePath> posters))
        {
          paths.Paths.Remove(FanArtTypes.Poster);
          paths.AddRange(FanArtTypes.Cover, posters);
        }

        foreach (KeyValuePair<string, List<ResourcePath>> typePaths in paths)
        {
          if (typePaths.Value.Count == 0)
            continue;
          if (coverAlreadyStored && COVER_TYPES.Contains(typePaths.Key))
            continue;
          await fanArtCache.TrySaveFanArt(albumMediaItemId, albumTitle, typePaths.Key, typePaths.Value,
              (saveDirectory, path) => TrySaveFolderImage(locator.NativeSystemId, path, saveDirectory))
              .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("{0}: Could not read the pictures in '{1}'", ex, Metadata.Name, albumDirectory);
      }
    }

    /// <summary>
    /// Storing the pictures lying in the folder of the album's artist, under the artist's own id.
    /// </summary>
    /// <param name="locator">Where one of the album's tracks is.</param>
    /// <param name="fanArtCache">The cache the pictures go into.</param>
    /// <param name="aspects">The track's aspects, which name its album artists and hold their ids.</param>
    /// <param name="albumDirectory">The folder the album's files lie in.</param>
    /// <returns>A <see cref="Task"/> that completes when the artist's folder has been walked.</returns>
    /// <remarks>
    /// <para>
    /// <b>Why the music reader answers for a person.</b> A person's face is asked for as a
    /// <c>Thumbnail</c> and as nothing else - the skin's person tile reads
    /// <c>FanArtType="Thumbnail"</c> (themes/default/styles/CurrentlyPlayingStyles.xaml:220-222) - and the
    /// reference answered that question for an artist at the moment it was asked, out of the folder one of the
    /// artist's tracks stands in two levels below (<c>LocalArtistFanartProvider.cs:59-82</c> and
    /// <c>:160-186</c>). A server that only answers from its cache has to have been told the picture exists
    /// before anybody asks, and nothing in a library tagged by hand ever told it: the nfo plugin files the
    /// pictures an <c>artist.nfo</c> names by URL, and an <c>artist.nfo</c> that only carries a biography names
    /// none. The result, measured on a library of thirty-nine artists: one artist with a face.
    /// </para>
    /// <para>
    /// <b>Which file is the face.</b> The reference's order for an artist: the folder's own <c>artist.*</c>
    /// first, then <c>poster|folder|cover|keyart.*</c>, then <c>thumb|landscape.*</c>. The middle group is what
    /// matters in practice, because <c>folder.jpg</c> beside an artist's albums is exactly what Kodi, MusicBrainz
    /// Picard and Media-Buddy write for an artist; it is filed as the artist's thumbnail here, while the album
    /// two levels below keeps its own <c>folder.jpg</c> as its cover.
    /// </para>
    /// <para>
    /// <b>Why the folder has to be named after the artist.</b> The reference could afford to take whatever the
    /// parent folder held because it looked at request time, at one known track of that artist. Storing the same
    /// answer during the import means the walk has to be sure the folder it is standing in is the artist's and
    /// not, say, the library root of a various-artist compilation - whose <c>folder.jpg</c> would otherwise be
    /// filed as the face of whichever artist the import happened to reach first. An artist whose files do not
    /// stand under a folder of its own name is left without a picture, which is the truth about that layout.
    /// </para>
    /// </remarks>
    protected virtual async Task ExtractArtistFolderPictures(IResourceLocator locator, IFanArtCache fanArtCache,
        IDictionary<Guid, IList<MediaItemAspect>> aspects, ResourcePath albumDirectory)
    {
      IList<Tuple<Guid, string>> artists = AlbumArtists(aspects);
      if (artists == null || artists.Count == 0)
        return;

      ResourcePath artistDirectory = ResourcePathHelper.Combine(albumDirectory, "../");
      Tuple<Guid, string> artist = artists.FirstOrDefault(a => string.Equals(a.Item2, artistDirectory.FileName,
          StringComparison.InvariantCultureIgnoreCase));
      if (artist == null)
        return;

      // One artist is looked at once per run, like one album: an artist of ten albums would otherwise open the
      // same ten files to write the same picture.
      if (!AddToCache(artist.Item1))
        return;

      FanArtPathCollection paths = new FanArtPathCollection();
      AddArtistPictures(paths, locator, artistDirectory);
      TryAddCentralArtistPictures(paths, locator, artist);
      if (paths.Paths.Count == 0)
        return;

      try
      {
        foreach (KeyValuePair<string, List<ResourcePath>> typePaths in paths)
        {
          if (typePaths.Value.Count == 0)
            continue;
          await fanArtCache.TrySaveFanArt(artist.Item1, artist.Item2, typePaths.Key, typePaths.Value,
              (saveDirectory, path) => TrySaveFolderImage(locator.NativeSystemId, path, saveDirectory))
              .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("{0}: Could not store the pictures in '{1}'", ex, Metadata.Name, artistDirectory);
      }
    }

    /// <summary>
    /// The album artists the track's aspects name, with the id each of them has in the library.
    /// </summary>
    /// <param name="aspects">The track's aspects.</param>
    /// <returns>Pairs of artist id and artist name, or <c>null</c> when the track names none.</returns>
    /// <remarks>
    /// The names come from the track's own tags and the ids from the relationships the import wrote for them,
    /// which is how the reference's own music handler paired them before it walked an artist folder.
    /// </remarks>
    protected static IList<Tuple<Guid, string>> AlbumArtists(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Tuple<Guid, string>> artists = null;
      if (MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out SingleMediaItemAspect audio))
      {
        IList<string> names = audio.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS)?.ToList();
        if (names != null && names.Count > 0)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ALBUMARTIST, aspects, names, out artists);
      }
      return artists;
    }

    /// <summary>
    /// Adding every picture of an artist that a folder holds to the collection of paths to store.
    /// </summary>
    /// <param name="paths">Collection the found pictures are added to, in the order a thumbnail is filled.</param>
    /// <param name="locator">Where the artist's files are reached from; used to open the folder.</param>
    /// <param name="artistDirectory">The artist's folder.</param>
    protected void AddArtistPictures(FanArtPathCollection paths, IResourceLocator locator, ResourcePath artistDirectory)
    {
      try
      {
        using (IResourceAccessor accessor = new ResourceLocator(locator.NativeSystemId, artistDirectory).CreateAccessor())
        {
          IFileSystemResourceAccessor folder = accessor as IFileSystemResourceAccessor;
          if (folder == null || folder.IsFile || !folder.Exists)
            return;

          List<ResourcePath> candidates = LocalFanartHelper.GetPotentialFanArtFiles(folder);

          // The face, in the reference's order: the artist's own picture, then the folder's front picture,
          // then a wide picture of the band. The cache keeps one thumbnail, so what is offered first is what a
          // tile shows.
          paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.ARTIST_FILENAMES));
          paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.POSTER_FILENAMES));
          paths.AddRange(FanArtTypes.Thumbnail, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.THUMB_FILENAMES));

          // The rest of what the same reference provider asks an artist's folder for, so that a household which
          // keeps banners and logos has them in the cache when it builds a screen for them.
          paths.AddRange(FanArtTypes.Banner, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.BANNER_FILENAMES));
          paths.AddRange(FanArtTypes.Logo, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.LOGO_FILENAMES));
          paths.AddRange(FanArtTypes.ClearArt, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.CLEARART_FILENAMES));
          paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.FilterPotentialFanArtFilesByPrefix(candidates,
              LocalFanartHelper.BACKDROP_FILENAMES));

          if (folder.ResourceExists("ExtraFanArt/"))
          {
            using (IResourceAccessor extra = folder.GetResource("ExtraFanArt/"))
            {
              IFileSystemResourceAccessor extraFolder = extra as IFileSystemResourceAccessor;
              if (extraFolder != null)
                paths.AddRange(FanArtTypes.FanArt, LocalFanartHelper.GetPotentialFanArtFiles(extraFolder));
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("{0}: Could not read the pictures in '{1}'", ex, Metadata.Name, artistDirectory);
      }
    }

    /// <summary>
    /// Adding the pictures of a household's central artist folder, where pictures that do not belong to one
    /// share are kept.
    /// </summary>
    /// <param name="paths">Collection the found pictures are added to.</param>
    /// <param name="locator">Where the artist's files are reached from.</param>
    /// <param name="artist">Pair of artist id and artist name.</param>
    /// <remarks>
    /// Pictures from here are added <em>after</em> the artist's own folder, so that a picture the household put
    /// next to the music wins over a central collection of everything. The reference read the central folder the
    /// same way round (<c>LocalArtistFanartProvider.cs:221</c> onwards).
    /// </remarks>
    protected void TryAddCentralArtistPictures(FanArtPathCollection paths, IResourceLocator locator,
        Tuple<Guid, string> artist)
    {
      try
      {
        ResourcePath centralFolder = LocalFanartHelper.GetCentralPersonFolder(
            ResourcePathHelper.Combine(locator.NativeResourcePath, "../../"), CentralPersonFolderType.AudioArtists);
        if (centralFolder == null)
          return;

        AddArtistPictures(paths, locator,
            ResourcePathHelper.Combine(centralFolder, LocalFanartHelper.GetSafePersonFolderName(artist.Item2) + "/"));
      }
      catch (Exception ex)
      {
        Logger.Warn("{0}: Could not read the central artist folder for '{1}'", ex, Metadata.Name, artist.Item2);
      }
    }

    /// <summary>
    /// The folder an album's files lie in, one level above a disc folder of a set.
    /// </summary>
    /// <param name="trackPath">Where one of the album's tracks is.</param>
    /// <param name="albumTitle">The album's name.</param>
    /// <returns>The folder to walk for the album's pictures.</returns>
    internal static ResourcePath AlbumFolderOf(ResourcePath trackPath, string albumTitle)
    {
      ResourcePath albumDirectory = ResourcePathHelper.Combine(trackPath, "../");
      if (IsDiscFolder(albumTitle, albumDirectory.FileName))
        albumDirectory = ResourcePathHelper.Combine(albumDirectory, "../");
      return albumDirectory;
    }

    /// <summary>
    /// The cover carried in an audio file's tags, or <c>null</c> when the file holds no picture or is not a file.
    /// </summary>
    /// <param name="fsra">The audio file, or <c>null</c> when the caller's accessor does not reach the disc.</param>
    /// <returns>The picture to store, picked in the reference's order of preference.</returns>
    internal static IPicture ReadTagPicture(IFileSystemResourceAccessor fsra)
    {
      if (fsra == null)
        return null;

      using (TagLib.File tag = AudioTags.LoadTag(fsra))
      {
        IPicture[] pictures = tag?.Tag?.Pictures;
        if (pictures == null || pictures.Length == 0)
          return null;

        // The front cover is the album's picture; "other" is what a tagger writes when it does not know the
        // front-cover type, which happens often enough that the reference takes it for a cover as well. A tag
        // holding neither - a band logo and a back cover, say - still gives its first picture, because a shelf
        // wearing the wrong cover is nearer the truth than a shelf wearing none.
        IPicture cover = Array.Find(pictures, picture =>
            picture.Type == PictureType.FrontCover || picture.Type == PictureType.Other);
        return cover ?? pictures[0];
      }
    }

    /// <summary>
    /// Whether a folder is one disc of a set rather than the album itself.
    /// </summary>
    /// <param name="album">The album's name.</param>
    /// <param name="albumFolder">The name of the folder the track was found in.</param>
    /// <returns><c>true</c> when the folder is a disc of a set whose album is not itself named like one.</returns>
    /// <remarks>
    /// The reference's own test of the same name, including the second half of each comparison, which is what
    /// keeps a genuine album honest: <c>CD</c> is the name of an album by Rammstein, and a folder of that name
    /// should not be walked past on the way up.
    /// </remarks>
    internal static bool IsDiscFolder(string album, string albumFolder)
    {
      if (album == null || albumFolder == null)
        return false;

      return (albumFolder.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase)
              && !album.StartsWith("CD", StringComparison.InvariantCultureIgnoreCase))
          || (albumFolder.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase)
              && !album.StartsWith("Disc", StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Writing the bytes of a cover into the cache, under both names a cover is asked for by.
    /// </summary>
    /// <param name="fanArtCache">The cache to write into.</param>
    /// <param name="albumMediaItemId">Id of the album the picture belongs to.</param>
    /// <param name="albumTitle">The album's name.</param>
    /// <param name="data">The picture, as its file holds it.</param>
    /// <param name="filename">Name to give the stored file, without an extension.</param>
    /// <returns>A <see cref="Task"/> that completes when both names have been answered.</returns>
    protected async Task SavePicture(IFanArtCache fanArtCache, Guid albumMediaItemId, string albumTitle,
        byte[] data, string filename)
    {
      if (data == null || data.Length == 0)
        return;

      foreach (string coverType in COVER_TYPES)
        await fanArtCache.TrySaveFanArt(albumMediaItemId, albumTitle, coverType,
            saveDirectory => Task.FromResult(TrySavePicture(data, saveDirectory, filename))).ConfigureAwait(false);
    }

    /// <summary>
    /// Writing one picture into the directory the cache offered, keeping the format the bytes are already in.
    /// </summary>
    /// <param name="data">The picture.</param>
    /// <param name="saveDirectory">The cache's directory for this id and this kind.</param>
    /// <param name="filename">Name to give the file, without an extension.</param>
    /// <returns><c>true</c> when the picture is in the folder now.</returns>
    /// <remarks>
    /// <para>
    /// The base class has a method that does this, but it hands the bytes to the <see cref="IImageTransformer"/>
    /// service to be re-encoded as JPEG first - and that service's only implementation is the one built on GDI+,
    /// so a server running somewhere else has nothing to answer the call. Re-encoding is not what a cover needs
    /// anyway: it arrives as a JPEG already, at a size a shelf can draw.
    /// </para>
    /// <para>
    /// The extension says what the bytes are, because that is how the cache lists its files back - it looks for
    /// <c>.jpg</c>, <c>.png</c> and <c>.tbn</c> - so anything that is neither of the two known formats is filed
    /// under the third rather than lying about what it is.
    /// </para>
    /// </remarks>
    internal static bool TrySavePicture(byte[] data, string saveDirectory, string filename)
    {
      string savePath = Path.Combine(saveDirectory, "File." + filename + PictureExtension(data));
      try
      {
        if (System.IO.File.Exists(savePath))
          return false;
        System.IO.File.WriteAllBytes(savePath, data);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Warn("AudioTagFanArtHandler: Could not write the cover to '{0}'", ex, savePath);
        return false;
      }
    }

    /// <summary>
    /// The extension that matches a picture's bytes, judged by the first of them.
    /// </summary>
    /// <param name="data">The picture's bytes.</param>
    /// <returns><c>.png</c>, <c>.jpg</c>, or <c>.tbn</c> for a format the cache lists without naming.</returns>
    internal static string PictureExtension(byte[] data)
    {
      if (data.Length > 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        return ".png";
      if (data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8)
        return ".jpg";
      return ".tbn";
    }

    #endregion
  }
}
