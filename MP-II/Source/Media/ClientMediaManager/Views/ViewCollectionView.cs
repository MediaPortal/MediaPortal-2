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

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// View which only contains a configurable list of subviews and no media items.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ViewCollectionView : View
  {
    #region Protected fields

    protected string _displayName;
    protected List<View> _viewCollection = new List<View>();

    #endregion

    #region Ctor

    public ViewCollectionView(string displayName, View parentView) : base(parentView, new Guid[] { })
    {
      _displayName = displayName;
    }

    #endregion

    #region Base overrides

    [XmlIgnore]
    public override string DisplayName
    {
      get { return _displayName; }
    }

    internal override void Loaded(View parentView)
    {
      base.Loaded(parentView);
      foreach (View subView in _viewCollection)
        subView.Loaded(this);
    }

    protected override IList<MediaItem> ReLoadItems() { return new List<MediaItem>(); }

    protected override IList<View> ReLoadSubViews()
    {
      return _viewCollection;
    }

    public override ICollection<Guid> MediaItemAspectIds
    {
      get { return new List<Guid>(); }
    }

    #endregion

    #region Additional members for the XML serialization

    // Serialization of view collection views works like this:
    // We simply serialize our name and all our sub views.

    internal ViewCollectionView() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("DisplayName", IsNullable = false)]
    public string XML_DisplayName
    {
      get { return _displayName; }
      set { _displayName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("ViewCollection", IsNullable = false)]
    [XmlArrayItem("LocalShareView", typeof(LocalShareView))]
    [XmlArrayItem("MediaLibraryView", typeof(MediaLibraryView))]
    [XmlArrayItem("ViewCollectionView", typeof(ViewCollectionView))]
    public List<View> XML_ViewCollection
    {
      get { return _viewCollection; }
      set { _viewCollection = value; }
    }

    #endregion
  }
}
