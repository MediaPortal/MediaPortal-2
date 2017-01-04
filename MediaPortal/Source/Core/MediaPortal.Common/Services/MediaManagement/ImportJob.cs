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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Holds the data which specifies a job for the importer worker.
  /// </summary>
  /// <remarks>
  /// <para>
  /// An import job specifies a resource, a directory or a directory tree to be imported. An import job
  /// can potentially be very big (i.e. many sub directories). To be able to persist the current progress of an import
  /// job, we split the job into smaller pieces, the collection of <see cref="PendingResources"/>, which holds all
  /// sub directories to be processed.
  /// In order to have consistent data to be persisted at each time, the importer worker will process for each directory
  /// its files first. After that, it will replace the current directory in the <see cref="PendingResources"/> by the
  /// collection of sub directories. So the job can be terminated or persisted at any time without the need to save more
  /// process information.
  /// </para>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ImportJob : IDisposable
  {
    protected object _syncObj = new object();
    protected ImportJobInformation _jobInfo;
    protected List<PendingImportResource> _pendingResources = new List<PendingImportResource>();

    public ImportJob(ImportJobType jobType, ResourcePath basePath, IEnumerable<Guid> metadataExtractorIds,
        bool includeSubDirectories)
    {
      _jobInfo = new ImportJobInformation(jobType, basePath, new List<Guid>(metadataExtractorIds), includeSubDirectories);
    }

    public void Dispose()
    {
      foreach (PendingImportResource pendingResource in _pendingResources)
        pendingResource.Dispose();
      _pendingResources.Clear();
    }

    [XmlIgnore]
    public object SyncObj
    {
      get { return _syncObj; }
    }

    [XmlIgnore]
    public ImportJobInformation JobInformation
    {
      get { return _jobInfo; }
    }

    [XmlIgnore]
    public ImportJobState State
    {
      get
      {
        lock (_syncObj)
          return _jobInfo.State;
      }
      set
      {
        lock (_syncObj)
          _jobInfo.State = value;
      }
    }

    [XmlIgnore]
    public ImportJobType JobType
    {
      get { return _jobInfo.JobType; }
    }

    /// <summary>
    /// Path which should be processed. If <see cref="IncludeSubDirectories"/> is set to <c>true</c>,
    /// this property gives the root directory of the tree structure to be processed.
    /// </summary>
    [XmlIgnore]
    public ResourcePath BasePath
    {
      get { return _jobInfo.BasePath; }
    }

    /// <summary>
    /// Collection of pending directories to import. This property only has a sensible value in import job state
    /// <see cref="ImportJobState.Active"/>.
    /// </summary>
    [XmlIgnore]
    public IList<PendingImportResource> PendingResources
    {
      get { return _pendingResources; }
    }

    [XmlIgnore]
    public bool HasPendingResources
    {
      get
      {
        lock (_syncObj)
          return _pendingResources.Count > 0;
      }
    }

    /// <summary>
    /// Collection of IDs of metadata extractors to apply during this import job.
    /// </summary>
    /// <remarks>
    /// We don't store the media categories from whose the metadata extractors were derived to avoid
    /// that MEs change during the (potential very long) job runtime. Remember a job can be suspended
    /// for an undefined time while the MediaPortal server is down.
    /// </remarks>
    [XmlIgnore]
    public ICollection<Guid> MetadataExtractorIds
    {
      get { return _jobInfo.MetadataExtractorIds; }
    }

    [XmlIgnore]
    public bool IncludeSubDirectories
    {
      get { return _jobInfo.IncludeSubDirectories; }
    }

    public void Cancel()
    {
      State = ImportJobState.Cancelled;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ImportJob))
        return false;
      ImportJob other = (ImportJob) obj;
      return _jobInfo.Equals(other._jobInfo);
    }

    public override int GetHashCode()
    {
      return _jobInfo.GetHashCode();
    }

    public override string ToString()
    {
      return _jobInfo.ToString();
    }

    #region Additional members for the XML serialization

    internal ImportJob() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("JobInfo")]
    public ImportJobInformation XML_JobInfo
    {
      get { return _jobInfo; }
      set { _jobInfo = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("PendingResources", IsNullable = false)]
    [XmlArrayItem("Resource")]
    public List<PendingImportResource> XML_PendingResources
    {
      get { return _pendingResources; }
      set { _pendingResources = value; }
    }

    #endregion
  }
}
