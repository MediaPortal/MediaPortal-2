#region Copyright (C) 2007-xxCurrentYear Team MediaPortal

/*
    Copyright (C) 2007-xxCurrentYear Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;


namespace MediaPortal.Extensions.MetadataExtractors.xxPluginName
{
  /// <summary>
  /// MediaPortal 2 metadata extractor implementation for xxPluginName.
  /// </summary>
  public class xxPluginName : IMetadataExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the xxPluginName.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "xxPluginId";

    /// <summary>
    /// xxPluginName GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    protected const string MEDIA_CATEGORY_NAME = "xxPluginName";

    #endregion

    #region Protected fields and classes

    protected static ICollection<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory>();
    protected MetadataExtractorMetadata _metadata;

    #endregion

    #region Ctor

    static xxPluginName()
    {
      MediaCategory category;
      IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      if (!mediaAccessor.MediaCategories.TryGetValue(MEDIA_CATEGORY_NAME, out category))
        category = mediaAccessor.RegisterMediaCategory(MEDIA_CATEGORY_NAME, new List<MediaCategory> {DefaultMediaCategories}); // add your MediaCategory here
      MEDIA_CATEGORIES.Add(category);
    }

    public xxPluginName()
    {
      _metadata = new MetadataExtractorMetadata(METADATAEXTRACTOR_ID, "xxPluginName", MetadataExtractorPriority.External, true,
          MEDIA_CATEGORIES, new[]
              {
                // Add your xxAspect.Metadata here
                //MediaAspect.Metadata,
              });
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      if (forceQuickMode)
        return false;

      try
      {
        // add your Extractor logic here
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Info("xxPluginName: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    #endregion
  }
}