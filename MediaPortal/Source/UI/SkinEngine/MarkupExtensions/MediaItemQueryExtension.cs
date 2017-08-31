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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Threading;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Markup extension which performs a media item query and
  /// updates the target property with the returned <see cref="MediaItem"/>(s).
  /// </summary>
  public class MediaItemQueryExtension : BindingBase
  {
    #region Protected fields
    
    protected readonly object _syncObj = new object();
    //Whether we are currently performing a query
    protected bool _isUpdating = false;
    //Whether state has changed whilst performing a query -> perform new query when current query has finished
    protected bool _isDirty = false;

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
      _necessaryRequestedMIAsProperty = new SProperty(typeof(IEnumerable<Guid>), null);
      _optionalRequestedMIAsProperty = new SProperty(typeof(IEnumerable<Guid>), null);
      _filterProperty = new SProperty(typeof(IFilter), null);
    }

    void Attach()
    {
      _filterProperty.Attach(OnFilterChanged);
    }

    void Detach()
    {
      _filterProperty.Detach(OnFilterChanged);
    }

    #endregion

    #region Event handlers

    protected void OnFilterChanged(AbstractProperty property, object oldValue)
    {
      if (_active)
        UpdateAsync();
    }

    #endregion

    #region Protected methods

    //Asynchronously performs a query using the specified filter
    protected void UpdateAsync()
    {
      //Check state is valid before invoking a thread pool thread
      if (Filter == null)
        return;
      //Update using the thread pool
      IThreadPool tp = ServiceRegistration.Get<IThreadPool>();
      tp.Add(Update);
    }


    protected void Update()
    {
      lock (_syncObj)
      {
        if (_isUpdating)
        {
          //If we are currently querying, mark as dirty so
          //we can re-update when the current query has finished
          _isDirty = true;
          return;
        }
        _isUpdating = true;
      }

      bool isDirty;
      try
      {
        QueryMediaItems();
      }
      finally
      {
        lock (_syncObj)
        {
          //reset inside lock
          isDirty = _isDirty;
          _isDirty = false;
          _isUpdating = false;
        }
      }

      //update outside lock
      if (isDirty)
        Update();
    }

    //Performs the actual query and updates the target property with the
    //returned media item(s)
    protected void QueryMediaItems()
    {
      IFilter filter = Filter;
      if (filter == null)
        return;

      IContentDirectory cd = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (cd == null)
        return;

      MediaItemQuery query = new MediaItemQuery(NecessaryRequestedMIAs, OptionalRequestedMIAs, filter);
      IList<MediaItem> items = cd.Search(query, true, GetCurrentUserId(), false);

      object result;
      if (_targetDataDescriptor != null)
      {
        //Try and update our target property with either a media item enumeration or the first media item
        if (TypeConverter.Convert(items, _targetDataDescriptor.DataType, out result) ||
          (items != null && TypeConverter.Convert(items.FirstOrDefault(), _targetDataDescriptor.DataType, out result)))
          _targetDataDescriptor.Value = result;
      }
    }

    protected Guid? GetCurrentUserId()
    {
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      return userProfileDataManagement != null && userProfileDataManagement.IsValidUser ?
        userProfileDataManagement.CurrentUser.ProfileId : (Guid?)null;
    }

    #endregion

    #region Base overrides

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      MediaItemQueryExtension miqe = (MediaItemQueryExtension)source;
      NecessaryRequestedMIAs = miqe.NecessaryRequestedMIAs;
      OptionalRequestedMIAs = miqe.OptionalRequestedMIAs;
      Filter = miqe.Filter;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    #endregion

    #region GUI Properties

    public AbstractProperty NecessaryRequestedMIAsProperty
    {
      get { return _necessaryRequestedMIAsProperty; }
    }

    public IEnumerable<Guid> NecessaryRequestedMIAs
    {
      get { return (IEnumerable<Guid>)_necessaryRequestedMIAsProperty.GetValue(); }
      set { _necessaryRequestedMIAsProperty.SetValue(value); }
    }

    public AbstractProperty OptionalRequestedMIAsProperty
    {
      get { return _optionalRequestedMIAsProperty; }
    }

    public IEnumerable<Guid> OptionalRequestedMIAs
    {
      get { return (IEnumerable<Guid>)_optionalRequestedMIAsProperty.GetValue(); }
      set { _optionalRequestedMIAsProperty.SetValue(value); }
    }

    public AbstractProperty FilterProperty
    {
      get { return _filterProperty; }
    }

    public IFilter Filter
    {
      get { return (IFilter)_filterProperty.GetValue(); }
      set { _filterProperty.SetValue(value); }
    }

    #endregion
  }
}