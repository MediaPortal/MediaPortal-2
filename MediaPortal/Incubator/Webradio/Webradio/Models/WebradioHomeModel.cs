#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.UserServices.FanArtService.Client.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using Webradio.Helper;
using Webradio.Player;
using Webradio.Settings;

namespace Webradio.Models
{
  public class WebradioHomeModel : IWorkflowModel, IPluginStateTracker
  {
    public static ItemsList AllRadioStreams = new ItemsList();

    public void Init()
    {
      ClearFanart();

      Radiostations.Reset();

      if (Filters.Instance.ActiveFilter != null)
      {
        WebradioDataModel.ActiveFilter = Filters.Instance.ActiveFilter.Titel;
        var mst = Radiostations.Filtered(Filters.Instance.ActiveFilter, Radiostations.Instance.Stations);
        if (mst == null)
        {
          mst = Radiostations.Instance.Stations;
        }
        else if (mst.Count == 0)
        {
          mst = Radiostations.Instance.Stations;
        }

        FillItemList(mst);
      }
      else
      {
        FillItemList(Radiostations.Instance.Stations);
      }
    }

    /// <summary>
    /// Fill the List and set the Labels
    /// </summary>
    public static void FillItemList(List<RadioStation> list)
    {
      AllRadioStreams.Clear();
      foreach (var ms in list)
      {
        SetFallbackValues(ms);
        var item = CreateStreamListItem(ms);
        AllRadioStreams.Add(item);
      }
      WebradioDataModel.StreamListCount = AllRadioStreams.Count;

      ListItem li = AllRadioStreams.First();
      li.Selected = true;
      AllRadioStreams.FireChange();
    }

    public static ListItem CreateStreamListItem(RadioStation rs)
    {
      var item = new ListItem();
      SetListItemProperties(rs, item);
      return item;
    }

    public static void SetListItemProperties(RadioStation rs, ListItem item)
    {
      item.AdditionalProperties[STREAM_ID] = rs.Id;
      item.SetLabel("Name", rs.Name);
      item.SetLabel("Genres", string.Join(", ", rs.Genres));
      item.SetLabel("Country", "[Country." + rs.Country + "]");
      item.SetLabel("CountryCode", rs.Country);
      item.SetLabel("City", rs.City);
      item.SetLabel("Language", "[Language." + rs.Language + "]");
      item.SetLabel("ImageSrc", SetStreamLogo(rs));
      item.SetLabel("IsFavorite", Favorites.IsFavorite(rs).ToString());
    }

    public static void UpdateFavorits(string streamId, bool isFavorite)
    {
      foreach (ListItem item in AllRadioStreams)
      {
        if ((string)item.AdditionalProperties[STREAM_ID] == streamId)
        {
          item.SetLabel("IsFavorite", isFavorite.ToString());
          item.FireChange();
          break;
        }
      }
    }

    /// <summary>
    /// Set the Logo of a Stream or use the DefaultLogo
    /// </summary>
    public static string SetStreamLogo(RadioStation rs)
    {
      var s = "DefaultLogo.png";
      if (rs.Logo != "") s = rs.Logo;
      return s;
    }

    /// <summary>
    /// Set the FallbackValues of the current Stream
    /// </summary>
    public static void SetFallbackValues(RadioStation rs)
    {
      if (rs.Country == "") rs.Country = "unknown";
      if (rs.City == "") rs.City = "unknown";
    }

    private void ClearFanart()
    {
      var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
      if (fanArtBgModel != null) fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
    }

    #region Used by Skin

    /// <summary>
    /// Reset the List and show all Streams
    /// </summary>
    public void ShowAllStreams()
    {
      Filters.Instance.ClearActiveFilter();
      FillItemList(Radiostations.Instance.Stations);
    }

    public void ShowFavotites()
    {
      var favoritList = ServiceRegistration.Get<ISettingsManager>().Load<Favorites>().List ?? new List<string>();
      if (favoritList.Count > 0)
      {
        List<RadioStation> list = new List<RadioStation>();
        foreach (var st in favoritList)
        {
          list.Add(Radiostations.ById(st));
        }
        Filters.Instance.ClearActiveFilter();
        FillItemList(list);
      }
    }

    /// <summary>
    /// Get the selected Stream
    /// </summary>
    public void SelectStream(ListItem item)
    {
      WebRadioPlayerHelper.PlayStream(Radiostations.ById((string)item.AdditionalProperties[STREAM_ID]));
    }

    public void SetFavoritStatus(ListItem item)
    {
      WebradioDataModel.SelectedStream = Radiostations.ById((string)item.AdditionalProperties[STREAM_ID]);
      ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E3ED86D5-D54C-44F4-A4AA-ABEBBC3574F8"));
    }

    #endregion

    #region Consts

    protected const string MODEL_ID_STR = "EA3CC191-0BE5-4C8D-889F-E9C4616AB554";
    protected const string STREAM_ID = "Id";

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId => new Guid(MODEL_ID_STR);

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (Radiostations.Instance.Stations == null)
      {
        WebradioDataModel.DialogMessage = "[Webradio.Dialog.Search.NoStreams]";
        ServiceRegistration.Get<IWorkflowManager>().NavigatePushAsync(new Guid("E0C1F78A-D32F-44BC-9678-EDCD0710FF75"));
        return false;
      }

      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Init();
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
      ClearFanart();
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region PluginStateTracker

    public async void Activated(PluginRuntime pluginRuntime)
    {
      var ret = await Task.Run(Radiostations.NeedUpdate);
      if (ret)
      {
        await Task.Run(Radiostations.MakeUpdate);
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    #endregion
  }
}
