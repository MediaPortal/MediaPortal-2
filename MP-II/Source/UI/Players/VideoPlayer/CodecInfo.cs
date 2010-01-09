#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace Ui.Players.Video
{
  public class CodecInfo: IComparable
  {
    #region Variables

    private CodecHandler.CodecCapabilities _capabilities;
    private String _name;
    private Boolean _preferred;
    private String _CLSID;

    #endregion

    #region Properties

    public CodecHandler.CodecCapabilities Capabilities
    {
      get
      {
        return _capabilities;
      }
      set
      {
        _capabilities = value;
      }
    }

    public String Name
    {
      get
      {
        return _name;
      }
      set
      {
        _name = value;
      }
    }

    public String CLSID
    {
      get
      {
        return _CLSID;
      }
      set
      {
        _CLSID = value;
      }
    }

    public Boolean Preferred
    {
      get
      {
        return _preferred;
      }
      set
      {
        _preferred = value;
      }
    }

    #endregion

    #region Constructor

    public CodecInfo()
    {

    }

    public CodecInfo(String CodecName, CodecHandler.CodecCapabilities CodecCapabilities)
    {
      _name = CodecName;
      _capabilities = CodecCapabilities;
    }

    public CodecInfo(String CodecName, CodecHandler.CodecCapabilities CodecCapabilities, String CodecCLSID)
      : this(CodecName, CodecCapabilities)
    {
      _CLSID = CodecCLSID;
    }

    #endregion

    #region Overrides

    public override String ToString()
    {
      return String.Format("{0} [{1}] preferred: {2}", _name, _capabilities.ToString(), _preferred);
    }

    #endregion


    #region IComparable Member

    public int CompareTo(object obj)
    {
      if (! (obj is CodecInfo) ) return -1;
      if (this.Preferred && !((CodecInfo)obj).Preferred) return -1;
      if (!this.Preferred && ((CodecInfo)obj).Preferred) return +1;
      return 0;
    }

    #endregion
  }
}
