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
using System.IO;
using System.Xml.Serialization;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Utilities;

namespace MediaPortal.Core.Views
{
  /// <summary>
  /// View implementation which is based on a local provider path.
  /// </summary>
  public class LocalShareViewSpecification : ViewSpecification
  {
    #region Consts

    public const string INVALID_SHARE_NAME_RESOURCE = "[Media.InvalidShareName]";

    #endregion

    #region Protected fields

    protected Guid _shareId;
    protected string _overrideName;
    protected string _relativePath;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new <see cref="LocalShareViewSpecification"/> instance.
    /// </summary>
    /// <param name="shareId">The id of the share this view is based on.</param>
    /// <param name="overrideName">Overridden name for the view. If not set, the media provider's resource name
    /// for the specified <paramref name="relativePath"/> will be used as <see cref="ViewDisplayName"/>.</param>
    /// <param name="relativePath">Path relative to the base share's path, this view is based on.</param>
    /// <param name="mediaItemAspectIds">Ids of the media item aspects which should be extracted for all items and
    /// sub views of this view.</param>
    internal LocalShareViewSpecification(Guid shareId, string overrideName, string relativePath,
        IEnumerable<Guid> mediaItemAspectIds) : base(null, mediaItemAspectIds)
    {
      _shareId = shareId;
      _overrideName = overrideName;
      _relativePath = relativePath;
      CollectionUtils.AddAll(_mediaItemAspectIds, mediaItemAspectIds);
      if (!_mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
      UpdateDisplayName();
    }

    #endregion

    /// <summary>
    /// Returns the id of the media provider the view is based on.
    /// </summary>
    [XmlIgnore]
    public Guid ShareId
    {
      get { return _shareId; }
    }

    /// <summary>
    /// Returns the path of this view relative to the share this view is based on.
    /// </summary>
    [XmlIgnore]
    public string RelativePath
    {
      get { return _relativePath; }
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
      get
      {
        ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
        if (share == null)
          return false;
        IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
        Guid providerId = share.MediaProviderId;
        return mediaManager.LocalMediaProviders.ContainsKey(providerId);
      }
    }

    internal override IEnumerable<MediaItem> ReLoadItems()
    {
      ShareDescriptor share;
      IMediaProvider provider;
      if (!GetShareAndMediaProvider(out share, out provider))
        yield break;
      string path = Path.Combine(share.Path, _relativePath);
      ICollection<Guid> metadataExtractorIds = new List<Guid>();
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      foreach (KeyValuePair<Guid, IMetadataExtractor> extractor in mediaManager.LocalMetadataExtractors)
        // Collect all metadata extractors which fill our desired media item aspects
        if (CollectionUtils.HasIntersection(extractor.Value.Metadata.ExtractedAspectTypes.Keys, _mediaItemAspectIds))
          metadataExtractorIds.Add(extractor.Key);
      Guid providerId = provider.Metadata.MediaProviderId;
      if (provider is IFileSystemMediaProvider)
      { // Add all items at the specified path
        IFileSystemMediaProvider fsmp = (IFileSystemMediaProvider) provider;
        ICollection<string> files = fsmp.GetFiles(path);
        if (files != null)
          foreach (string mediaItemPath in files)
          {
            MediaItem result = GetMetadata(mediaManager, providerId, mediaItemPath, metadataExtractorIds);
            if (result != null)
              yield return result;
          }
      }
      else
      {
        // Add the path itself (Could be a single-item share)
        MediaItem result = GetMetadata(mediaManager, providerId, path, metadataExtractorIds);
        if (result != null)
          yield return result;
      }
    }

    internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      ShareDescriptor share;
      IMediaProvider provider;
      if (!GetShareAndMediaProvider(out share, out provider))
        yield break;
      string path = Path.Combine(share.Path, _relativePath);
      if (provider is IFileSystemMediaProvider)
      { // Add all directories at the specified path
        IFileSystemMediaProvider fsmp = (IFileSystemMediaProvider) provider;
        ICollection<string> directories = fsmp.GetChildDirectories(path);
        if (directories != null)
          foreach (string childDirectory in directories)
            yield return new LocalShareViewSpecification(_shareId, null, childDirectory.Substring(path.Length),
                _mediaItemAspectIds);
      }
    }

    #endregion

    protected bool GetShareAndMediaProvider(out ShareDescriptor share, out IMediaProvider provider)
    {
      provider = null;
      share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
      if (share == null)
        return false;
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      Guid providerId = share.MediaProviderId;
      return mediaManager.LocalMediaProviders.TryGetValue(providerId, out provider);
    }

    protected void UpdateDisplayName()
    {
      _viewDisplayName = Path.GetFileName(_relativePath); // Fallback
      ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
      if (share == null)
      {
        _viewDisplayName = INVALID_SHARE_NAME_RESOURCE;
        return;
      }
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      Guid providerId = share.MediaProviderId;
      IMediaProvider provider;
      if (!mediaManager.LocalMediaProviders.TryGetValue(providerId, out provider))
        _viewDisplayName = _relativePath;
      else
      {
        string path = Path.Combine(share.Path, _relativePath);
        _viewDisplayName = provider.GetResourceName(path);
      }
    }

    /// <summary>
    /// Returns a media item with metadata extracted by the metadata extractors specified by the
    /// <paramref name="metadataExtractorIds"/> from the specified <paramref name="providerId"/> and
    /// <paramref name="path"/>.
    /// </summary>
    /// <param name="mediaManager">Media manager instance. This parameter is for performance to avoid
    /// iterated calls to the <see cref="ServiceScope"/>.</param>
    /// <param name="providerId">Id of the media provider which provides the media item to analyse.</param>
    /// <param name="path">Path of the media item to analyse.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <returns>Media item with the specified metadata </returns>
    protected static MediaItem GetMetadata(IMediaManager mediaManager, Guid providerId, string path,
        IEnumerable<Guid> metadataExtractorIds)
    {
      IDictionary<Guid, MediaItemAspect> aspects = mediaManager.ExtractMetadata(providerId, path, metadataExtractorIds);
      if (aspects == null)
        return null;
      MediaItemAspect providerResourceAspect;
      if (aspects.ContainsKey(ProviderResourceAspect.ASPECT_ID))
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID];
      else
        providerResourceAspect = aspects[ProviderResourceAspect.ASPECT_ID] = new MediaItemAspect(
            ProviderResourceAspect.Metadata);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SOURCE_COMPUTER, SystemName.LocalHostName);
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_PROVIDER_ID, providerId.ToString());
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_PATH, path);
      providerResourceAspect.SetCollectionAttribute(ProviderResourceAspect.ATTR_PARENTPROVIDERS, new string[] {});
      providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_DATEADDED, DateTime.Now);
      return new MediaItem(aspects);
    }

    #region Additional members for the XML serialization

    // Serialization of local share views works like this:
    // The first (upper) local share view will be serialized by the data denoted below.
    // The deeper views won't be serialized as they are re-created dynamically the next time
    // the system starts.

    internal LocalShareViewSpecification() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ShareId", IsNullable = false)]
    public Guid XML_ShareId
    {
      get { return _shareId; }
      set { _shareId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("Path", IsNullable = false)]
    public string XML_Path
    {
      get { return _relativePath; }
      set { _relativePath = value; }
    }

    #endregion
  }
}
