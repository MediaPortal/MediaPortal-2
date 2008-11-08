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


namespace MediaPortal.Media.MediaManagement
{
  /// <summary>
  /// Format specification for media metadata provided by a metadata extractor for a media item.
  /// </summary>
  public interface IMetadataFormat
  {
    // TODO: Accessor methods for the metadata format. The signatures are not specified yet.
    // We might get here a list of column names mapped to their type needed for the metadata to be
    // stored at the DB.
  }
}
