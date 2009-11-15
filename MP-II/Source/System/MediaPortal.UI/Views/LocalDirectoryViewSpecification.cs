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
using System.Xml.Serialization;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.Views
{
  /// <summary>
  /// View implementation which is based on a local provider path.
  /// </summary>
  public class LocalDirectoryViewSpecification : ViewSpecification
  {
    #region Consts

    public const string INVALID_SHARE_NAME_RESOURCE = "[Media.InvalidShareName]";

    #endregion

    #region Protected fields

    protected string _overrideName;
    protected ResourcePath _viewPath;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="LocalDirectoryViewSpecification"/> instance.
    /// </summary>
    /// <param name="overrideName">Overridden name for the view. If not set, the resource name of the specified
    /// <paramref name="viewPath"/> will be used as <see cref="ViewDisplayName"/>.</param>
    /// <param name="viewPath">Path of a directory in a local filesystem provider.</param>
    /// <param name="mediaItemAspectIds">Ids of the media item aspects which should be extracted for all items and
    /// sub views of this view.</param>
    internal LocalDirectoryViewSpecification(string overrideName, ResourcePath viewPath,
        IEnumerable<Guid> mediaItemAspectIds) : base(null, mediaItemAspectIds)
    {
      _overrideName = overrideName;
      _viewPath = viewPath;
      CollectionUtils.AddAll(_mediaItemAspectIds, mediaItemAspectIds);
      if (!_mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
      UpdateDisplayName();
    }

    #endregion

    /// <summary>
    /// Returns the resource path of the directory of this view.
    /// </summary>
    [XmlIgnore]
    public ResourcePath ViewPath
    {
      get { return _viewPath; }
    }

    /// <summary>
    /// Returns the display name which overrides the default (created) display name. This can be
    /// useful for shares root directories.
    /// </summary>
    [XmlElement("OverrideName")]
    public string OverrideName
    {
      get { return _overrideName; }
      set
      {
        _overrideName = value;
        UpdateDisplayName();
      }
    }

    #region Base overrides

    [XmlIgnore]
    public override string ViewDisplayName
    {
      get
      {
        if (string.IsNullOrEmpty(_viewDisplayName))
          UpdateDisplayName();
        return _viewDisplayName;
      }
    }

    [XmlIgnore]
    public override bool CanBeBuilt
    {
      get { return _viewPath.IsValidLocalPath; }
    }

    internal override IEnumerable<MediaItem> ReLoadItems()
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      ICollection<Guid> metadataExtractorIds = new List<Guid>();
      foreach (KeyValuePair<Guid, IMetadataExtractor> extractor in mediaAccessor.LocalMetadataExtractors)
        // Collect all metadata extractors which fill our desired media item aspects
        if (CollectionUtils.HasIntersection(extractor.Value.Metadata.ExtractedAspectTypes.Keys, _mediaItemAspectIds))
          metadataExtractorIds.Add(extractor.Key);
      IResourceAccessor baseResourceAccessor = _viewPath.CreateLocalMediaItemAccessor();
      // Add all items at the specified path
      ICollection<IFileSystemResourceAccessor> files = FileSystemResourceNavigator.GetFiles(baseResourceAccessor);
      if (files != null)
        foreach (IFileSystemResourceAccessor childAccessor in files)
        {
          MediaItem result = GetMetadata(mediaAccessor, childAccessor, metadataExtractorIds);
          if (result != null)
            yield return result;
        }
    }

    internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      IResourceAccessor baseResourceAccessor = _viewPath.CreateLocalMediaItemAccessor();
      // Add all directories at the specified path
      ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(baseResourceAccessor);
      if (directories != null)
        foreach (IFileSystemResourceAccessor childDirectory in directories)
          yield return new LocalDirectoryViewSpecification(null, childDirectory.LocalResourcePath,
              _mediaItemAspectIds);
    }

    #endregion

    protected void UpdateDisplayName()
    {
      _viewDisplayName = string.IsNullOrEmpty(_overrideName) ?
          _viewPath.CreateLocalMediaItemAccessor().ResourceName : _overrideName;
    }

    /// <summary>
    /// Returns a media item with metadata extracted by the metadata extractors specified by the
    /// <paramref name="metadataExtractorIds"/> from the specified <paramref name="mediaItemAccessor"/>.
    /// </summary>
    /// <param name="mediaAccessor">Media manager instance. This parameter is for performance to avoid
    /// iterated calls to the <see cref="ServiceScope"/>.</param>
    /// <param name="mediaItemAccessor">Accessor describing the media item to extract metadata.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <returns>Media item with the specified metadata </returns>
    protected static MediaItem GetMetadata(IMediaAccessor mediaAccessor, IResourceAccessor mediaItemAccessor,
        IEnumerable<Guid> metadataExtractorIds)
    {
      IDictionary<Guid, MediaItemAspect> aspects = mediaAccessor.ExtractMetadata(mediaItemAccessor, metadataExtractorIds);
      if (aspects == null)
        return null;
      MediaItemAspect providerResourceAspect;
      if (aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID];
      else
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID] = new MediaItemAspect(
            ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SOURCE_COMPUTER, SystemName.LocalHostName);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, mediaItemAccessor.LocalResourcePath.Serialize());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_DATEADDED, DateTime.Now);
      return new MediaItem(aspects);
    }

    #region Additional members for the XML serialization

    // Serialization of local share views works like this:
    // The first (upper) local share view will be serialized by the data denoted below.
    // The deeper views won't be serialized as they are re-created dynamically the next time
    // the system starts.

    internal LocalDirectoryViewSpecification() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ViewPath", IsNullable = false)]
    public string XML_ViewPath
    {
      get { return _viewPath.Serialize(); }
      set { _viewPath = ResourcePath.Deserialize(value); }
    }

    #endregion
  }
}
