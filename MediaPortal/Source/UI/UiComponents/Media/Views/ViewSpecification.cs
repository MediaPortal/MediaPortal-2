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
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Media.Views
{
  /// <summary>
  /// Holds the building instructions for creating a collection of media items and sub views.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A view specification is an abstract construct which will be implemented in subclasses in a more special way.
  /// It specifies a list of media items, for example by a database query, or by a hard disc location.
  /// A view specification can be instantiated to a concrete view, which then will reference to its view specification.
  /// This view specification itself doesn't hold any references to its created views.
  /// The view's contents may be ordered or not.<br/>
  /// </para>
  /// <para>
  /// Views are built on demand from a <see cref="ViewSpecification"/> which comes from a media module. Some media
  /// modules might persist their configured <see cref="ViewSpecification"/> structures by their own.
  /// </para>
  /// </remarks>
  public abstract class ViewSpecification
  {
    protected string _viewDisplayName;
    protected ICollection<Guid> _necessaryMIATypeIds;
    protected ICollection<Guid> _optionalMIATypeIds;

    public delegate void ItemsListSortingDelegate(ItemsList itemsList, Sorting sorting);

    /// <summary>
    /// Provides a way for sorting the final result list, which can consist of both MediaItems and SubViews.
    /// This delegate needs to be set by a ViewSpecification, if a custom logic is required.
    /// </summary>
    public ItemsListSortingDelegate CustomItemsListSorting { get; protected set; }

    protected ViewSpecification(string viewDisplayName,
        IEnumerable<Guid> necessaryMIATypeIds, IEnumerable<Guid> optionalMIATypeIds)
    {
      _viewDisplayName = viewDisplayName;
      _necessaryMIATypeIds = necessaryMIATypeIds == null ? new HashSet<Guid>() : new HashSet<Guid>(necessaryMIATypeIds);
      _optionalMIATypeIds = optionalMIATypeIds == null ? new HashSet<Guid>() : new HashSet<Guid>(optionalMIATypeIds);
      if (!_necessaryMIATypeIds.Contains(ProviderResourceAspect.ASPECT_ID))
        _necessaryMIATypeIds.Add(ProviderResourceAspect.ASPECT_ID);
    }

    /// <summary>
    /// Instantiates this view specification to a new view.
    /// </summary>
    public View BuildView()
    {
      return new View(this);
    }

    /// <summary>
    /// Returns the IDs of media item aspects which need to be present in all media items contained in this view.
    /// </summary>
    public ICollection<Guid> NecessaryMIATypeIds
    {
      get { return _necessaryMIATypeIds; }
    }

    /// <summary>
    /// Returns the IDs of media item aspects which may be present in all media items contained in this view.
    /// </summary>
    public ICollection<Guid> OptionalMIATypeIds
    {
      get { return _optionalMIATypeIds; }
    }

    /// <summary>
    /// Returns the display name of the created view.
    /// </summary>
    public virtual string ViewDisplayName
    {
      get { return _viewDisplayName; }
    }

    /// <summary>
    /// Returns the information if the view specified by this instance currently can be built (i.e. if all of its
    /// providers/shares are present). If the task to check that completely is too complicated, implementors can also
    /// return <c>true</c> and later fail in method <see cref="ReLoadItemsAndSubViewSpecifications"/>.
    /// </summary>
    public abstract bool CanBeBuilt { get; }

    /// <summary>
    /// Returns the absolute number of items and child items of this view specification. If this value cannot easily be evaluated, returns <c>null</c>.
    /// </summary>
    public virtual int? AbsNumItems
    {
      get { return null; }
    }

    /// <summary>
    /// Returns all media items of this view and all sub views. Can be overridden to provide a more efficient implementation.
    /// </summary>
    /// <returns>Enumeration of all media items of this and all sub views.</returns>
    public virtual IEnumerable<MediaItem> GetAllMediaItems()
    {
      IList<MediaItem> mis;
      IList<ViewSpecification> vss;
      ReLoadItemsAndSubViewSpecifications(out mis, out vss);
      return vss.SelectMany(subViewSpecification => subViewSpecification.GetAllMediaItems()).Union(mis);
    }

    /// <summary>
    /// Indicates if the list of subviews uses an own sorting. If set to <c>true</c>, the list will not be sorted later.
    /// </summary>
    public bool SortedSubViews { get; set; }

    /// <summary>
    /// Loads or reloads the items and sub view specifications for a view to this specification.
    /// This will re-request the media database or datastore.
    /// </summary>
    /// <remarks>
    /// This method will load the media items and sub view specifications of a view specified by this
    /// <see cref="ViewSpecification"/>.
    /// It will load all of the specified media item aspects which are available for the media items.
    /// <i>Hint:</i>
    /// The uppercase L of the name is no spelling error; it denotes that this method is for
    /// Loading and Reloading of media items.
    /// </remarks>
    /// <param name="mediaItems">Media items to this view specification. <c>null</c> if the loading of items didn't succeed.</param>
    /// <param name="subViewSpecifications">Sub view specifications to this view specification. <c>null</c> if the loading of sub views didn't succeed.</param>
    /// <exception cref="Exception">If there are problems accessing the datasource of this view. Exceptions in reading
    /// and/or parsing media items should not be thrown; those media items should simply be ignored.</exception>
    protected internal abstract void ReLoadItemsAndSubViewSpecifications(out IList<MediaItem> mediaItems, out IList<ViewSpecification> subViewSpecifications);

    public virtual IViewChangeNotificator CreateChangeNotificator()
    {
      return null;
    }
  }
}
