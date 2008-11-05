#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Media.MediaManagement
{
  public delegate void MediaDatabaseConnectedDelegate(IMediaDatabase mediaDatabase);

  public delegate void MediaLibraryDisconnected();

  /// <summary>
  /// The MediaManager is the central component for managing media in the MediaPortal-II system.
  /// It manages media access, the acces to the MediaDatabase and the import of media files.
  /// The MediaManager encapsulates the whole media management subsystem from the media usage subsystem, i.e.
  /// all media access
  /// </summary>
  /// <remarks>
  /// The responsibilities of the MediaManager are:
  /// <list type="bullet">
  /// <item>Managing MediaProviders and MediaProviders plugin access</item>
  /// <item>Managing MetadataExtractors and MetadataExtractors plugin access</item>
  /// <item>Managing all available media views</item>
  /// </list>
  /// </remarks>
  public interface IMediaManager
  {
    /// <summary>
    /// Collection of all registered media providers, organized as a dictionary of
    /// (GUID; provider) mappings.
    /// This media provider collection is the proposed entry point to get access to physical media
    /// files.
    /// </summary>
    IDictionary<string, IMediaProvider> MediaProviders { get;}

    /// <summary>
    /// Collection of all registered metadata extractors, organized as a dictionary of
    /// (GUID; metadata extractor) mappings.
    /// </summary>
    IDictionary<string, IMetadataExtractor> MetadataExtractors { get; }

    /// <summary>
    /// Gets access to the system's MediaDatabase.
    /// </summary>
    /// <remarks>
    /// Physically, this will return the local MediaDatabase
    /// (in case we're on the MP server) or a reference to a MediaDatabase stub which is connected to the
    /// server's MediaDatabase. In the startup and shutdown phaes and in case we're running in a pure-client
    /// scenario, the returned reference may be <c>null</c>.
    /// </remarks>
    /// <seealso cref="MediaDatabaseConnected"/>
    /// <seealso cref="MediaDatabaseDisconnected"/>
    IMediaDatabase MediaDatabase { get; }

    /// <summary>
    /// This event gets fired when the local system gains access to a MediaDatabase instance.
    /// </summary>
    /// <remarks>
    /// For an MP server, this event will be fired as soon as the MediaDatabase is available.
    /// For an MP client, this event will be fired when the client has connected to an MP server
    /// and built up its local MediaDatabase stub.
    /// After this event is fired, the property <see cref="MediaDatabase"/> will return the current MediaLibrary
    /// accessor instance.
    /// </remarks>
    event MediaDatabaseConnectedDelegate MediaDatabaseConnected;

    /// <summary>
    /// This event gets fired when the local system looses access to the MediaDatabase instance.
    /// </summary>
    /// <remarks>
    /// For an MP server, this event will be fired when the server shuts down.
    /// For an MP client, this event will be fired when the client has disconnected from an MP server.
    /// After this event is fired, the property <see cref="MediaDatabase"/> will return <c>null</c>.
    /// </remarks>
    event MediaLibraryDisconnected MediaDatabaseDisconnected;
  }
}
