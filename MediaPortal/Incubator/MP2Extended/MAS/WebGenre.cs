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

namespace MediaPortal.Plugins.MP2Extended.MAS
{
  public class WebGenre : WebObject, ITitleSortable
  {
    public string Title { get; set; }

    public WebGenre()
    {
    }

    public WebGenre(string title)
    {
      Title = title;
    }

    public override string ToString()
    {
      return Title;
    }

    public override bool Equals(object obj)
    {
      WebGenre r = obj is string ? new WebGenre((string)obj) : obj as WebGenre;
      return (object)r != null && this.Title == r.Title;
    }

    public override int GetHashCode()
    {
      return Title.GetHashCode();
    }

    public static bool operator ==(WebGenre a, WebGenre b)
    {
      return ReferenceEquals(a, b) || (((object)a) != null && ((object)b) != null && a.Title == b.Title);
    }

    public static bool operator !=(WebGenre a, WebGenre b)
    {
      return !(a == b);
    }

    public static implicit operator WebGenre(string value)
    {
      return new WebGenre(value);
    }

    public static implicit operator string(WebGenre value)
    {
      return value.Title;
    }
  }
}
