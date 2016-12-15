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

using System.Collections.Generic;

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Represents one node of the media category hierarchy. Examples for media categories are "music", "audio drama", "other audio", all with parent category "audio" or
  /// "movie", "series", "other video", all with parent category "video". Metadata extractors are assigned to media categories. Media categories are structured in a hierarchy,
  /// i.e. if a media category is assigned to a share, all metadata extractors for that category and of all direct and indirect parent categories are applied to that share.
  /// </summary>
  /// <remarks>
  /// Note that media categories are modelled as objects which are held locally in an MP2 system; they are not shared across different MP2 systems attached to a server.
  /// The reason is, the hierarchy information of a media category is only needed on the same system of a share of that category, it isn't needed in other systems.
  /// Media categories other than the default categories are in general provided by plugins which also provide metadata extractors.
  /// So if such a plugin is registered on a special MP2 system, that system will be aware of that media category because
  /// the plugin is forced to register that category. Thus, shares of that system can be added with that category because the category is known in that system. Furthermore,
  /// that system will be able to resolve the set of metadata extractors which are responsible for shares of that media category.
  /// For other systems, the category will just remain a string which is assigned to a share, without hierarchy information.
  /// </remarks>
  public class MediaCategory
  {
    #region Protected fields

    protected string _name;
    protected ICollection<MediaCategory> _parentCategories;

    #endregion

    public MediaCategory(string name, IEnumerable<MediaCategory> parentCategories)
    {
      _name = name;
      _parentCategories = parentCategories == null ? new List<MediaCategory>() : new List<MediaCategory>(parentCategories);
    }

    public string CategoryName
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets all parent media categories of this category.
    /// </summary>
    public ICollection<MediaCategory> ParentCategories
    {
      get { return _parentCategories; }
    }

    public override int GetHashCode()
    {
      return _name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      MediaCategory other = obj as MediaCategory;
      return other != null && _name.Equals(other._name);
    }

    public override string ToString()
    {
      return _name;
    }
  }
}
