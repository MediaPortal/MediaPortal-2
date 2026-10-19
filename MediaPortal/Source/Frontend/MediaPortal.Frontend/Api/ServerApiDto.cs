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
using System.Text.Json.Serialization;

namespace MediaPortal.Frontend.Api
{
  /// <summary>
  /// The shapes the server's HTTP API writes, as the front end sees them.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These are copies, not the server's types, and that is the point. The server hands out
  /// <c>MediaItemAspectMetadata</c>-shaped JSON but never the type itself, so a client that wants the media
  /// library has to be built against a wire contract instead of against an assembly - which is the only way it
  /// can run on a platform where the server's assemblies do not exist.
  /// </para>
  /// <para>
  /// Property names are pinned with <see cref="JsonPropertyNameAttribute"/> rather than left to a naming policy.
  /// A naming policy makes the client's property names and the wire's spelling agree by convention, and a
  /// convention nobody can see is a quiet way for a field to stop arriving. The attribute is the statement:
  /// this JSON key fills this property.
  /// </para>
  /// </remarks>
  public record HealthInfo(
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("version")] string Version,
      [property: JsonPropertyName("utc")] string? Utc,
      [property: JsonPropertyName("pathBase")] string? PathBase,
      [property: JsonPropertyName("path")] string? Path);

  /// <summary>
  /// What the server said about itself at <c>GET api/serverinfo</c>.
  /// </summary>
  public record ServerInfo(
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("version")] string Version,
      [property: JsonPropertyName("framework")] string? Framework,
      [property: JsonPropertyName("operatingSystem")] string? OperatingSystem,
      [property: JsonPropertyName("osArchitecture")] string? OsArchitecture,
      [property: JsonPropertyName("processArchitecture")] string? ProcessArchitecture,
      [property: JsonPropertyName("processId")] int ProcessId,
      [property: JsonPropertyName("serverUtc")] string? ServerUtc,
      [property: JsonPropertyName("pathBase")] string? PathBase,
      [property: JsonPropertyName("ipAddressBindings")] IReadOnlyList<string>? IpAddressBindings,
      [property: JsonPropertyName("useIPv4")] bool UseIPv4,
      [property: JsonPropertyName("useIPv6")] bool UseIPv6)
  {
    /// <summary>
    /// One line for a status bar: who is there and what they run on.
    /// </summary>
    public string Describe()
    {
      return string.Format("{0} {1} - {2}, {3}/{4}",
          Name, Version, Framework, OperatingSystem, ProcessArchitecture);
    }
  }

  /// <summary>
  /// One attribute of a media item aspect, from <c>GET api/library/aspects</c>.
  /// </summary>
  public record AspectAttribute(
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("type")] string? Type,
      [property: JsonPropertyName("indexed")] bool Indexed,
      [property: JsonPropertyName("collection")] bool Collection);

  /// <summary>
  /// One media item aspect: the unit the server's media database stores and sorts by.
  /// </summary>
  public record MediaAspect(
      [property: JsonPropertyName("id")] Guid Id,
      [property: JsonPropertyName("name")] string Name,
      [property: JsonPropertyName("systemAspect")] bool SystemAspect,
      [property: JsonPropertyName("transient")] bool Transient,
      [property: JsonPropertyName("referencingAspectId")] Guid? ReferencingAspectId,
      [property: JsonPropertyName("attributes")] IReadOnlyList<AspectAttribute> Attributes);

  /// <summary>
  /// The answer of <c>GET api/library/aspects</c>.
  /// </summary>
  public record LibraryAspects(
      [property: JsonPropertyName("count")] int Count,
      [property: JsonPropertyName("aspects")] IReadOnlyList<MediaAspect> Aspects);

  /// <summary>
  /// The answer of <c>GET api/library/count</c>.
  /// </summary>
  public record MediaCount(
      [property: JsonPropertyName("aspect")] string? Aspect,
      [property: JsonPropertyName("aspectId")] Guid? AspectId,
      [property: JsonPropertyName("count")] int Count);

  /// <summary>
  /// One title from <c>GET api/media/titles</c> - one row of the list a viewer picks from.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <paramref name="Path"/> is the item's primary resource in the server's own serialization
  /// (<c>{provider-id}://path</c>), not a file name the server invented for the client. It stays in that form on
  /// purpose: the answer to "can this be watched here?" depends on where the client is, and a server that had
  /// already decided it would have had to guess.
  /// </para>
  /// <para>
  /// <paramref name="Title"/> is nullable because a title list contains items nobody ever named - an import that
  /// found a file it could not identify leaves one behind. A screen showing the empty string looks like a bug;
  /// a screen showing "Untitled" says what happened.
  /// </para>
  /// </remarks>
  public record MediaTitle(
      [property: JsonPropertyName("id")] Guid Id,
      [property: JsonPropertyName("title")] string? Title,
      [property: JsonPropertyName("sortTitle")] string? SortTitle,
      [property: JsonPropertyName("year")] int? Year,
      [property: JsonPropertyName("runtimeMinutes")] int? RuntimeMinutes,
      [property: JsonPropertyName("path")] string? Path,
      [property: JsonPropertyName("stub")] bool Stub,
      [property: JsonPropertyName("playCount")] int? PlayCount = null,
      [property: JsonPropertyName("lastPlayed")] DateTime? LastPlayed = null,
      [property: JsonPropertyName("resume")] int? Resume = null,
      [property: JsonPropertyName("season")] int? Season = null,
      [property: JsonPropertyName("episode")] int? Episode = null,
      [property: JsonPropertyName("track")] int? Track = null,
      [property: JsonPropertyName("artist")] string? Artist = null,
      [property: JsonPropertyName("trackCount")] int? TrackCount = null);

  /// <summary>
  /// The answer of <c>GET api/media/titles</c>: one page of titles plus what is left behind it.
  /// </summary>
  /// <remarks>
  /// <paramref name="Sort"/> is echoed by the server and is what a screen says out loud when a viewer wonders why
  /// the list is in the order it is; <paramref name="Error"/> is only set on an empty answer that has a reason
  /// the caller can act on.
  /// </remarks>
  public record MediaTitles(
      [property: JsonPropertyName("aspect")] string? Aspect,
      [property: JsonPropertyName("aspectId")] Guid? AspectId,
      [property: JsonPropertyName("total")] int Total,
      [property: JsonPropertyName("offset")] int Offset,
      [property: JsonPropertyName("limit")] int Limit,
      [property: JsonPropertyName("returned")] int Returned,
      [property: JsonPropertyName("sort")] string? Sort,
      [property: JsonPropertyName("titles")] IReadOnlyList<MediaTitle>? Titles,
      [property: JsonPropertyName("error")] string? Error,
      [property: JsonPropertyName("search")] string? Search = null,
      [property: JsonPropertyName("seen")] string? Seen = null,
      [property: JsonPropertyName("inside")] Guid? Inside = null)
  {
    /// <summary>
    /// Titles the server has for this aspect beyond the page that arrived.
    /// </summary>
    public int Remaining
    {
      get { return Math.Max(Total - Offset - Returned, 0); }
    }
  }

  /// <summary>
  /// One title as <c>GET api/media/items/{id}</c> answers it: what the library knows beyond the row in a list.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Every field is nullable because that is what the library's own data looks like: a film imported from a bare
  /// file name has a title and a path and nothing else, and a details screen that was given empty strings would
  /// render a blank line where the truth is "this server was never told". <paramref name="Overview"/> and
  /// <paramref name="StoryPlot"/> are two fields because MP2 keeps two fields - the comment on the Media aspect
  /// and the plot on the Video aspect - and one title commonly has one and not the other.
  /// </para>
  /// <para>
  /// There are no genres and no people here, and that is the server's decision, not an omission here: MP2 files
  /// those as separate items linked by relationships, so naming them would mean a query per title.
  /// </para>
  /// </remarks>
  public record MediaItemDetails(
      [property: JsonPropertyName("id")] Guid Id,
      [property: JsonPropertyName("title")] string? Title,
      [property: JsonPropertyName("sortTitle")] string? SortTitle,
      [property: JsonPropertyName("year")] int? Year,
      [property: JsonPropertyName("runtimeMinutes")] int? RuntimeMinutes,
      [property: JsonPropertyName("overview")] string? Overview,
      [property: JsonPropertyName("storyPlot")] string? StoryPlot,
      [property: JsonPropertyName("tagline")] string? Tagline,
      [property: JsonPropertyName("certification")] string? Certification,
      [property: JsonPropertyName("collection")] string? Collection,
      [property: JsonPropertyName("rating")] int? Rating,
      [property: JsonPropertyName("seriesName")] string? SeriesName,
      [property: JsonPropertyName("season")] int? Season,
      [property: JsonPropertyName("episode")] int? Episode,
      [property: JsonPropertyName("episodeName")] string? EpisodeName,
      [property: JsonPropertyName("playCount")] int? PlayCount,
      [property: JsonPropertyName("lastPlayed")] DateTime? LastPlayed,
      [property: JsonPropertyName("path")] string? Path,
      [property: JsonPropertyName("stub")] bool Stub,
      [property: JsonPropertyName("error")] string? Error)
  {
    /// <summary>
    /// The longest single line of prose the title carries, or null when it carries none. What a details screen
    /// puts in the middle of the screen, decided here so that two screens do not pick different fields and then
    /// disagree about what a film is about.
    /// </summary>
    public string? Description
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(Overview))
          return Overview!.Trim();
        return string.IsNullOrWhiteSpace(StoryPlot) ? null : StoryPlot!.Trim();
      }
    }
  }

  /// <summary>
  /// The answer of <c>POST api/media/items/{id}/playback</c>: not an echo of what was reported, but what the
  /// library holds afterwards.
  /// </summary>
  /// <remarks>
  /// A screen that reported "finished" can put the server's own count on the row it is looking at, instead of
  /// the number it hoped for. <paramref name="Error"/> is only set on an answer that did not record anything.
  /// </remarks>
  public record MediaPlayback(
      [property: JsonPropertyName("id")] Guid Id,
      [property: JsonPropertyName("playCount")] int PlayCount,
      [property: JsonPropertyName("lastPlayed")] DateTime? LastPlayed,
      [property: JsonPropertyName("watched")] bool Watched,
      [property: JsonPropertyName("error")] string? Error,
      [property: JsonPropertyName("resume")] int? Resume = null);

  /// <summary>
  /// One folder the server builds its library from, from <c>GET api/media/shares</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <paramref name="Path"/> is the folder in the server's own filesystem notation and can be null: a share can
  /// point somewhere that is not a local directory (another machine, another provider), and then
  /// <paramref name="ResourcePath"/> is the only true thing that can be shown about it. A screen that printed the
  /// empty string for those would print "this folder is nowhere".
  /// </para>
  /// <para>
  /// <paramref name="Local"/> is not decoration. A share on another system cannot be imported by this server, and
  /// a screen that offered a "re-import" button for it would be offering a button whose only answer is a refusal.
  /// </para>
  /// </remarks>
  public record ShareInfo(
      [property: JsonPropertyName("id")] Guid Id,
      [property: JsonPropertyName("name")] string? Name,
      [property: JsonPropertyName("path")] string? Path,
      [property: JsonPropertyName("resourcePath")] string? ResourcePath,
      [property: JsonPropertyName("systemId")] string? SystemId,
      [property: JsonPropertyName("local")] bool Local,
      [property: JsonPropertyName("mediaCategories")] IReadOnlyList<string>? MediaCategories,
      [property: JsonPropertyName("useShareWatcher")] bool UseShareWatcher);

  /// <summary>
  /// The answer of <c>GET api/media/shares</c>: the folders, and the kinds of media a new one can be told about.
  /// </summary>
  /// <remarks>
  /// The categories arrive with the shares on purpose. They are what the server's extractors registered
  /// themselves under, so a screen that hard-coded "Movie, Series, Audio" would offer choices this particular
  /// server may not be able to import - and a folder filed under a category nobody reads comes back empty.
  /// </remarks>
  public record SharesAnswer(
      [property: JsonPropertyName("count")] int Count,
      [property: JsonPropertyName("shares")] IReadOnlyList<ShareInfo>? Shares,
      [property: JsonPropertyName("mediaCategories")] IReadOnlyList<string>? MediaCategories);

  /// <summary>
  /// One job the importer still holds, from <c>GET api/media/import</c>.
  /// </summary>
  public record ImportJob(
      [property: JsonPropertyName("type")] string? Type,
      [property: JsonPropertyName("path")] string? Path,
      [property: JsonPropertyName("includeSubDirectories")] bool IncludeSubDirectories);

  /// <summary>
  /// The answer of <c>GET api/media/import</c>.
  /// </summary>
  /// <remarks>
  /// <paramref name="Pending"/> counts jobs, not files - one job is one folder tree and can hold a hundred
  /// thousand files - so a screen says "still importing" rather than a percentage it was never told.
  /// <paramref name="Suspended"/> is the state in which the server holds its jobs and does not work on them; a
  /// queue that never empties and is suspended is a different complaint from one that is merely long.
  /// </remarks>
  public record ImportStatus(
      [property: JsonPropertyName("pending")] int Pending,
      [property: JsonPropertyName("suspended")] bool Suspended,
      [property: JsonPropertyName("jobs")] IReadOnlyList<ImportJob>? Jobs)
  {
    /// <summary>
    /// What a status line says about the queue.
    /// </summary>
    public string Describe()
    {
      if (Suspended && Pending > 0)
        return string.Format("{0} folder(s) waiting, and the importer is paused", Pending);
      if (Pending > 0)
        return string.Format("importing {0} folder(s)", Pending);
      return Suspended ? "the importer is paused" : "nothing being imported";
    }
  }
}
