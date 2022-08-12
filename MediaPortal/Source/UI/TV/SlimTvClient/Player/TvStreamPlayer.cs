#region Copyright (C) 2007-2021 Team MediaPortal

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

using DirectShow;
using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using System;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  /// <summary>
  /// Base player for tv players that play live/recorded streams from a url.
  /// </summary>
  public class TvStreamPlayer : VideoPlayer
  {
    public TvStreamPlayer()
    {
      PlayerTitle = "TvStreamPlayer"; // for logging
    }

    protected override void AddSourceFilter()
    {
      Guid CLSID_LAV_SPLITTER = new Guid("{B98D13E7-55DB-4385-A33D-09FD1BA26338}");
      var networkResourceAccessor = _resourceAccessor as INetworkResourceAccessor;
      if (networkResourceAccessor != null)
      {
        ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for network media item '{1}'", PlayerTitle, networkResourceAccessor.URL);

        var sourceFilter = FilterGraphTools.AddFilterFromClsid(_graphBuilder, CLSID_LAV_SPLITTER, "LAV Splitter Source");
        if (sourceFilter != null)
        {
          var fileSourceFilter = ((IFileSourceFilter)sourceFilter);
          var hr = (HRESULT)fileSourceFilter.Load(networkResourceAccessor.URL, null);

          new HRESULT(hr).Throw();

          using (DSFilter source2 = new DSFilter(sourceFilter))
            foreach (DSPin pin in source2.Output)
              hr = pin.Render(); // Some pins might fail rendering (i.e. subtitles), but the graph can be still playable
        }
        return;
      }
      base.AddSourceFilter();
    }
  }
}
