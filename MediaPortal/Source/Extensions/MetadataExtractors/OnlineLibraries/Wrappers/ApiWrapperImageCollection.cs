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

using System;
using System.Collections.Generic;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  public class ApiWrapperImageCollection<T>
  {
    public string Id = null;
    public List<T> Backdrops = new List<T>();
    public List<T> Banners = new List<T>();
    public List<T> Posters = new List<T>();
    public List<T> DiscArt = new List<T>();
    public List<T> ClearArt = new List<T>();
    public List<T> Logos = new List<T>();
    public List<T> Thumbnails = new List<T>();
    public List<T> Covers = new List<T>();
  }
}
