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

using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Players;

namespace MediaPortal.UI.SkinEngine.Utils
{
  public static class UIResourcesHelper
  {
    public static void ReleaseUIResources()
    {
      PlayersHelper.ReleaseGUIResources();

      Controls.Brushes.BrushCache.Instance.Clear();
      // Albert, 2011-03-25: I think that actually, ContentManager.Free() should be called here. Clear() makes the ContentManager
      // forget all its cached assets and so we must make sure that no more asset references are in the system. That's why we also
      // need to clear the brush cache.
      ContentManager.Instance.Clear();
    }

    public static void ReallocUIResources()
    {
      PlayersHelper.ReallocGUIResources();
    }
  }
}