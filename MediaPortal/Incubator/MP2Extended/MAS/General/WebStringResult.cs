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

namespace MediaPortal.Plugins.MP2Extended.MAS.General
{
  public class WebStringResult
  {
    public string Result { get; set; }

    public WebStringResult()
    {
    }

    public WebStringResult(string value)
    {
      Result = value;
    }

    public override string ToString()
    {
      return Result;
    }

    public override bool Equals(object obj)
    {
      WebStringResult r = obj is string ? new WebStringResult((string)obj) : obj as WebStringResult;
      return (object)r != null && this.Result == r.Result;
    }

    public override int GetHashCode()
    {
      return Result.GetHashCode();
    }

    public static bool operator ==(WebStringResult a, WebStringResult b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Result == b.Result);
    }

    public static bool operator !=(WebStringResult a, WebStringResult b)
    {
      return !(a == b);
    }

    public static implicit operator WebStringResult(string value)
    {
      return new WebStringResult(value);
    }

    public static implicit operator string(WebStringResult value)
    {
      return value.Result;
    }
  }
}
