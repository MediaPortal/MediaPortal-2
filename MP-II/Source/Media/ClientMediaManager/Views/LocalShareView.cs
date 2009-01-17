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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Utilities;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// View implementation which is based on a local provider path.
  /// </summary>
  public class LocalShareView : View
  {
    #region Protected fields

    protected Guid _shareId;
    protected string _overrideName;
    protected string _relativePath;
    protected HashSet<Guid> _mediaItemAspectIds = new HashSet<Guid>();

    #endregion

    #region Ctor

    internal LocalShareView(Guid shareId, string overrideName, string relativePath,
        View parentView, IEnumerable<Guid> mediaItemAspectIds) :
        base(parentView, mediaItemAspectIds)
    {
      _shareId = shareId;
      _overrideName = overrideName;
      _relativePath = relativePath;
      CollectionUtils.AddAll(_mediaItemAspectIds, mediaItemAspectIds);
      if (!_mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
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
      set { _overrideName = value; }
    }

    [XmlIgnore]
    public override ICollection<Guid> MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
    }

    #region Base overrides

    [XmlIgnore]
    public override string DisplayName
    {
      get { return _overrideName ?? Path.GetFileName(_relativePath); }
    }

    [XmlIgnore]
    public override bool IsValid
    {
      get
      {
        ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
        if (share == null)
          return false;
        MediaManager mediaManager = ServiceScope.Get<MediaManager>();
        Guid providerId = share.MediaProviderId;
        return mediaManager.LocalMediaProviders.ContainsKey(providerId);
      }
    }

    public override bool IsBasedOnShare(Guid shareId)
    {
      return _shareId == shareId;
    }

    protected override IList<MediaItem> ReLoadItems()
    {
      ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
      if (share == null)
        return null;
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      Guid providerId = share.MediaProviderId;
      IMediaProvider provider;
      if (!mediaManager.LocalMediaProviders.TryGetValue(providerId, out provider))
        return null;
      IList<MediaItem> result = new List<MediaItem>();
      string path = Path.Combine(share.Path, _relativePath);
      IEnumerable<Guid> metadataExtractorIds = share.MetadataExtractorIds;
      if (provider is IFileSystemMediaProvider)
      { // Add all items at the specified path
        IFileSystemMediaProvider fsmp = (IFileSystemMediaProvider) provider;
        foreach (string mediaItemPath in fsmp.GetFiles(path))
          AddMetadata(mediaManager, providerId, mediaItemPath, metadataExtractorIds, result);
      }
      else
        // Add the path itself (Could be a single-item share)
        AddMetadata(mediaManager, providerId, path, metadataExtractorIds, result);
      return result;
    }

    protected override IList<View> ReLoadSubViews()
    {
      ShareDescriptor share = ServiceScope.Get<ISharesManagement>().GetShare(_shareId);
      if (share == null)
        return null;
      MediaManager mediaManager = ServiceScope.Get<MediaManager>();
      Guid providerId = share.MediaProviderId;
      IMediaProvider provider = mediaManager.LocalMediaProviders[providerId];
      if (provider == null)
        return null;
      IList<View> result = new List<View>();
      string path = Path.Combine(share.Path, _relativePath);
      if (provider is IFileSystemMediaProvider)
      { // Add all items at the specified path
        IFileSystemMediaProvider fsmp = (IFileSystemMediaProvider) provider;
        foreach (string childDirectory in fsmp.GetChildDirectories(path))
          result.Add(new LocalShareView(_shareId, null, _relativePath, this, _mediaItemAspectIds));
      }
      return result;
    }

    #endregion

    /// <summary>
    /// Adds a media item with metadata extracted by the metadata extractors specified by the
    /// <paramref name="metadataExtractorIds"/> from the specified <paramref name="providerId"/> and
    /// <paramref name="path"/> to the <paramref name="result"/>.
    /// </summary>
    /// <param name="mediaManager">Media manager instance. This parameter is for performance to avoid
    /// iterated calls to the <see cref="ServiceScope"/>.</param>
    /// <param name="providerId">Id of the media provider which provides the media item to analyse.</param>
    /// <param name="path">Path of the media item to analyse.</param>
    /// <param name="metadataExtractorIds">Ids of the metadata extractors to employ on the media item.</param>
    /// <param name="result">Collection to add the resulting <see cref="MediaItem"/> to.</param>
    protected static void AddMetadata(MediaManager mediaManager, Guid providerId, string path,
        IEnumerable<Guid> metadataExtractorIds, ICollection<MediaItem> result)
    {
      IDictionary<Guid, MediaItemAspect> aspects = mediaManager.ExtractMetadata(providerId, path, metadataExtractorIds);
      if (aspects != null)
      {
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
        result.Add(new MediaItem(aspects));
      }
    }

    #region Additional members for the XML serialization

    // Serialization of local share views works like this:
    // The first (upper) local share view will be serialized by the data denoted below.
    // The deeper views won't be serialized as they are re-created dynamically the next time
    // the system starts.

    internal LocalShareView() { }

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

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItemAspectIds")]
    public HashSet<Guid> XML_MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
      set { _mediaItemAspectIds = value; }
    }

    #endregion
  }
}
