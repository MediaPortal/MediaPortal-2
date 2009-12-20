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
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Views
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
    /// <param name="necessaryMIATypeIds">Ids of the media item aspect types which should be extracted for all items and
    /// sub views of this view.</param>
    /// <param name="optionalMIATypeIds">Ids of the media item aspect types which may be extracted for items and
    /// sub views of this view.</param>
    internal LocalDirectoryViewSpecification(string overrideName, ResourcePath viewPath,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds) :
        base(null, necessaryMIATypeIds, optionalMIATypeIds)
    {
      _overrideName = overrideName;
      _viewPath = viewPath;
      UpdateDisplayName();
    }

    #endregion

    /// <summary>
    /// Returns the resource path of the directory of this view.
    /// </summary>
    public ResourcePath ViewPath
    {
      get { return _viewPath; }
    }

    /// <summary>
    /// Returns the display name which overrides the default (created) display name. This can be
    /// useful for shares root directories.
    /// </summary>
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

    public override string ViewDisplayName
    {
      get
      {
        if (string.IsNullOrEmpty(_viewDisplayName))
          UpdateDisplayName();
        return _viewDisplayName;
      }
    }

    public override bool CanBeBuilt
    {
      get { return _viewPath.IsValidLocalPath; }
    }

    protected internal override IEnumerable<MediaItem> ReLoadItems()
    {
      IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
      ISystemResolver systemResolver = ServiceScope.Get<ISystemResolver>();
      ICollection<Guid> metadataExtractorIds = new List<Guid>();
      ICollection<Guid> miaTypeIDs = new HashSet<Guid>(_necessaryMIATypeIds);
      CollectionUtils.AddAll(miaTypeIDs, _optionalMIATypeIds);
      foreach (KeyValuePair<Guid, IMetadataExtractor> extractor in mediaAccessor.LocalMetadataExtractors)
        // Collect all metadata extractors which fill our desired media item aspects
        if (CollectionUtils.HasIntersection(extractor.Value.Metadata.ExtractedAspectTypes.Keys, miaTypeIDs))
          metadataExtractorIds.Add(extractor.Key);
      IResourceAccessor baseResourceAccessor = _viewPath.CreateLocalMediaItemAccessor();
      // Add all items at the specified path
      ICollection<IFileSystemResourceAccessor> files = FileSystemResourceNavigator.GetFiles(baseResourceAccessor);
      if (files != null)
        foreach (IFileSystemResourceAccessor childAccessor in files)
        {
          MediaItem result = GetMetadata(mediaAccessor, systemResolver, childAccessor, metadataExtractorIds);
          if (result != null)
            yield return result;
        }
    }

    protected internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      IResourceAccessor baseResourceAccessor = _viewPath.CreateLocalMediaItemAccessor();
      // Add all directories at the specified path
      ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(baseResourceAccessor);
      if (directories != null)
        foreach (IFileSystemResourceAccessor childDirectory in directories)
          yield return new LocalDirectoryViewSpecification(null, childDirectory.LocalResourcePath,
              _necessaryMIATypeIds, _optionalMIATypeIds);
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
    /// <param name="systemResolver">System resolver instance. This parameter is for performance to avoid
    /// iterated calls to the <see cref="ServiceScope"/>.</param>
    /// <param name="mediaItemAccessor">Accessor describing the media item to extract metadata.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <returns>Media item with the specified metadata </returns>
    protected static MediaItem GetMetadata(IMediaAccessor mediaAccessor, ISystemResolver systemResolver,
        IResourceAccessor mediaItemAccessor, IEnumerable<Guid> metadataExtractorIds)
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
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, mediaItemAccessor.LocalResourcePath.Serialize());
      return new MediaItem(aspects);
    }
  }
}
