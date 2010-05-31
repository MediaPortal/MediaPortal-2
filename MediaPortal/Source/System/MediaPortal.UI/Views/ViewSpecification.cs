#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UI.Views
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
  /// </remarks>
  public abstract class ViewSpecification
  {
    protected string _viewDisplayName;
    protected ICollection<Guid> _necessaryMIATypeIds;
    protected ICollection<Guid> _optionalMIATypeIds;

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
    /// Builds a new rooted view from this view specification (i.e. without a parent view).
    /// </summary>
    public View BuildRootView()
    {
      return new View(null, this);
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
    /// return <c>true</c> and later fail in the methods <see cref="ReLoadItems"/> and/or
    /// <see cref="ReLoadSubViewSpecifications"/>.
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
    /// <exception cref="Exception">If there are problems accessing the datasource of this view. Exceptions in reading
    /// and/or parsing media items should not be thrown; those media items should simply be ignored.</exception>
    protected internal abstract IEnumerable<MediaItem> ReLoadItems();

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
    /// <exception cref="Exception">If there are problems accessing the datasource of this view.</exception>
    protected internal abstract IEnumerable<ViewSpecification> ReLoadSubViewSpecifications();
  }
}
