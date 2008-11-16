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

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Interface for transmitting import jobs.
  /// </summary>
  public interface IImporter
  {
    /// <summary>
    /// Triggers an asynchronous import of the specified import location. The import results will be written into
    /// the system's registered <see cref="IMediaDatabase"/> instance, if there is one.
    /// </summary>
    /// <param name="shareId">Id of the share to import from. If set to <c>null</c>, all shares
    /// will be imported, else the <paramref name="path"/> of the specified share will be imported.</param>
    /// <param name="path">Path to be imported from the share with the specified <paramref name="shareId"/>.
    /// This parameter will be ignored if <paramref name="shareId"/> is set to <c>null</c>.</param>
    void ForceImport(Guid? shareId, string path);
}
