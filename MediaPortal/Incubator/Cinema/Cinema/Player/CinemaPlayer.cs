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
using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace Cinema.Player
{
  public class CinemaPlayer : VideoPlayer, IUIContributorPlayer
  {
    public const string CINEMA_MIMETYPE = "cinema/stream";
    private const string SOURCE_FILTER_NAME = "LAV Splitter Source";

    protected void AddFileSource()
    {
      IBaseFilter sourceFilter = null;
      try
      {
        sourceFilter = FilterGraphTools.AddFilterByName(_graphBuilder, FilterCategory.LegacyAmFilterCategory, SOURCE_FILTER_NAME);
        FilterGraphTools.RenderOutputPins(_graphBuilder, sourceFilter);
      }
      finally
      {
        FilterGraphTools.TryRelease(ref sourceFilter);
      }
    }

    protected override void AddSourceFilter()
    {
      base.AddSourceFilter();
      // HD streams tend to come with a separate audio stream, check if that's the case and also add the audio stream to the graph
      if(_mediaItem is CinemaMediaItem cinemaMediaItem && cinemaMediaItem.AudioUrl != null)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing separate audio stream for network media item '{1}'", PlayerTitle, cinemaMediaItem.AudioUrl);

        // try to render the url and let DirectShow choose the source filter
        int hr = _graphBuilder.RenderFile(cinemaMediaItem.AudioUrl, null);
        new HRESULT(hr).Throw();
      }

    }

    public Type UIContributorType
    {
      get { return typeof(CinemaUiContributor); }
    }
  }
}
