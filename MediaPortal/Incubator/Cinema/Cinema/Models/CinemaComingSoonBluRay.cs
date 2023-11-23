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
using Cinema.Player;
using Cinema.Settings;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Extensions.UserServices.FanArtService.Client.Models;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;

namespace Cinema.Models
{
    public class CinemaComingSoonBluRay : IWorkflowModel
    {
        public static ItemsList Items = new ItemsList();

        #region Consts

        public const string MODEL_ID_STR = "50AD907D-32F2-4633-B017-E9BFC6B6C960";
        public const string NAME = "name";
        public const string TRAILER = "trailer";

        #endregion

        #region Propertys

        private static readonly AbstractProperty _infoProperty = new WProperty(typeof(string), string.Empty);

        public AbstractProperty InfoProperty => _infoProperty;

        public static string Info
        {
            get { return (string)_infoProperty.GetValue(); }
            set { _infoProperty.SetValue(value); }
        }

        #endregion

        #region public Methods

        public static void SelectMovie(ListItem item)
        {
            var t = new Trailer { Title = (string)item.AdditionalProperties[NAME], Url = (string)item.AdditionalProperties[TRAILER] };
            if (t.Url != "")
            {
                CinemaPlayerHelper.PlayStream(t);
            }
        }

        public void SetSelectedItem(ListItem selectedItem)
        {
            if (selectedItem != null)
            {
                var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
                if (fanArtBgModel != null)
                {
                    var uriSource = selectedItem.Labels["Picture"].ToString();

                    if (uriSource != "") fanArtBgModel.ImageSource = new MultiImageSource { UriSource = uriSource };
                    else fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
                }
            }
        }

        #endregion

        #region private Methods

        private void Init()
        {
            //MovieInfo.Movies = new List<SortedMovie>();
            //MovieInfo.Movies = Search.CommingSoon(Functions.DefaultCountry(), Search.Feed.BluRay, Search.Typ.coming, 20);
            //FillItems();
        }

        private static void FillItems()
        {
            //Items.Clear();
            //foreach (var movie in MovieInfo.Movies)
            //{
            //    var item = new ListItem { AdditionalProperties = { [NAME] = movie.Title } };
            //    item.SetLabel("Name", movie.Title);
            //    item.SetLabel("Poster", movie.Cover);
            //    item.SetLabel("Picture", movie.Picture);
            //    item.SetLabel("Description", movie.Description);
            //    item.SetLabel("Year", movie.Year);
            //    item.SetLabel("AgeLimit", movie.AgeLimit);
            //    item.SetLabel("Genre", movie.Genre);
            //    item.AdditionalProperties[TRAILER] = movie.Trailer;
            //    item.SetLabel("Duration", movie.Duration);
            //    item.SetLabel("Premiere", movie.Premiere);
            //    Items.Add(item);
            //}
            //Items.FireChange();
        }

        private void ClearFanart()
        {
            var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
            if (fanArtBgModel != null)
            {
                fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
            }
        }

        #endregion

        #region IWorkflowModel implementation

        public Guid ModelId
        {
            get { return new Guid(MODEL_ID_STR); }
        }

        public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
        {
            return true;
        }

        public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            Init();
        }

        public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
        {
            ClearFanart();
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

        #endregion
    }
}
