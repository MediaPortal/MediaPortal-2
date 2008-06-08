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

using System.Collections.Generic;

namespace Presentation.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Resource dictionary which holds MPF application resources like themes.
  /// This class is a singleton class.
  /// </summary>
  public class ApplicationResources: Dictionary<string, object>
  {
    protected static ApplicationResources _instance = new ApplicationResources();

    public static ApplicationResources Instance
    {
      get { return _instance; }
    }

    public void Merge(IDictionary<string, object> dict)
    {
      IEnumerator<KeyValuePair<string, object>> enumer = dict.GetEnumerator();
      while (enumer.MoveNext())
        this[enumer.Current.Key] = enumer.Current.Value;
    }
  }
}
