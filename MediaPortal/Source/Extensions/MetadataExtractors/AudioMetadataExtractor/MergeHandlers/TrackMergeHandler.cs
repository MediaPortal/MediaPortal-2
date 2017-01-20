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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Common.MediaManagement.MLQueries;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class TrackMergeHandler : IMediaMergeHandler
  {
    #region Constants

    private static readonly Guid[] MERGE_ASPECTS = { AudioAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the track merge handler.
    /// </summary>
    public const string MERGEHANDLER_ID_STR = "5266897F-0251-4927-80BA-66DCB1AB52BE";

    /// <summary>
    /// Track merge handler GUID.
    /// </summary>
    public static Guid MERGEHANDLER_ID = new Guid(MERGEHANDLER_ID_STR);

    #endregion

    protected MergeHandlerMetadata _metadata;

    public TrackMergeHandler()
    {
      _metadata = new MergeHandlerMetadata(MERGEHANDLER_ID, "Track merge handler");
    }

    public Guid[] MergeableAspects
    {
      get
      {
        return MERGE_ASPECTS;
      }
    }

    public MergeHandlerMetadata Metadata
    {
      get { return _metadata; }
    }

    public Guid[] MatchAspects
    {
      get { return TrackInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return IAudioRelationshipExtractor.GetTrackSearchFilter(extractedAspects);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      TrackInfo linkedTrack = new TrackInfo();
      if (!linkedTrack.FromMetadata(extractedAspects))
        return false;

      TrackInfo existingTrack = new TrackInfo();
      if (!existingTrack.FromMetadata(existingAspects))
        return false;

      return linkedTrack.Equals(existingTrack);
    }

    public bool TryMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      try
      {
        //Extracted aspects
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return false;

        //Don't merge virtual resource
        string accessorPath = (string)providerResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        ResourcePath resourcePath = resourcePath = ResourcePath.Deserialize(accessorPath);
        if (resourcePath.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
          return false;

        //Existing aspects
        IList<MultipleMediaItemAspect> existingProviderResourceAspects;
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

        //Replace if existing is a virtual resource
        accessorPath = (string)existingProviderResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        resourcePath = ResourcePath.Deserialize(accessorPath);
        if (resourcePath.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        {
          existingAspects[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false);
          existingAspects.Remove(ProviderResourceAspect.ASPECT_ID);
          foreach (Guid aspect in extractedAspects.Keys)
          {
            if (!existingAspects.ContainsKey(aspect))
              existingAspects.Add(aspect, extractedAspects[aspect]);
          }
          existingAspects[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false);
          return true; 
        }
        
        return false;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("EpisodeMergeHandler: Exception merging resources (Text: '{0}')", e.Message);
        return false;
      }
    }

    public bool RequiresMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      //Allowed to be virtual so never requires merge
      return false;
    }
  }
}
