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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents a writer filter choice.
  /// </summary>
  public class WriterFilterItem : FilterItem
  {
    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);

      PersonInfo person = new PersonInfo();
      if (!person.FromMetadata(mediaItem.Aspects))
        return;

      Name = person.Name ?? "";
      Description = person.Biography.Text ?? "";

      FireChange();
    }

    public string Name
    {
      get { return this[Consts.KEY_NAME]; }
      set { SetLabel(Consts.KEY_NAME, value); }
    }

    public string Description
    {
      get { return this[Consts.KEY_DESCRIPTION]; }
      set { SetLabel(Consts.KEY_DESCRIPTION, value); }
    }
  }
}
