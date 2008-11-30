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

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaPortal.Media.ClientMediaManager.Views
{
  /// <summary>
  /// Storage data object for holding all local view configuration.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class ViewsStorage
  {
    #region Protected fields

    protected List<ViewMetadata> _views = new List<ViewMetadata>();
    protected Guid _rootView;

    #endregion

    /// <summary>
    /// Gets or sets the root view's id.
    /// </summary>
    public Guid RootViewId
    {
      get { return _rootView; }
      set { _rootView = value; }
    }

    /// <summary>
    /// Gets or sets the collection of view metadata descriptors for all available views.
    /// </summary>
    [XmlIgnore]
    public ICollection<ViewMetadata> Views
    {
      get { return _views; }
    }

    #region Additional Additional members for the XML serialization

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("Views")]
    [XmlArrayItem("LocalShareView", Type = typeof(LocalShareViewMetadata))]
    [XmlArrayItem("MediaLibraryView", Type = typeof(MediaLibraryViewMetadata))]
    [XmlArrayItem("ViewCollectionView", Type = typeof(ViewCollectionViewMetadata))]
    public List<ViewMetadata> XML_Views
    {
      get { return _views; }
      set { _views = value; }
    }

    #endregion
  }
}