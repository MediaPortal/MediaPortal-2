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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UI.Services.UserManagement;

namespace MediaPortal.UiComponents.BlueVision.Models
{
  public class LatestMediaModel : BaseTimerControlledModel
  {
    #region Consts

    // Global ID definitions and references
    public const string LATEST_MEDIA_MODEL_ID_STR = "19FBB179-51FB-4DB6-B19C-D5C765E9B870";

    // ID variables
    public static readonly Guid LATEST_MEDIA_MODEL_ID = new Guid(LATEST_MEDIA_MODEL_ID_STR);

    public static Guid[] NECESSARY_RECORDING_MIAS =
    {
      ProviderResourceAspect.ASPECT_ID,
      MediaAspect.ASPECT_ID,
      VideoAspect.ASPECT_ID,
      new Guid("8DB70262-0DCE-4C80-AD03-FB1CDF7E1913") /* RecordingAspect.ASPECT_ID*/
    };

    private readonly AbstractProperty _queryLimitProperty;
    private bool _updatePending = false;

    #endregion

    public const int DEFAULT_QUERY_LIMIT = 5;
    public const int AUTO_UPDATE_INTERVAL = 30000;

    public delegate PlayableMediaItem MediaItemToListItemAction(MediaItem mediaItem);

    public AbstractProperty QueryLimitProperty { get { return _queryLimitProperty; } }

    public int QueryLimit
    {
      get { return (int)_queryLimitProperty.GetValue(); }
      set { _queryLimitProperty.SetValue(value); }
    }

    public ItemsList Videos { get; private set; }
    public ItemsList Series { get; private set; }
    public ItemsList Movies { get; private set; }
    public ItemsList Audio { get; private set; }
    public ItemsList Images { get; private set; }
    public ItemsList Recordings { get; private set; }

    public LatestMediaModel()
      : base(true, 500)
    {
      _queryLimitProperty = new WProperty(typeof(int), DEFAULT_QUERY_LIMIT);

      Videos = new ItemsList();
      Series = new ItemsList();
      Movies = new ItemsList();
      Audio = new ItemsList();
      Images = new ItemsList();
      Recordings = new ItemsList();
    }

    protected IEnumerable<ItemsList> AllItems
    {
      get { return new[] { Videos, Series, Movies, Audio, Images, Recordings }; }
    }

    protected void ClearAll()
    {
      foreach (ItemsList itemsList in AllItems)
      {
        itemsList.Clear();
        itemsList.FireChange();
      }
    }

    protected override void Update()
    {
      if (_updatePending)
        UpdateItems();
    }

    public bool UpdateItems()
    {
      try
      {
        ClearAll();
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
        {
          _updatePending = true;
          return false;
        }
        _updatePending = false;

        SetLayout();

        FillList_Async(contentDirectory, Media.General.Consts.NECESSARY_MOVIES_MIAS, Movies, item => new MovieItem(item));
        FillList_Async(contentDirectory, Media.General.Consts.NECESSARY_EPISODE_MIAS, Series, item => new EpisodeItem(item));
        FillList_Async(contentDirectory, Media.General.Consts.NECESSARY_IMAGE_MIAS, Images, item => new ImageItem(item));
        FillList_Async(contentDirectory, Media.General.Consts.NECESSARY_VIDEO_MIAS, Videos, item => new VideoItem(item));
        FillList_Async(contentDirectory, Media.General.Consts.NECESSARY_AUDIO_MIAS, Audio, item => new AudioItem(item));
        FillList_Async(contentDirectory, NECESSARY_RECORDING_MIAS, Recordings, item => new VideoItem(item));
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Latest Media", ex);
        return false;
      }
    }

    protected void FillList_Async(IContentDirectory contentDirectory, Guid[] necessaryMIAs, ItemsList list, MediaItemToListItemAction converterAction)
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(() => FillList(contentDirectory, necessaryMIAs, list, converterAction));
    }

    protected void FillList(IContentDirectory contentDirectory, Guid[] necessaryMIAs, ItemsList list, MediaItemToListItemAction converterAction)
    {
      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, null)
      {
        Limit = (uint)QueryLimit, // Last 5 imported items
        SortInformation = new List<SortInformation> { new SortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
      };

      Guid? userProfile = null;
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement != null && userProfileDataManagement.IsValidUser)
        userProfile = userProfileDataManagement.CurrentUser.ProfileId;

      var items = contentDirectory.Search(query, false, userProfile, false);
      list.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        list.Add(listItem);
      }
      list.FireChange();
    }

    protected void SetLayout()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      ViewModeModel vwm = workflowManager.GetModel(ViewModeModel.VM_MODEL_ID) as ViewModeModel;
      if (vwm != null)
      {
        vwm.LayoutType = LayoutType.GridLayout;
        vwm.LayoutSize = LayoutSize.Medium;
      }
    }
  }
}
