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

using System;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageCore.Package
{
  public interface ICheckable
  {
    /// <summary>
    /// Gets the name of this element
    /// </summary>
    string ElementsName { get; }

    /// <summary>
    /// Checks the element 
    /// </summary>
    /// <param name="log">Logger.</param>
    /// <returns>Returns <c>true</c> if the package has no errors.</returns>
    bool CheckElements(ILogger log);
  }

  public static class CheckableExtensions
  {
    public static bool CheckNotNullAndContent(this ICheckable checkable, ICheckable element, string elementName, ILogger log)
    {
      if (element == null)
      {
        if (log != null)
        {
          log.Error("{0}: Element '{1}' is missing", checkable.ElementsName);
        }
        return false;
      }
      return element.CheckElements(log);
    }

    public static bool CheckNotNullOrEmpty(this ICheckable checkable, string propertyValue, string propertyName, ILogger log)
    {
      if (String.IsNullOrEmpty(propertyValue))
      {
        if (log != null)
        {
          log.Error("{0}: Attribute '{1}' must not be null or empty", checkable.ElementsName);
        }
        return false;
      }
      return true;
    }
  }
}