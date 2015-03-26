#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.Navigation;

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
      new Guid("C389F655-ED60-4271-91EA-EC589BD815C6") /* RecordingAspect.ASPECT_ID*/
    };

    private readonly AbstractProperty _queryLimitProperty;

    #endregion

    public const int DEFAULT_QUERY_LIMIT = 5;

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
      try
      {
        ClearAll();
        var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
        if (contentDirectory == null)
          return;

        // Once MediaLibrary is connected, we reduce the update interval to 10 seconds
        if (_updateInterval != 10000)
          ChangeInterval(10000);

        FillList(contentDirectory, Media.General.Consts.NECESSARY_MOVIES_MIAS, Movies, item => new MovieItem(item));
        FillList(contentDirectory, Media.General.Consts.NECESSARY_SERIES_MIAS, Series, item => new SeriesItem(item));
        FillList(contentDirectory, Media.General.Consts.NECESSARY_IMAGE_MIAS, Images, item => new ImageItem(item));
        FillList(contentDirectory, Media.General.Consts.NECESSARY_VIDEO_MIAS, Audio, item => new AudioItem(item));
        FillList(contentDirectory, Media.General.Consts.NECESSARY_AUDIO_MIAS, Videos, item => new AudioItem(item));
        FillList(contentDirectory, NECESSARY_RECORDING_MIAS, Videos, item => new AudioItem(item));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error updating Latest Media", ex);
      }
    }

    protected void FillList(IContentDirectory contentDirectory, Guid[] necessaryMIAs, ItemsList list, MediaItemToListItemAction converterAction)
    {
      MediaItemQuery query = new MediaItemQuery(necessaryMIAs, null)
      {
        Limit = (uint)QueryLimit, // Last 5 imported items
        SortInformation = new List<SortInformation> { new SortInformation(ImporterAspect.ATTR_DATEADDED, SortDirection.Descending) }
      };

      var items = contentDirectory.Search(query, false);
      list.Clear();
      foreach (MediaItem mediaItem in items)
      {
        PlayableMediaItem listItem = converterAction(mediaItem);
        listItem.Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(listItem.MediaItem));
        list.Add(listItem);
      }
      list.FireChange();
    }
  }
}
