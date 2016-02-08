#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MediaServer.Objects.Basic;

namespace MediaPortal.Plugins.MediaServer.Filters
{
  public class GenericContentDirectoryFilter
  {
    public enum ContentFilter
    {
      GenericContentFilter,
      SamsungContentFilter,
      XBoxContentFilter,
      WMPContentFilter,
      SimpleContentFilter
    }

    public static GenericContentDirectoryFilter GetContentFilter(ContentFilter Filter)
    {
      if (Filter == ContentFilter.SamsungContentFilter)
      {
        return new SamsungContentDirectoryFilter();
      }
      else if (Filter == ContentFilter.XBoxContentFilter)
      {
        return new XBoxContentDirectoryFilter();
      }
      else if (Filter == ContentFilter.WMPContentFilter)
      {
        return new WMPContentDirectoryFilter();
      }
      else if (Filter == ContentFilter.SimpleContentFilter)
      {
        return new SimpleContentDirectoryFilter();
      }
      return new GenericContentDirectoryFilter();
    }

    public virtual string FilterObjectId(string requestedNodeId, bool isSearch)
    {
      return requestedNodeId;
    }

    public virtual void FilterContainerClassType(string objectId, ref BasicObject container)
    {

    }

    public virtual void FilterClassProperties(string objectId, ref BasicObject container)
    {
      if (container is BasicContainer)
      {
        ((BasicContainer)container).Searchable = false;
      }
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
