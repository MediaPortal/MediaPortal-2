#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Core.MediaManagement;
using System;

namespace MediaPortal.Views
{
  /// <summary>
  /// View specification which defining a view which only contains a configurable list of subviews and no media items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ViewCollectionViewSpecification : ViewSpecification
  {
    #region Protected fields

    protected List<ViewSpecification> _subViews = new List<ViewSpecification>();

    #endregion

    #region Ctor

    public ViewCollectionViewSpecification(string viewDisplayName, ICollection<Guid> mediaItemAspectIds) :
        base(viewDisplayName, mediaItemAspectIds) { }

    #endregion

    public void AddSubView(ViewSpecification subView)
    {
      _subViews.Add(subView);
    }

    public void RemoveSubView(ViewSpecification subView)
    {
      _subViews.Remove(subView);
    }

    #region Base overrides

    [XmlIgnore]
    public override bool CanBeBuilt
    {
      get { return true; }
    }

    internal override IEnumerable<MediaItem> ReLoadItems()
    {
      yield break;
    }

    internal override IEnumerable<ViewSpecification> ReLoadSubViewSpecifications()
    {
      return _subViews;
    }

    #endregion

    #region Additional members for the XML serialization

    // Serialization of view collection views works like this:
    // We simply serialize our name and all our sub views.

    internal ViewCollectionViewSpecification() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("SubViews", IsNullable = false)]
    [XmlArrayItem("LocalDirectoryViewSpecification", typeof(LocalDirectoryViewSpecification))]
    [XmlArrayItem("MediaLibraryViewSpecification", typeof(MediaLibraryViewSpecification))]
    [XmlArrayItem("ViewCollectionViewSpecification", typeof(ViewCollectionViewSpecification))]
    public List<ViewSpecification> XML_SubViews
    {
      get { return _subViews; }
      set { _subViews = value; }
    }

    #endregion
  }
}
