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

using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Filter which matches the media item virtual flag.
  /// </summary>
  public class VirtualFilter : IFilter
  {
    protected bool _includeVirtual;

    public VirtualFilter(bool includeVirtual)
    {
      _includeVirtual = includeVirtual;
    }

    [XmlIgnore]
    public bool IncludeVirtual
    {
      get { return _includeVirtual; }
      set { _includeVirtual = value; }
    }

    public override string ToString()
    {
      return "IsVirtual = " + (_includeVirtual ? "1" : "0");
    }

    #region Additional members for the XML serialization

    internal VirtualFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("IncludeVirtual")]
    public bool XML_IncludeVirtual
    {
      get { return _includeVirtual; }
      set { _includeVirtual = value; }
    }

    #endregion
  }
}
