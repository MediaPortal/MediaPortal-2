#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using System.Collections.Generic;

namespace Webradio.Settings
{
  public class Filter
  {
    public string Titel;
    public List<string> Countrys;
    public List<string> Genres;


    public Filter()
    {
      Titel = "";
      Countrys = new List<string>();
      Genres = new List<string>();
    }

    public Filter(string titel, List<string> countrys, List<string> genres)
    {
      Titel = titel;
      Countrys = countrys;
      Genres = genres;
    }
  }
}
