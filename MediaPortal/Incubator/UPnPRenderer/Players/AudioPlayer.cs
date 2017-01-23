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

using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UPnPRenderer.Players
{
  public class UPnPRendererAudioPlayer : BaseDXPlayer, IAudioPlayer
  {
    private FilterFileWrapper _filterWrapper;
    public const string MIMETYPE = "upnpaudio/upnprenderer";
    public const string DUMMY_FILE = "UPnPRenderer://localhost/UPnPRendererAudio.upnp";

    protected override void AddSourceFilter()
    {
      PlayerHelpers.AddSourceFilterOverride(base.AddSourceFilter, _resourceAccessor, _graphBuilder, out _filterWrapper);
    }

    public override string Name
    {
      get { return "UPnPRenderer Audio Player"; }
    }

    public override void Dispose()
    {
      FilterGraphTools.TryDispose(ref _filterWrapper);
      base.Dispose();
    }
  }
}
