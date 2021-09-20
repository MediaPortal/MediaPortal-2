#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

namespace MediaPortal.Plugins.MP2Extended.Common
{
  public class WebBoolResult
  {
    public bool Result { get; set; }

    public WebBoolResult()
    {
    }

    public WebBoolResult(bool value)
    {
      Result = value;
    }

    public override string ToString()
    {
      return Result.ToString();
    }

    public override bool Equals(object obj)
    {
      WebBoolResult r = obj is bool ? new WebBoolResult((bool)obj) : obj as WebBoolResult;
      return (object)r != null && this.Result == r.Result;
    }

    public override int GetHashCode()
    {
      return Result ? 1 : 0;
    }

    public static bool operator ==(WebBoolResult a, WebBoolResult b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Result == b.Result);
    }

    public static bool operator !=(WebBoolResult a, WebBoolResult b)
    {
      return !(a == b);
    }

    public static implicit operator WebBoolResult(bool value)
    {
      return new WebBoolResult(value);
    }

    public static implicit operator bool(WebBoolResult value)
    {
      return value.Result;
    }
  }
}
