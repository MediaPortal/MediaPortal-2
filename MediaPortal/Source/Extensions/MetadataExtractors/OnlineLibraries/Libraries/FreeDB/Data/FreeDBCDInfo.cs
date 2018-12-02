#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using System.Text;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Freedb.Data
{
	/// <summary>
	/// Contains Information about a CD
	/// </summary>
	public class FreeDBCDInfo
	{
    private string m_category;
    private string m_discid;
    private string m_title;

		public FreeDBCDInfo()
		{
		}

    public FreeDBCDInfo(string discid, string category, string title)
    {
      m_discid = discid;
      m_category = category;
      m_title = title;
    }

    public string Category
    {
      get
      {
        return m_category;
      }
      set
      {
        m_category = value;
      }
    }

    public string DiscId
    {
      get
      {
        return m_discid;
      }
      set
      {
        m_discid = value;
      }
    }

    public string Title
    {
      get
      {
        return m_title;
      }
      set
      {
        m_title = value;
      }
    }

    public override string ToString()
    {
      StringBuilder buff = new StringBuilder(100);
      buff.Append("DiscId: ");
      buff.Append(m_discid);
      buff.Append("; Category: ");
      buff.Append(m_category);
      buff.Append("; Title: ");
      buff.Append(m_title);
      return buff.ToString();
    }

	}
}
