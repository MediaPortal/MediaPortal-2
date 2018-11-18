#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Instances of this class are used for holding the data for single entries in media item views.
  /// </summary>
  /// <remarks>
  /// Instances of this class contain multiple media item aspect instances but not necessarily all media item
  /// aspects of the underlaying media item from the media library are contained.
  /// </remarks>
  public class MediaItem : IEquatable<MediaItem>, IXmlSerializable
  {
    #region Protected fields

    protected Guid _id;
    protected readonly IDictionary<Guid, IList<MediaItemAspect>> _aspects;
    protected readonly IDictionary<string, string> _userData = new Dictionary<string, string>();
    protected readonly string[] _opticalDiscMimes = new string[] { "video/dvd", "video/bluray" };

    #endregion

    /// <summary>
    /// Creates a new media item.
    /// </summary>
    /// <param name="mediaItemId">Id of the media item in the media library. For local media items, this must be <c>Guid.Empty</c>.</param>
    public MediaItem(Guid mediaItemId) : this()
    {
      _id = mediaItemId;
    }

    /// <summary>
    /// Creates a new media item.
    /// </summary>
    /// <param name="mediaItemId">Id of the media item in the media library. For local media items, this must be <c>Guid.Empty</c>.</param>
    /// <param name="aspects">Dictionary of media item aspects for the new media item instance.</param>
    public MediaItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      _id = mediaItemId;
      _aspects = new Dictionary<Guid, IList<MediaItemAspect>>(aspects);
    }

    /// <summary>
    /// Creates a new media item.
    /// </summary>
    /// <param name="mediaItemId">Id of the media item in the media library. For local media items, this must be <c>Guid.Empty</c>.</param>
    /// <param name="aspects">Dictionary of media item aspects for the new media item instance.</param>
    /// <param name="userData">Dictionary of user specific data for the new media item instance.</param>
    public MediaItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects, IDictionary<string, string> userData)
    {
      _id = mediaItemId;
      _aspects = new Dictionary<Guid, IList<MediaItemAspect>>(aspects);
      _userData = new Dictionary<string, string>(userData);
    }

    public Guid MediaItemId
    {
      get { return _id; }
    }

    public IDictionary<Guid, IList<MediaItemAspect>> Aspects
    {
      get { return _aspects; }
    }

    public IDictionary<string, string> UserData
    {
      get { return _userData; }
    }

    /// <summary>
    /// Returns the media item aspect of the specified <paramref name="mediaItemAspectId"/>, if it is
    /// contained in this media item. If the specified aspect is contained in this instance depends on two
    /// conditions: 1) the aspect has to be present on this media item in the media storage (media library
    /// or local storage), 2) the aspect data have to be added to this instance.
    /// </summary>
    /// <param name="mediaItemAspectId">Id of the media item aspect to retrieve.</param>
    /// <returns>Media item aspect of the specified <paramref name="mediaItemAspectId"/>, or <c>null</c>,
    /// if the aspect is not contained in this instance.</returns>
    public IList<MediaItemAspect> this[Guid mediaItemAspectId]
    {
      get
      {
        IList<MediaItemAspect> result;
        return _aspects.TryGetValue(mediaItemAspectId, out result) ? result : null;
      }
    }

    /// <summary>
    /// If this <see cref="MediaItem"/> represents multi-file media, this index points to the active part for 
    /// that the <see cref="GetResourceLocator"/> will return the locator.
    /// </summary>
    public int ActiveResourceLocatorIndex { get; set; }

    /// <summary>
    /// Returns the maximum zero-based index of available primary resource locators. For single media this will be always <c>0</c>.
    /// If no <see cref="ProviderResourceAspect"/> is available, the result is <c>-1</c>.
    /// Note: extra resources like subtitles are not considered here.
    /// </summary>
    public int MaximumResourceLocatorIndex
    {
      get
      {
        return PrimaryResources.Count - 1;
      }
    }

    /// <summary>
    /// Gets the primary resources of current MediaItem (presents physical parts of multi-file items) that can be used to start playback.
    /// Secondary resources (like subtitles) are not considered here.
    /// </summary>
    public IList<MultipleMediaItemAspect> PrimaryResources
    {
      get
      {
        IList<MultipleMediaItemAspect> providerAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return new List<MultipleMediaItemAspect>();

        // If there are different Editions we need to filter the resources to the current selected edition
        IList<int> selectedResources = HasEditions ? Editions[ActiveEditionIndex].PrimaryResourceIndexes :
          providerAspects.Where(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY).
          Select(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX)).OrderBy(i => i).ToList();

        // Consider only primary resources (physical main parts), but not extra resources (like subtitles)...
        return selectedResources.Select(idx => providerAspects.FirstOrDefault(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == idx)).
          Where(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY).ToList();
      }
    }

    /// <summary>
    /// If this <see cref="MediaItem"/> represents a multi-edition item, this index points to the active part for 
    /// that the <see cref="GetResourceLocator"/> will return the locator.
    /// </summary>
    public int ActiveEditionIndex { get; set; }

    /// <summary>
    /// Returns the maximum zero-based index of available primary resource locators. For single media this will be always <c>0</c>.
    /// If no <see cref="ProviderResourceAspect"/> is available, the result is <c>-1</c>.
    /// Note: extra resources like subtitles are not considered here.
    /// </summary>
    public int MaximumEditionIndex
    {
      get
      {
        return Editions.Count - 1;
      }
    }

    /// <summary>
    /// Indicates if the active resource is the last part of the current edition.
    /// </summary>
    public bool IsLastPart
    {
      get
      {
        if (PrimaryResources.Count <= ActiveResourceLocatorIndex)
          return true;

        IList<MultipleMediaItemAspect> videoStreamAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, VideoStreamAspect.Metadata, out videoStreamAspects))
          return true;

        if (HasEditions)
        {
          var setNo = Editions[ActiveEditionIndex].SetNo;
          var currentResourceIndex = PrimaryResources[ActiveResourceLocatorIndex].GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
          int maxPart = videoStreamAspects.Where(p => p.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == setNo).Max(p => p.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART));
          if (!videoStreamAspects.Any(p => p.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == setNo &&
            p.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART) == maxPart &&
            p.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX) == currentResourceIndex))
            return false;
        }
        return true;
      }
    }

    /// <summary>
    /// Indicates if this <see cref="MediaItem"/> represents a multi-edition item.
    /// </summary>
    public bool HasEditions
    {
      get
      {
        IList<MultipleMediaItemAspect> videoStreamAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, VideoStreamAspect.Metadata, out videoStreamAspects))
          return false;

        if (videoStreamAspects.Select(pra => pra.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET)).Distinct().Count() > 1)
          return true;

        //Special case for optical discs
        IList<MultipleMediaItemAspect> providerAspects;
        if (MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects) &&
          providerAspects.Where(pra => _opticalDiscMimes.Any(m => m.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase))).Count() > 1)
            return true;

        return false;
      }
    }

    /// <summary>
    /// Gets a map of sets and their primary resources indexes for the current MediaItem 
    /// (presents physical parts of multi-file items) that can be used to start playback.
    /// Secondary resources (like subtitles) are not considered here.
    /// </summary>
    public IDictionary<int, (int SetNo, string Name, IList<int> PrimaryResourceIndexes, TimeSpan Duration)> Editions
    {
      get
      {
        var map = new Dictionary<int, (int, string, IList<int>, TimeSpan)>();
        IList<MultipleMediaItemAspect> videoStreamAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, VideoStreamAspect.Metadata, out videoStreamAspects))
          return map;

        int editionIdx = 0;
        List<int> usedSets = new List<int>();
        foreach(var stream in videoStreamAspects.Where(pra => pra.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET) > -1).
          OrderBy(pra => pra.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART)))
        {
          var setNo = stream.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET);
          var videoStreams = videoStreamAspects.Where(v => v.GetAttributeValue<int>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == setNo);

          bool isOpticalDisc = false;
          IList<MultipleMediaItemAspect> providerAspects;
          if (MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
            isOpticalDisc = providerAspects.Any(pra => _opticalDiscMimes.Any(m => m.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE), StringComparison.InvariantCultureIgnoreCase)) &&
            videoStreams.Any(s => s.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX) == pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX)));

          if (usedSets.Contains(setNo) && !isOpticalDisc)
            continue;

          if (!usedSets.Contains(setNo))
            usedSets.Add(setNo);

          (int SetNo, string Name, IList<int> PrimaryResourceIndexes, TimeSpan Duration) edition = 
            (setNo, stream.GetAttributeValue<string>(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME), new List<int>(), new TimeSpan());

          bool durationIsValid = true;
          if (isOpticalDisc)
          {
            long? durSecs = stream.GetAttributeValue<long?>(VideoStreamAspect.ATTR_DURATION);
            if (durSecs.HasValue)
              edition.Duration = edition.Duration.Add(TimeSpan.FromSeconds(durSecs.Value));
            else
              durationIsValid = false;
            edition.PrimaryResourceIndexes.Add(stream.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX));
          }
          else
          {
            foreach (var res in videoStreams)
            {
              long? durSecs = res.GetAttributeValue<long?>(VideoStreamAspect.ATTR_DURATION);
              if (durSecs.HasValue)
                edition.Duration = edition.Duration.Add(TimeSpan.FromSeconds(durSecs.Value));
              else
                durationIsValid = false;
              edition.PrimaryResourceIndexes.Add(res.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX));
            }
          }

          if (durationIsValid)
            edition.Name += $": {edition.Duration.ToString()}";

          map[editionIdx] = edition;
          editionIdx++;
        }
        return map;
      }
    }

    /// <summary>
    /// Assign Id to a media item that has no Id
    /// </summary>
    public bool AssignMissingId(Guid mediaItemId)
    {
      if (_id == Guid.Empty)
      {
        _id = mediaItemId;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Indicates if the current MediaItem is a stub.
    /// </summary>
    public bool IsStub
    {
      get
      {
        if (PrimaryResources.Count > 0)
          return false;

        IList<MultipleMediaItemAspect> providerAspects;
        if (MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return providerAspects.Any(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB);

        return false;
      }
    }

    /// <summary>
    /// Indicates if the current MediaItem is virtual.
    /// </summary>
    public bool IsVirtual
    {
      get
      {
        if (PrimaryResources.Count > 0)
          return false;

        IList<MultipleMediaItemAspect> providerAspects;
        if (MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return providerAspects.Any(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_VIRTUAL);

        return false;
      }
    }

    /// <summary>
    /// Returns a resource locator instance for this item.
    /// </summary>
    /// <returns>Resource locator instance or <c>null</c>, if this item doesn't contain a <see cref="ProviderResourceAspect"/>.</returns>
    public virtual IResourceLocator GetResourceLocator()
    {
      MultipleMediaItemAspect aspect = null;
      if (HasEditions)
      {
        IList<MultipleMediaItemAspect> providerAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return null;

        if (ActiveEditionIndex <= MaximumEditionIndex)
        {
          var currentEdition = Editions[ActiveEditionIndex];
          var resourceIndex = Editions[ActiveEditionIndex].PrimaryResourceIndexes.First();
          if (resourceIndex < providerAspects.Count)
            aspect = providerAspects.First(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == resourceIndex);
        }
      }
      else if (IsStub)
      {
        // If there are no primary resources then return stub resource if available
        IList<MultipleMediaItemAspect> providerAspects;
        if (!MediaItemAspect.TryGetAspects(_aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return null;
        aspect = providerAspects.First(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB);
      }
      else
      {
        if (PrimaryResources.Count <= ActiveResourceLocatorIndex)
          return null;
        aspect = PrimaryResources[ActiveResourceLocatorIndex];
      }
      string systemId = (string)aspect[ProviderResourceAspect.ATTR_SYSTEM_ID];
      string resourceAccessorPath = (string)aspect[ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH];
      return new ResourceLocator(systemId, ResourcePath.Deserialize(resourceAccessorPath));
    }

    public bool GetPlayData(out string mimeType, out string mediaItemTitle)
    {
      mimeType = null;
      mediaItemTitle = null;
      SingleMediaItemAspect mediaAspect = null;
      if (!MediaItemAspect.TryGetAspect(this.Aspects, MediaAspect.Metadata, out mediaAspect))
        return false;
      IList<MultipleMediaItemAspect> resourceAspects = null;
      if (!MediaItemAspect.TryGetAspects(this.Aspects, ProviderResourceAspect.Metadata, out resourceAspects))
        return false;
      foreach (MultipleMediaItemAspect pra in resourceAspects)
      {
        if (pra.GetAttributeValue<int?>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY)
        {
          mimeType = (string)pra[ProviderResourceAspect.ATTR_MIME_TYPE];
          mediaItemTitle = (string)mediaAspect[MediaAspect.ATTR_TITLE];
          return true;
        }
      }
      return false;
    }

    XmlSchema IXmlSerializable.GetSchema()
    {
      return null;
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
      try
      {
        // First read attributes, then check for empty start element
        if (!reader.MoveToAttribute("Id"))
          throw new ArgumentException("Id attribute not present");
        _id = new Guid(reader.Value);
        if (reader.IsEmptyElement)
          return;
      }
      finally
      {
        reader.ReadStartElement();
      }
      while (reader.NodeType != XmlNodeType.EndElement)
      {
        if (reader.Name == "Aspect")
        {
          MediaItemAspect mia = MediaItemAspect.Deserialize(reader);
          if (mia is SingleMediaItemAspect)
          {
            MediaItemAspect.SetAspect(_aspects, (SingleMediaItemAspect)mia);
          }
          else if (mia is MultipleMediaItemAspect)
          {
            MediaItemAspect.AddOrUpdateAspect(_aspects, (MultipleMediaItemAspect)mia);
          }
        }
        else if (reader.Name == "UserData")
        {
          string key = null;
          string data = null;

          if (reader.MoveToAttribute("Key"))
            key = reader.ReadContentAsString();
          if (reader.MoveToAttribute("Data"))
            data = reader.ReadContentAsString();

          if (key != null && data != null)
            _userData.Add(key, data);

          reader.Read();
        }
        else
        {
          reader.Read();
        }
      }
      reader.ReadEndElement(); // MI
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("Id", _id.ToString("D"));
      foreach (IList<MediaItemAspect> list in _aspects.Values)
        foreach (MediaItemAspect mia in list)
          mia.Serialize(writer);

      foreach (string key in _userData.Keys)
      {
        writer.WriteStartElement("UserData");

        writer.WriteAttributeString("Key", key);
        writer.WriteAttributeString("Data", _userData[key]);

        writer.WriteEndElement();
      }
    }

    public void Serialize(XmlWriter writer)
    {
      writer.WriteStartElement("MI"); // MediaItem
      ((IXmlSerializable)this).WriteXml(writer);
      writer.WriteEndElement(); // MediaItem
    }

    public static MediaItem Deserialize(XmlReader reader)
    {
      MediaItem result = new MediaItem();
      ((IXmlSerializable)result).ReadXml(reader);
      return result;
    }

    public override string ToString()
    {
      string mimeType;
      string title;
      if (GetPlayData(out mimeType, out title))
        return title;
      return "<Unknown>";
    }

    #region IEquatable<MediaItem> implementation

    public bool Equals(MediaItem other)
    {
      if (other == null)
        return false;
      IList<MediaItemAspect> myProviderAspect;
      if (!_aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out myProviderAspect))
        return false;
      IList<MediaItemAspect> otherProviderAspect;
      if (!other._aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out otherProviderAspect))
        return false;

      return myProviderAspect.Any(ma => otherProviderAspect.Any(oa => string.Compare(oa.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH),
        ma.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH), true) == 0));
    }

    #endregion

    #region Base overrides

    public override int GetHashCode()
    {
      IList<MediaItemAspect> providerAspect;
      if (!_aspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out providerAspect) || providerAspect.Count == 0)
        return 0;
      return providerAspect[0][ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH].GetHashCode();
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as MediaItem);
    }

    #endregion

    #region Additional members for the XML serialization

    internal MediaItem()
    {
      _aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
    }

    #endregion
  }
}
