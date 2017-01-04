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

using System.IO;
using System.Xml.Serialization;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Services.PluginManager.Builders;

namespace MediaPortal.UiComponents.Media.Extensions
{
  /// <summary>
  /// Plugin item builder for <c>MediaNavigationFilter</c> plugin items.
  /// </summary>
  public class MediaNavigationFilterBuilder : IPluginItemBuilder
  {
    public const string MEDIA_FILTERS_PATH = "/Media/Filters";

    protected static XmlSerializer _serializer;

    #region IPluginItemBuilder Member

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      BuilderHelper.CheckParameter("ClassName", itemData);
      BuilderHelper.CheckParameter("Filter", itemData);
      // Support for simplified escaping inside XML tag
      string filter = itemData.Attributes["Filter"].Replace("{", "<").Replace("}", ">");
      FilterWrapper wrapper;

      if (_serializer == null)
        _serializer = new XmlSerializer(typeof(FilterWrapper));

      using (var reader = new StringReader(filter))
        wrapper = (FilterWrapper)_serializer.Deserialize(reader);

      return new MediaNavigationFilter(itemData.Attributes["ClassName"], wrapper.Filter);
    }

    public void RevokeItem(object item, PluginItemMetadata itemData, PluginRuntime plugin)
    {
      // Noting to do
    }

    public bool NeedsPluginActive(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      return true;
    }

    #endregion
  }

  /// <summary>
  /// <see cref="MediaNavigationFilter"/> holds navigation filter metadata.
  /// </summary>
  public class MediaNavigationFilter
  {
    /// <summary>
    /// Gets the class name for which the filter should be applied.
    /// </summary>
    public string ClassName { get; private set; }

    /// <summary>
    /// Gets the filter.
    /// </summary>
    public IFilter Filter { get; set; }

    public MediaNavigationFilter(string className, IFilter filter)
    {
      ClassName = className;
      Filter = filter;
    }
  }
}
