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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Messaging;
using MediaPortal.Extensions.OnlineLibraries;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Provides a workflow model for selecting matching media items.
  /// </summary>
  public class MediaItemMatchModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_MIMATCH = "692FA8C3-41A5-43DD-8C12-C857C9C75E72";
    public static readonly Guid MODEL_ID_MIMATCH = new Guid(STR_MODEL_ID_MIMATCH);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected ItemsList _matchList = null;

    protected AbstractProperty _isSearchingProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public MediaItemMatchModel()
    {
      _isSearchingProperty = new WProperty(typeof(bool), false);

      _matchList = new ItemsList();
    }

    public void Dispose()
    {
      _matchList = null;
  }

    #endregion

    #region Public properties (Also accessed from the GUI)

    public ItemsList OnlineMatchList
    {
      get
      {
        lock (_syncObj)
          return _matchList;
      }
    }

    public AbstractProperty IsSearchingProperty
    {
      get { return _isSearchingProperty; }
    }

    public bool IsSearching
    {
      get { return (bool)_isSearchingProperty.GetValue(); }
      set { _isSearchingProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public async Task OpenSelectMatchDialogAsync(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IsSearching = true;
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectMatch",
        (string name, System.Guid id) =>
        {
          ListItem matchItem = _matchList.FirstOrDefault(i => i.Selected);

        });

      if (aspects.ContainsKey(MovieAspect.ASPECT_ID))
      {
        MovieInfo info = new MovieInfo();
        info.FromMetadata(aspects);
        var matches = await OnlineMatcherService.Instance.FindMatchingMoviesAsync(info);
        IsSearching = false;
      }
    }

    public Task<IEnumerable<MediaItemAspect>> WaitForMatchSelectionAsync()
    {
      return Task.FromResult((IEnumerable<MediaItemAspect>)new List<MediaItemAspect>());
    }

    #endregion

    #region Private and protected methods

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _matchList = new ItemsList();
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_MIMATCH; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ClearData();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {

    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
