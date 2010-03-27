#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Importer worker instance. Accepts import jobs and processes them, when possible.
  /// </summary>
  /// <remarks>
  /// The importer worker works in the MediaPortal client as well as in the server.<br/>
  /// <para>
  /// It has two possible states:
  /// <list type="table">
  /// <listheader><term>State</term><description>Description</description></listheader>
  /// <item><term>Active</term><description>The <see cref="IImportResultHandler"/> and <see cref="IMediaBrowsing"/> callback
  /// interfaces are installed, i.e. the MediaLibrary is connected.</description></item>
  /// <item><term>Suspended</term><description>The local system is shutting down or at least one of the callback interfaces
  /// disappeared or produced problems during the communication.</description></item>
  /// </list>
  /// The default state is <i>Suspended</i>. When a connection to the MediaLibrary is established, method
  /// <see cref="Activate"/> is called which installs the two callback interfaces and switches the importer worker state to
  /// <i>Active</i>. When the local system shuts down OR when the MediaLibrary gets disconnected (e.g. when this importer
  /// worker runs in the client and the MediaPortal server shuts down), the state switches back to <i>Suspended</i>.
  /// </para>
  /// <para>
  /// The import jobs of the importer worker are automatically persisted to disc and loaded again when the importer worker
  /// is restarted.
  /// </para>
  /// </remarks>
  public interface IImporterWorker
  {
    /// <summary>
    /// Gets the information if this importer worker was suspended due to a system shutdown or problems in the communication
    /// with the callback interfaces provided by method <see cref="Activate"/>.
    /// </summary>
    bool IsSuspended { get; }

    /// <summary>
    /// Starts the importer worker service.
    /// </summary>
    void Startup();

    /// <summary>
    /// Stops the importer worker service.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Activates the importer worker. The importer worker automatically stops its activity when it encounters
    /// problems in the communication with the two provided interfaces <paramref name="mediaBrowsingCallback"/> or
    /// <paramref name="importResultHandler"/>, or if it is shut down from outside, or if it encounters a system shutdown.
    /// </summary>
    /// <param name="mediaBrowsingCallback">Callback interface to browse existing media items in the media library.</param>
    /// <param name="importResultHandler">Handler interface for import results.</param>
    void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler);

    /// <summary>
    /// Suspends the importer worker. This will make the importer stop its current activity and move to the <i>Suspended</i>
    /// state.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Cancels all pending import jobs and clears the queue.
    /// </summary>
    void CancelPendingJobs();

    /// <summary>
    /// Stops all active tasks for the given <paramref name="path"/> and removes all pending tasks for the given
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Path, whose import tasks will be removed. Also tasks for sub-paths will be removed.</param>
    void CancelJobsForPath(ResourcePath path);

    /// <summary>
    /// Schedules an asynchronous import of the local resource specified by <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Resource path of the directory or file to be imported.</param>
    /// <param name="mediaCategories">Media categories to choose metadata extractors for.</param>
    /// <param name="includeSubDirectories">If the given <paramref name="path"/> is a directory, this parameter controls if
    /// subdirectories are imported or not.</param>
    void ScheduleImport(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories);

    /// <summary>
    /// Schedules an asynchronous refresh of the local resource specified by <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// This method will request the media library for the current data of all modules before starting the import to
    /// check if the module was changed against the media library.
    /// </remarks>
    /// <param name="path">Resource path of the directory or file to be imported.</param>
    /// <param name="mediaCategories">Media categories to choose metadata extractors for.</param>
    /// <param name="includeSubDirectories">If the given <paramref name="path"/> is a directory, this parameter controls if
    /// subdirectories are imported or not.</param>
    void ScheduleRefresh(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories);
  }
}
