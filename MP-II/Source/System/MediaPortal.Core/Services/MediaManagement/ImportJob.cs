#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Core.Services.MediaManagement
{
  public enum ImportJobType
  {
    Import,
    Refresh
  }

  /// <summary>
  /// Holds the data which specifies a job for the importer worker.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ImportJob
  {
    protected ImportJobType _jobType;
    protected ResourcePath _basePath;
    protected List<IFileSystemResourceAccessor> _pendingResources = new List<IFileSystemResourceAccessor>();
    protected HashSet<Guid> _metadataExtractorIds;
    protected bool _includeSubDirectories;

    public ImportJob(ImportJobType jobType, ResourcePath basePath, IEnumerable<Guid> metadataExtractorIds,
      bool includeSubDirectories)
    {
      _jobType = jobType;
      _basePath = basePath;
      _metadataExtractorIds = new HashSet<Guid>(metadataExtractorIds);
      _includeSubDirectories = includeSubDirectories;
    }

    [XmlIgnore]
    public ImportJobType JobType
    {
      get { return _jobType; }
    }

    [XmlIgnore]
    public ResourcePath BasePath
    {
      get { return _basePath; }
    }

    public ICollection<IFileSystemResourceAccessor> PendingResources
    {
      get { return _pendingResources; }
    }

    /// <summary>
    /// Collection of IDs of metadata extractors to apply during this import job.
    /// </summary>
    /// <remarks>
    /// We don't store the media categories from whose the metadata extractors were derived to avoid
    /// that MEs change during the (potential very long) job runtime. Remember a job can be suspended
    /// for an undefined time while the MediaPortal server is down.
    /// <(remarks>
    [XmlIgnore]
    public ICollection<Guid> MetadataExtractorIds
    {
      get { return _metadataExtractorIds; }
    }

    [XmlIgnore]
    public bool IncludeSubDirectories
    {
      get { return _includeSubDirectories; }
    }

    public override bool Equals(object obj)
    {
      if (!(obj is ImportJob))
        return false;
      ImportJob other = (ImportJob) obj;
      return _basePath == other._basePath && _jobType == other._jobType && _includeSubDirectories == other._includeSubDirectories;
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

    internal ImportJob() { }

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
    [XmlElement("BasePath", IsNullable = false)]
    public string XML_BasePath
    {
      get { return _basePath.Serialize(); }
      set { _basePath = ResourcePath.Deserialize(value); }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("PendingDirectories", IsNullable = false)]
    public List<string> XML_PendingDirectories
    {
      get
      {
        List<string> result = new List<string>(_pendingResources.Count);
        foreach (IFileSystemResourceAccessor resource in _pendingResources)
          result.Add(resource.LocalResourcePath.Serialize());
        return result;
      }
      set
      {
        foreach (string path in value)
          try
          {
            IResourceAccessor ra = ResourcePath.Deserialize(path).CreateLocalMediaItemAccessor();
            if (!(ra is IFileSystemResourceAccessor))
              throw new IllegalCallException("'{0}' is no filesystem resource");
            _pendingResources.Add((IFileSystemResourceAccessor) ra);
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Warn("ImportJob '{0}': cannot add pending import resource '{1}'", e,
                _basePath.Serialize(), path);
          }
      }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("MetadataExtractorIds", IsNullable = false)]
    public HashSet<Guid> XML_MetadataExtractorIds
    {
      get { return _metadataExtractorIds; }
      set { _metadataExtractorIds = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("IncludeSubDirectories", IsNullable = false)]
    public bool XML_IncludeSubDirectories
    {
      get { return _includeSubDirectories; }
      set { _includeSubDirectories = value; }
    }

    #endregion
  }
}
