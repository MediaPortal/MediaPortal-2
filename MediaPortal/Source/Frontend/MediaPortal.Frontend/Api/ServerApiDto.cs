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
      [property: JsonPropertyName("stub")] bool Stub);

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
      [property: JsonPropertyName("error")] string? Error)
  {
    /// <summary>
    /// Titles the server has for this aspect beyond the page that arrived.
    /// </summary>
    public int Remaining
    {
      get { return Math.Max(Total - Offset - Returned, 0); }
    }
  }
}
