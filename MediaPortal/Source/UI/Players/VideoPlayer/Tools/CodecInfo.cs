#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using DirectShowLib;

namespace MediaPortal.UI.Players.Video.Tools
{
  public class CodecInfo: IComparable
  {
    #region Properties

    public String Name { get; set; }

    public String CLSID { get; set; }

    public bool Preferred { get; set; }

    #endregion

    #region Constructor

    public CodecInfo() {}

    public CodecInfo(String codecName)
    {
      Name = codecName;
    }

    public CodecInfo(String codecName, DsGuid codecClsid) :
        this(codecName)
    {
      CLSID = codecClsid.ToString();
    }

    #endregion

    #region Overrides

    public override String ToString()
    {
      return String.Format("{0} [{1}]", Name, CLSID);
    }

    #endregion

    #region Methods

    public DsGuid GetCLSID()
    {
      return DsGuid.FromGuid(
          String.IsNullOrEmpty(CLSID)
              ? Guid.Empty
              : new Guid(CLSID));
    }

    #endregion

    #region IComparable Member

    public int CompareTo(object obj)
    {
      if (! (obj is CodecInfo) ) return -1;
      if (Preferred && !((CodecInfo)obj).Preferred) return -1;
      if (!Preferred && ((CodecInfo)obj).Preferred) return +1;
      return Name.CompareTo(((CodecInfo)obj).Name);
    }

    #endregion
  }
}