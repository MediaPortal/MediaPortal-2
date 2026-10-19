#region Copyright

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

#endregion

namespace MediaPortal.Common.ResourceAccess
{
  /// <summary>
  /// The decision about whether a resource may leave this process over HTTP to whoever asked for it.
  /// </summary>
  /// <remarks>
  /// <para>
  /// <see cref="MediaPortal.Common.Services.ResourceAccess.ResourceAccessModule"/> answers
  /// <c>GetResource?ResourcePath=...</c> for a serialized resource path handed to it in a URL. Until this seam
  /// existed its own <c>IsAllowedToAccess</c> said <c>true</c> for everything and carried a <c>TODO</c> about it,
  /// which - measured on a deployed server with web authorization at its shipped default - meant that anybody who
  /// could reach the port read any file the server's process could read: the API answers a stranger with
  /// <c>403</c> while the same socket handed over <c>C:\Windows\win.ini</c> with a <c>200</c>.
  /// </para>
  /// <para>
  /// The decision cannot be made in this assembly, because what a server may serve is the set of shares somebody
  /// registered, and those live behind the media library in the backend. So the module asks: the rule is declared
  /// here, the answer is registered by whoever owns the data. A host that registers no guard is refused
  /// everything - an open file server is not the safer default for a missing piece of plumbing, and a server that
  /// starts without its backend has no media to serve anyway.
  /// </para>
  /// <para>
  /// This is deliberately not an authentication seam. UPnP renderers are the reason: the media server hands a
  /// television a resource URL to fetch by itself, and a television cannot present a bearer ticket. What makes that
  /// safe is not who is asking but what is being asked for, which is why the shipped implementation answers with
  /// share membership rather than with identity.
  /// </para>
  /// </remarks>
  public interface IResourceAccessGuard
  {
    /// <summary>
    /// Whether the bytes behind <paramref name="resourcePath"/> may be written to an anonymous HTTP response.
    /// </summary>
    /// <param name="resourcePath">The path as it arrived in the request, i.e. relative to the server.</param>
    /// <returns><c>true</c> to serve it, <c>false</c> to answer <c>403</c> and say nothing else.</returns>
    /// <remarks>
    /// Implementations are called once per request - a film being watched over this endpoint asks many times - so
    /// they are expected to be cheap, and to fail closed when the data they decide from cannot be read.
    /// </remarks>
    bool IsAllowed(ResourcePath resourcePath);

    /// <summary>
    /// Whether the server may talk about the resource at all: name it, list what is in it, say whether it exists.
    /// </summary>
    /// <param name="resourcePath">The path as it arrived in the request, i.e. relative to the server.</param>
    /// <returns><c>true</c> if the resource is one of the server's own, <c>false</c> if it is not.</returns>
    /// <remarks>
    /// <para>
    /// Weaker than <see cref="IsAllowed"/> by exactly one thing: a share's own root. A folder cannot be streamed,
    /// so the file server refuses it, but browsing <i>starts</i> at a share - a renderer asked to show the films
    /// asks for the folder somebody registered - so refusing the root here would refuse the feature.
    /// </para>
    /// <para>
    /// The media server's UPnP <c>ResourceInformation</c> service asks this of every path a client names, having
    /// had the same <c>return true;</c> and the same <c>TODO</c> as the file server. Its actions - list the
    /// directories of, list the files of, name, does-it-exist - are the same leak one level up: without a rule they
    /// walk the server's disk from its root.
    /// </para>
    /// </remarks>
    bool IsInsideAShare(ResourcePath resourcePath);
  }
}
