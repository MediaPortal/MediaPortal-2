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

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  /// <summary>
  /// Base class for matches of Series or Movies. It contains required fields for the <see cref="BaseMatcher{TMatch,TId}"/> download management feature.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class BaseFanArtMatch<T> : BaseMatch
  {
    /// <summary>
    /// ID of the online library, type is specified by <typeparamref name="T"/>.
    /// </summary>
    public T Id;
    /// <summary>
    /// ID for downloading FanArt.
    /// </summary>
    public string FanArtDownloadId;
    /// <summary>
    /// Contains the start time of download. If it is not <c>null</c> and <see cref="FanArtDownloadFinished"/> is <c>null</c>, download should be still
    /// in progress.
    /// </summary>
    public DateTime? FanArtDownloadStarted;
    /// <summary>
    /// Contains the end time of download. If it is not <c>null</c>, download has been completed.
    /// </summary>
    public DateTime? FanArtDownloadFinished;
  }
}
