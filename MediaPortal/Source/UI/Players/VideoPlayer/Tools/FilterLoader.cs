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
using System.IO;
using System.Reflection;
using DirectShow;

namespace MediaPortal.UI.Players.Video.Tools
{
  /// <summary>
  /// <see cref="FilterLoader"/> is a helper class to load <see cref="IBaseFilter"/>s from any .dll. It's not needed that the filter is registered.
  /// </summary>
  public static class FilterLoader
  {
    /// <summary>
    /// Loads an COM .dll and creates an instance of the given Interface with IID <paramref name="interfaceId"/>.
    /// </summary>
    /// <param name="dllName">Filename of a .dll or .ax component</param>
    /// <param name="interfaceId">Interface to create an object instance for</param>
    /// <param name="useAssemblyRelativeLocation">Combine the given file name to a full path</param>
    /// <returns>Instance or <c>null</c></returns>
    public static FilterFileWrapper LoadFilterFromDll(string dllName, Guid interfaceId, bool useAssemblyRelativeLocation = false)
    {
      // Get a ClassFactory for our classID
      string dllPath = useAssemblyRelativeLocation ? BuildAssemblyRelativePath(dllName) : dllName;
      return new FilterFileWrapper(dllPath, interfaceId);
    }

    /// <summary>
    /// Builds a full path for a given <paramref name="fileName"/> that is located in the same folder as the <see cref="Assembly.GetCallingAssembly"/>.
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <returns>Combined path</returns>
    public static string BuildAssemblyRelativePath(string fileName)
    {
      string executingPath = Assembly.GetCallingAssembly().Location;
      return Path.Combine(Path.GetDirectoryName(executingPath), fileName);
    }
  }
}
