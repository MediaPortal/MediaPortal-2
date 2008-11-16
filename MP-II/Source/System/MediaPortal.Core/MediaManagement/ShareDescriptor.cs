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

using System;
using System.Collections.Generic;
using MediaPortal.Core.General;

namespace MediaPortal.Core.MediaManagement
{
  public enum DefaultMediaCategory
  {
    Audio,
    AudioStream,
    Video,
    VideoStream,
    RemovableDisc,
    TvData
  }

  /// <summary>
  /// Holds all configuration data of a share. A share descriptor globally describes a share
  /// in an MP-II system.
  /// A share basically is a directory of a provider, which gets assigned a special name (the share name).
  /// Some user interaction at the GUI level will use the share as a means to simplify the work with
  /// media provider paths (for example the automatic import).
  /// </summary>
  public class ShareDescriptor
  {
    #region Protected fields

    protected Guid _shareId;
    protected SystemName _systemName;
    protected Guid _mediaProviderId;
    protected string _path;
    protected string _name;
    protected IEnumerable<string> _mediaCategories;
    protected ICollection<Guid> _metadataExtractors;

    #endregion

    /// <summary>
    /// Creates a new share descriptor with the specified values.
    /// </summary>
    /// <param name="shareId">Id of the share. For the same share (i.e. the same media provider on the same
    /// system with the same path), the id should be perserverd, i.e. the id should not be re-created
    /// but stored persistently. This helps other components to use the id as fixed identifier for the share.</param>
    /// <param name="systemName">Specifies the system on that the media provider with the specified
    /// <paramref name="mediaProviderId"/> is located.</param>
    /// <param name="mediaProviderId">Id of the media provider which provides the file system for the
    /// share.</param>
    /// <param name="path">Path at the media provider with the specified <paramref name="mediaProviderId"/>
    /// where the share should have its root directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized.</param>
    /// <param name="mediaCategories">Categories of media in this share. If set, the categories describe
    /// the desired contents of this share. If set to <c>null</c>, the share has no explicit media categories,
    /// i.e. it is a general share.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors which should be used for the
    /// automatic import.</param>
    public ShareDescriptor(Guid shareId, SystemName systemName,
        Guid mediaProviderId, string path, string name,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractors)
    {
      _shareId = shareId;
      _systemName = systemName;
      _mediaProviderId = mediaProviderId;
      _path = path;
      _name = name;
      _mediaCategories = mediaCategories == null ? new List<string>() : new List<string>(mediaCategories);
      _metadataExtractors = new List<Guid>(metadataExtractors);
    }

    /// <summary>
    /// Creates a new share. This will create a new share id.
    /// </summary>
    /// <param name="systemName">Specifies the system on that the media provider with the specified
    /// <paramref name="mediaProviderId"/> is located.</param>
    /// <param name="mediaProviderId">Id of the media provider which provides the file system for the
    /// share.</param>
    /// <param name="path">Path at the media provider with the specified <paramref name="mediaProviderId"/>
    /// where the share should have its root directory.</param>
    /// <param name="name">Name of the share. This name will be shown at the GUI. The string might be
    /// localized.</param>
    /// <param name="mediaCategories">Media content categories of this share. If set, the category
    /// describes the desired contents of this share. If set to <c>null</c>, this share has no explicit
    /// media categories, i.e. it is a general share.</param>
    /// <param name="metadataExtractors">Enumeration of metadata extractors which should be used for the
    /// automatic import.</param>
    /// <returns>Created <see cref="ShareDescriptor"/> with a new share id.</returns>
    public static ShareDescriptor CreateNewShare(SystemName systemName,
        Guid mediaProviderId, string path, string name,
        IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractors)
    {
      return new ShareDescriptor(Guid.NewGuid(), systemName, mediaProviderId, path,
          name, mediaCategories, metadataExtractors);
    }

    /// <summary>
    /// Returns the globally unique id of this share.
    /// </summary>
    public Guid ShareId
    {
      get { return _shareId; }
    }

    /// <summary>
    /// Returns the system name where this share is located.
    /// </summary>
    public SystemName System
    {
      get { return _systemName; }
    }

    /// <summary>
    /// Returns the id of the media provider this share is based on.
    /// </summary>
    public Guid MediaProviderId
    {
      get { return _mediaProviderId; }
    }

    /// <summary>
    /// Returns the path used for the media provider (specified by <see cref="MediaProviderId"/>) for this share.
    /// </summary>
    public string Path
    {
      get { return _path; }
    }

    /// <summary>
    /// Returns the name of this share.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the media contents categories of this share. The media categories can be used for a filtering
    /// of shares or for the GUI to add default metadata extractors for the specified categories.
    /// </summary>
    public IEnumerable<string> MediaCategories
    {
      get { return _mediaCategories; }
    }

    /// <summary>
    /// Returns a collection of ids of metadata extractors which are used for the automatic import of this share.
    /// </summary>
    public ICollection<Guid> MetadataExtractors
    {
      get { return _metadataExtractors; }
    }
  }
}
