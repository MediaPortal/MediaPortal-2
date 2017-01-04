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

using DirectShow.Helper;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video;

namespace MediaPortal.Plugins.SlimTv.Client.Player
{
  public class WTVPlayer : VideoPlayer
  {
    public WTVPlayer()
    {
      PlayerTitle = "WTVPlayer";
    }

    protected override void AddSubtitleFilter(bool isSourceFilterPresent)
    {
      // Avoid duplicate handling in later step
      if (isSourceFilterPresent)
        return;

      base.AddSubtitleFilter(false);
    }

    protected override void AddSourceFilter()
    {
      ServiceRegistration.Get<ILogger>().Debug("{0}: Initializing for WTV media item '{1}'", PlayerTitle, SourcePathOrUrl);

      // We call the subfilter handler earlier then the regular step
      AddSubtitleFilter(false);

      // try to render the url and let DirectShow choose the source filter
      int hr = _graphBuilder.RenderFile(SourcePathOrUrl, null);
      new HRESULT(hr).Throw();
    }
  }
}
