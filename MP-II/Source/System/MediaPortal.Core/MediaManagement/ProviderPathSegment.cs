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
  public class ProviderPathSegment
  {
    protected Guid _providerId;
    protected string _path;
    protected bool _isBaseSegment;

    public ProviderPathSegment(Guid providerId, string path, bool isBaseSegment)
    {
      _providerId = providerId;
      _path = path;
      _isBaseSegment = isBaseSegment;
    }

    protected static string EscapePath(string path)
    {
      return path.Replace("%", "%25").Replace(">", "%3E").Replace("<", "%3C");
    }

    protected static string UnescapePath(string escapedPath)
    {
      return escapedPath.Replace("%3C", "<").Replace("%3E", ">").Replace("%25", "%");
    }

    public Guid ProviderId
    {
      get { return _providerId; }
    }

    public string Path
    {
      get { return _path; }
    }

    public bool IsBaseSegment
    {
      get { return _isBaseSegment; }
    }

    public string Serialize()
    {
      return (_isBaseSegment ? string.Empty : ">" ) + _providerId.ToString("B") + "://" + EscapePath(_path);
    }

    public static ProviderPathSegment Deserialize(string ppsStr)
    {
      ppsStr = ppsStr.Trim();
      bool isBaseSegment = true;
      if (ppsStr.StartsWith(">"))
      {
        isBaseSegment = false;
        ppsStr = ppsStr.Substring(1).Trim();
      }
      int index = ppsStr.IndexOf("://");
      if (index == -1)
        throw new ArgumentException("ProviderPathSegment cannot be deserialized from string '{0}', missing '://' separator", ppsStr);
      Guid providerId = new Guid(ppsStr.Substring(0, index));
      string path = UnescapePath(ppsStr.Substring(index + 3));
      return new ProviderPathSegment(providerId, path, isBaseSegment);
    }
  }
}