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

namespace MediaPortal.Plugins.MP2Extended.Common
{
  public class WebIntResult
  {
    public int Result { get; set; }

    public WebIntResult()
    {
    }

    public WebIntResult(int value)
    {
      Result = value;
    }

    public override string ToString()
    {
      return Result.ToString();
    }

    public override bool Equals(object obj)
    {
      WebIntResult r = obj is string ? new WebIntResult((int)obj) : obj as WebIntResult;
      return (object)r != null && this.Result == r.Result;
    }

    public override int GetHashCode()
    {
      return Result.GetHashCode();
    }

    public static bool operator ==(WebIntResult a, WebIntResult b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Result == b.Result);
    }

    public static bool operator !=(WebIntResult a, WebIntResult b)
    {
      return !(a == b);
    }

    public static implicit operator WebIntResult(int value)
    {
      return new WebIntResult(value);
    }

    public static implicit operator int(WebIntResult value)
    {
      return value.Result;
    }
  }
}
