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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Views
{
  /// <summary>
  /// Holds the building instructions for creating a collection of media items and sub views.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A view specification is an abstract construct which will be implemented concrete in subclasses.
  /// It specifies a list of media items, for example by a database query, or by a hard disc location.
  /// A view specification can create n concrete views, which then will reference to their view specification.
  /// This view specification itself doesn't hold any references to its created views.
  /// The view contents may be ordered or not.<br/>
  /// </para>
  /// <para>
  /// Views are built on demand from a <see cref="ViewSpecification"/> which comes from a media module. Some media
  /// modules might persist their configured <see cref="ViewSpecification"/> structure by their own.
  /// </para>
  /// <para>
  /// Note: This class and its subclasses are serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public abstract class ViewSpecification
  {
    protected string _viewDisplayName;
    protected HashSet<Guid> _mediaItemAspectIds;

    protected ViewSpecification(string viewDisplayName, IEnumerable<Guid> mediaItemAspectIds)
    {
      _viewDisplayName = viewDisplayName;
      _mediaItemAspectIds = new HashSet<Guid>(mediaItemAspectIds);
      if (!_mediaItemAspectIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
    }

    /// <summary>
    /// Builds a new view from this view specification, which is a root view (i.e. without a parent view).
    /// </summary>
    public View BuildRootView()
    {
      return new View(null, this);
    }

    /// <summary>
    /// Returns the media item aspects whose data is contained in this view.
    /// </summary>
    public ICollection<Guid> MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
    }

    /// <summary>
    /// Returns the display name of the created view.
    /// </summary>
    [XmlIgnore]
    public virtual string ViewDisplayName
    {
      get { return _viewDisplayName; }
    }

    /// <summary>
    /// Returns the information if the view specified by this instance currently can be built (i.e. if all of its
    /// providers/shares are present).
    /// </summary>
    public abstract bool CanBeBuilt { get; }

    /// <summary>
    /// Loads or reloads the items of for a view to this specification. This will re-request the database or datastore for
    /// the media items.
    /// </summary>
    /// <remarks>
    /// This method will load the media items of a view specified by this <see cref="ViewSpecification"/>.
    /// It will load all of the specified media item aspects which are available for the media items.
    /// <i>Hint:</i>
    /// The uppercase L of the name is no spelling error; it denotes that this method is for
    /// Loading and Reloading of media items.
    /// </remarks>
    /// <returns>Media items in a view specified by this specification.</returns>
    internal abstract IEnumerable<MediaItem> ReLoadItems();

    /// <summary>
    /// Loads or reloads the specifications of the sub views to this specification. This will rebuild the
    /// sub view specifications by re-requesting the database or datastore, if necessary.
    /// </summary>
    /// <remarks>
    /// <i>Hint:</i>
    /// The uppercase L of the name is no spelling error; it denotes that this method is for
    /// Loading and Reloading of sub views.
    /// </remarks>
    /// <returns>Sub views of a view specified by this specification.</returns>
    internal abstract IEnumerable<ViewSpecification> ReLoadSubViewSpecifications();

    #region Additional members for the XML serialization

    internal ViewSpecification() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlElement("ViewDisplayName", IsNullable = false)]
    public string XML_ViewDisplayName
    {
      get { return _viewDisplayName; }
      set { _viewDisplayName = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlArray("MediaItemAspectIds")]
    public HashSet<Guid> XML_MediaItemAspectIds
    {
      get { return _mediaItemAspectIds; }
      set { _mediaItemAspectIds = value; }
    }

    #endregion
  }
}
