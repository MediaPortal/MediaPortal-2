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

using System;

namespace UPnP.Infrastructure.Common
{
  public class HTTPVersion
  {
    public const string VERSION_PREFIX = "HTTP/";

    protected int _verMax;
    protected int _verMin;

    public HTTPVersion(int verMax, int verMin)
    {
      _verMax = verMax;
      _verMin = verMin;
    }

    public int VerMax
    {
      get { return _verMax; }
    }

    public int VerMin
    {
      get { return _verMin; }
    }

    public static HTTPVersion Parse(string versionStr)
    {
      HTTPVersion result;
      if (!TryParse(versionStr, out result))
        throw new ArgumentException(string.Format("HTTP version string '{0}' cannot be parsed", versionStr));
      return result;
    }

    public static bool TryParse(string versionStr, out HTTPVersion result)
    {
      result = null;
      int dotIndex = versionStr.IndexOf('.');
      if (!versionStr.StartsWith(VERSION_PREFIX) || dotIndex < VERSION_PREFIX.Length + 1)
        return false;
      int verMin;
      if (!int.TryParse(versionStr.Substring(VERSION_PREFIX.Length, dotIndex - VERSION_PREFIX.Length), out verMin))
        return false;
      int verMax;
      if (!int.TryParse(versionStr.Substring(dotIndex + 1), out verMax))
        return false;
      result = new HTTPVersion(verMax, verMin);
      return true;
    }

    public override string ToString()
    {
      return VERSION_PREFIX + _verMax + "." + _verMin;
    }
  }
}
