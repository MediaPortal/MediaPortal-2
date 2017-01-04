#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Xml.Serialization;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement;

namespace MediaPortal.Common.MediaManagement
{
  public enum ImportJobType
  {
    Import,
    Refresh
  }

  // ToDo: ImportJobState is not required for ImporterWorkerNewGen anymore.
  // The state is represented by the Completion task of the ImportJobController  
  public enum ImportJobState
  {
    None,
    Scheduled,
    Active,
    Finished,
    Cancelled,
    Erroneous
  }

  public class DisconnectedException : ApplicationException
  {
    public DisconnectedException() {}
    public DisconnectedException(string message, params object[] parameters) : base(string.Format(message, parameters)) {}
    public DisconnectedException(string message, Exception innerException, params object[] parameters) :
        base(string.Format(message, parameters), innerException) {}
  }

  /// <summary>
  /// Holds the data which specifies the basic data of an import job.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public struct ImportJobInformation : IEquatable<ImportJobInformation>, IComparable<ImportJobInformation>
  {
    private ImportJobType _jobType;
    private ResourcePath _basePath;
    private HashSet<Guid> _metadataExtractorIds;
    private bool _includeSubDirectories;
    private ImportJobState _state;

    public ImportJobInformation(ImportJobType jobType, ResourcePath basePath, IEnumerable<Guid> metadataExtractorIds, bool includeSubDirectories)
    {
      _jobType = jobType;
      _basePath = basePath;
      _metadataExtractorIds = new HashSet<Guid>(metadataExtractorIds);
      _includeSubDirectories = includeSubDirectories;
      _state = ImportJobState.None;
    }

    public ImportJobInformation(ImportJobInformation other)
    {
      _jobType = other.JobType;
      _basePath = other.BasePath;
      _metadataExtractorIds = new HashSet<Guid>(other.MetadataExtractorIds);
      _includeSubDirectories = other.IncludeSubDirectories;
      _state = other.State;
    }

    [XmlIgnore]
    public ImportJobType JobType
    {
      get { return _jobType; }
      set { _jobType = value; }
    }

    [XmlIgnore]
    public ResourcePath BasePath
    {
      get { return _basePath; }
      set { _basePath = value; }
    }

    [XmlIgnore]
    public ICollection<Guid> MetadataExtractorIds
    {
      get { return _metadataExtractorIds; }
      set { _metadataExtractorIds = new HashSet<Guid>(value); }
    }

    [XmlIgnore]
    public bool IncludeSubDirectories
    {
      get { return _includeSubDirectories; }
      set { _includeSubDirectories = value; }
    }

    [XmlIgnore]
    public ImportJobState State
    {
      get { return _state; }
      set { _state = value; }
    }

    public bool Equals(ImportJobInformation other)
    {
      return _basePath == other._basePath &&
             _jobType == other._jobType &&
             _includeSubDirectories == other._includeSubDirectories &&
             _metadataExtractorIds.SetEquals(other._metadataExtractorIds) &&
             // ToDo: Remove when ImporterWorkerNewGen is ready
             _state == other._state;
    }

    public static bool operator ==(ImportJobInformation i1, ImportJobInformation i2)
    {
      return i1.Equals(i2);
    }

    public static bool operator !=(ImportJobInformation i1, ImportJobInformation i2)
    {
      return !(i1 == i2);
    }

    /// <summary>
    /// Checks whether an ImportJob is contained in another ImportJob
    /// (a >= b means a contains b)
    /// </summary>
    /// <param name="other">ImportJobInformation to compare with</param>
    /// <returns></returns>
    public int CompareTo(ImportJobInformation other)
    {
      // both are equal
      if (Equals(other))
        return 0;

      // the current ImportJobInformation contains other
      if (_basePath.IsParentOf(other._basePath) &&
        _jobType == other._jobType &&
        _includeSubDirectories &&
        _metadataExtractorIds.IsSupersetOf(other._metadataExtractorIds))
          return 1;

      return -1;
    }

    public static bool operator >(ImportJobInformation i1, ImportJobInformation i2)
    {
      return i1.CompareTo(i2) > 0;
    }

    public static bool operator <(ImportJobInformation i1, ImportJobInformation i2)
    {
      return i1.CompareTo(i2) < 0;
    }

    public static bool operator >=(ImportJobInformation i1, ImportJobInformation i2)
    {
      return (i1 == i2) || (i1 > i2);
    }

    public static bool operator <=(ImportJobInformation i1, ImportJobInformation i2)
    {
      return (i1 == i2) || (i1 < i2);
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ImportJobInformation))
        return false;
      ImportJobInformation other = (ImportJobInformation) obj;
      return _basePath == other._basePath && _jobType == other._jobType &&
          _includeSubDirectories == other._includeSubDirectories &&
          _metadataExtractorIds.SetEquals(other._metadataExtractorIds) &&
          _state == other._state;
    }

    public override int GetHashCode()
    {
      return _basePath.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("ImportJob '{0}'", _basePath.Serialize());
    }

    #region Additional members for the XML serialization

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("JobType")]
    public ImportJobType XML_JobType
    {
      get { return _jobType; }
      set { _jobType = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("State")]
    public ImportJobState XML_State
    {
      get { return _state; }
      set { _state = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("BasePath", IsNullable = false)]
    public string XML_BasePath
    {
      get { return _basePath.Serialize(); }
      set { _basePath = ResourcePath.Deserialize(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MetadataExtractorIds", IsNullable = false)]
    [XmlArrayItem("Id")]
    public HashSet<Guid> XML_MetadataExtractorIds
    {
      get { return _metadataExtractorIds; }
      set { _metadataExtractorIds = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("IncludeSubDirectories")]
    public bool XML_IncludeSubDirectories
    {
      get { return _includeSubDirectories; }
      set { _includeSubDirectories = value; }
    }

    #endregion
  }

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
    /// Returns a collection of objects holding information about all current import jobs.
    /// </summary>
    ICollection<ImportJobInformation> ImportJobs { get; }

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
    void ScheduleImport(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories);

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
    void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories);
  }
}
