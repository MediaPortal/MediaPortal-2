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
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System;
using System.Collections.Generic;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaInfoModel : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "E71C7104-4398-41BF-A019-5C92904DE7E7";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);
    public static Guid WF_MEDIA_INFO_AUDIO = new Guid("FB8BA593-8818-47F1-82A2-F4F3AE9D6932");
    public static Guid WF_MEDIA_INFO_VIDEO = new Guid("1FB7B965-0104-497B-A9BD-15C5E2B8AA14");
    public static Guid WF_MEDIA_INFO_IMAGES = new Guid("3947E401-F744-4CA9-8522-A5C4E85CA2D2");

    #endregion

    protected AbstractProperty _mediaItemProperty;

    public MediaInfoModel()
    {
      _mediaItemProperty = new WProperty(typeof(MediaItem));
    }

    protected NavigationData GetCurrentNavigationData()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return MediaNavigationModel.GetNavigationData(workflowManager.CurrentNavigationContext, false);
    }

    #region Members to be accessed from the GUI

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    public void Play()
    {
      if (MediaItem != null)
        PlayItemsModel.CheckQueryPlayAction(MediaItem);
    }

    #endregion

    public Guid ModelId  => MODEL_ID; 

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.ContextVariables.TryGetValue(Consts.KEY_MEDIA_ITEM, out var mediaItem))
        MediaItem = (MediaItem)mediaItem;
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }
  }
}
