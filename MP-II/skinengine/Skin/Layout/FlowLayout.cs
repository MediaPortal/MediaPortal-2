#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using SkinEngine.Controls;
using SkinEngine.Properties;

namespace SkinEngine.Skin.Layout
{
  public class FlowLayout :  ILayout
  {
    private Group _group;

    public FlowLayout(Group group)
    {
      _group = group;
      Perform();
    }

    public void Perform()
    {

      float width = 0;
      for (int i = 0; i < _group.Controls.Count; ++i)
      {
        width += _group.Controls[i].Width;
      }
      int items = _group.Controls.Count;
      float offset = 0;
      for (int i = 0; i < _group.Controls.Count; ++i)
      {
        _group.Controls[i].Position = new Vector3(_group.OriginalPosition.X + offset,
                                                 _group.Controls[i].OriginalPosition.Y,
                                                 _group.Controls[i].OriginalPosition.Z);
        offset += (width / ((float)items));
      }
    }
  }
}
