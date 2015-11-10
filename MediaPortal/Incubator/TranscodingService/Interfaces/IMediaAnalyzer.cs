#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using System.Collections.Generic;

namespace MediaPortal.Plugins.Transcoding.Service.Interfaces
{
  /// <summary>
  /// This Interface should only be used from within MetadataExtractors.
  /// </summary>
  public interface IMediaAnalyzer
  {

    /// <summary>
    /// Sets the timeout after which the Thread gets stopped
    /// </summary>
    int AnalyzerTimeout { get; set; }

    /// <summary>
    /// The maximum number of threads to use
    /// </summary>
    int AnalyzerMaximumThreads { get; set; }

    /// <summary>
    /// Sets or gets the logger implementation to use for logging.
    /// </summary>
    ILogger Logger { get; set; }
    
    /// <summary>
    /// Pareses a local image file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the image file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    MetadataContainer ParseImageFile(ILocalFsResourceAccessor lfsra);

    /// <summary>
    /// Pareses a local video file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the video file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    MetadataContainer ParseVideoFile(ILocalFsResourceAccessor lfsra);

    /// <summary>
    /// Pareses a local audio file using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="lfsra">ILocalFsResourceAccessor to the file</param>
    /// <returns>a Metadata Container with all information about the mediaitem</returns>
    MetadataContainer ParseAudioFile(ILocalFsResourceAccessor lfsra);

    /// <summary>
    /// Pareses a video URL using FFProbe and returns a MetadataContainer with the information (codecs, container, streams, ...) found
    /// </summary>
    /// <param name="streamLink">INetworkResourceAccessor to the video URL</param>
    /// <returns>a Metadata Container with all information about the URL</returns>
    MetadataContainer ParseVideoStream(INetworkResourceAccessor streamLink);
  }
}
