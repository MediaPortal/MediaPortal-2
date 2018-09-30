#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Determines whether the <see cref="MediaItemQueryExtension"/> updates the target
  /// property with an enumeration of media items or a single media item. 
  /// </summary>
  public enum MediaItemQueryMode
  {
    /// <summary>
    /// Updates the target property with a single media item.
    /// </summary>
    SingleItem,

    /// <summary>
    /// Updates the target property with an enumeration of media items.
    /// </summary>
    MultipleItems,

    /// <summary>
    /// Updates the target property with either an enumeration of media items 
    /// or a single media item depending on the target property's type.
    /// </summary>
    Default
  }

  /// <summary>
  /// Markup extension which performs a media item query and
  /// updates the target property with the returned <see cref="MediaItem"/>(s).
  /// </summary>
  public class MediaItemQueryExtension : BindingBase
  {
    #region Protected fields

    protected readonly object _syncObj = new object();
    //Whether we are currently updating properties
    protected bool _isUpdating = false;
    //The last updated value, used to avoid multiple updates with the same value
    protected object _lastUpdatedValue = null;
    //The last used filter, used to avoid multiple searches with same filter
    protected IFilter _lastUsedFilter = null;

    protected AbstractProperty _queryModeProperty;
    protected AbstractProperty _necessaryRequestedMIAsProperty;
    protected AbstractProperty _optionalRequestedMIAsProperty;
    protected AbstractProperty _filterProperty;

    #endregion

    #region Ctor/Init

    public MediaItemQueryExtension()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _queryModeProperty = new SProperty(typeof(MediaItemQueryMode), MediaItemQueryMode.Default);
      _necessaryRequestedMIAsProperty = new SProperty(typeof(IEnumerable<Guid>), null);
      _optionalRequestedMIAsProperty = new SProperty(typeof(IEnumerable<Guid>), null);
      _filterProperty = new SProperty(typeof(IFilter), null);
    }

    void Attach()
    {
      _queryModeProperty.Attach(OnPropertyChanged);
      _necessaryRequestedMIAsProperty.Attach(OnPropertyChanged);
      _optionalRequestedMIAsProperty.Attach(OnPropertyChanged);
      _filterProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _queryModeProperty.Detach(OnPropertyChanged);
      _necessaryRequestedMIAsProperty.Detach(OnPropertyChanged);
      _optionalRequestedMIAsProperty.Detach(OnPropertyChanged);
      _filterProperty.Detach(OnPropertyChanged);
    }

    #endregion

    #region Event handlers

    protected void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (_active)
        _ = UpdateAsync();
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Called before performing a query. Can be overridden in
    /// derived classes to update the query filter.
    /// </summary>
    protected virtual void OnBeginUpdate()
    {
    }

    //Asynchronously performs a query using the specified filter
    protected async Task UpdateAsync()
    {
      //Avoid recursive calls, this can happen if the call to BeginUpdate
      //updates one of our properties, triggering another update
      if (_isUpdating)
        return;

      _isUpdating = true;
      try
      {
        OnBeginUpdate();

        if (_lastUsedFilter == Filter)
          return;

        await QueryMediaItems();
      }
      finally
      {
        _isUpdating = false;
      }
    }

    //Performs the actual query and updates the target property with the returned media item(s)
    protected async Task QueryMediaItems()
    {
      IFilter filter = Filter;
      if (filter == null)
      {
        //Set target property to null if invalid to remove any previously assigned media items
        UpdateTargetProperty(null, QueryMode);
        return;
      }

      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;

      _lastUsedFilter = filter;

      MediaItemQueryMode queryMode = QueryMode;
      MediaItemQuery query = new MediaItemQuery(NecessaryRequestedMIAs, OptionalRequestedMIAs, filter);
      if (queryMode == MediaItemQueryMode.SingleItem)
        query.Limit = 1;

      IList<MediaItem> items = await cd.SearchAsync(query, true, GetCurrentUserId(), false);
      UpdateTargetProperty(items, queryMode);
    }

    protected Guid? GetCurrentUserId()
    {
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      return userProfileDataManagement != null && userProfileDataManagement.IsValidUser ?
        userProfileDataManagement.CurrentUser.ProfileId : (Guid?)null;
    }

    protected void UpdateTargetProperty(IList<MediaItem> items, MediaItemQueryMode queryMode)
    {
      IDataDescriptor targetDataDescriptor = _targetDataDescriptor;
      if (targetDataDescriptor == null)
        return;

      object result;
      //Try and update our target property with either a media item enumeration or the first media item
      if ((queryMode != MediaItemQueryMode.SingleItem && TypeConverter.Convert(items, targetDataDescriptor.DataType, out result)) ||
        (queryMode != MediaItemQueryMode.MultipleItems && TypeConverter.Convert(items != null ? items.FirstOrDefault() : null, targetDataDescriptor.DataType, out result)))
      {
        lock (_syncObj)
        {
          //Avoid multiple updates with the same value
          if (ReferenceEquals(_lastUpdatedValue, result))
            return;
          _lastUpdatedValue = result;
        }
        targetDataDescriptor.Value = result;
      }
    }

    #endregion

    #region Base overrides

    public override void Activate()
    {
      if (_active)
        return;
      base.Activate();
      //State may already be valid so try a query now
      _ = UpdateAsync();
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      MediaItemQueryExtension miqe = (MediaItemQueryExtension)source;
      QueryMode = miqe.QueryMode;
      NecessaryRequestedMIAs = miqe.NecessaryRequestedMIAs;
      OptionalRequestedMIAs = miqe.OptionalRequestedMIAs;
      Filter = miqe.Filter;
      Attach();
    }

    public override void Dispose()
    {
      Detach();
      base.Dispose();
    }

    #endregion

    #region GUI Properties

    public AbstractProperty QueryModeProperty
    {
      get { return _queryModeProperty; }
    }

    /// <summary>
    /// Whether the target property should be updated with an enumeration of media items
    /// or a single media item.
    /// </summary>
    public MediaItemQueryMode QueryMode
    {
      get { return (MediaItemQueryMode)_queryModeProperty.GetValue(); }
      set { _queryModeProperty.SetValue(value); }
    }

    public AbstractProperty NecessaryRequestedMIAsProperty
    {
      get { return _necessaryRequestedMIAsProperty; }
    }

    /// <summary>
    /// Enumeration of necessary media item aspect ids.
    /// </summary>
    public IEnumerable<Guid> NecessaryRequestedMIAs
    {
      get { return (IEnumerable<Guid>)_necessaryRequestedMIAsProperty.GetValue(); }
      set { _necessaryRequestedMIAsProperty.SetValue(value); }
    }

    public AbstractProperty OptionalRequestedMIAsProperty
    {
      get { return _optionalRequestedMIAsProperty; }
    }

    /// <summary>
    /// Enumeration of optional media item aspect ids.
    /// </summary>
    public IEnumerable<Guid> OptionalRequestedMIAs
    {
      get { return (IEnumerable<Guid>)_optionalRequestedMIAsProperty.GetValue(); }
      set { _optionalRequestedMIAsProperty.SetValue(value); }
    }

    public AbstractProperty FilterProperty
    {
      get { return _filterProperty; }
    }

    /// <summary>
    /// Filter to use for the query.
    /// </summary>
    public IFilter Filter
    {
      get { return (IFilter)_filterProperty.GetValue(); }
      set { _filterProperty.SetValue(value); }
    }

    #endregion
  }
}
