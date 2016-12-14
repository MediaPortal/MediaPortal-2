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

using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// GenreAspectWrapper wraps the contents of <see cref="GenreAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class GenreAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _genreIdProperty;
protected AbstractProperty _genreProperty;
protected AbstractProperty _mediaItemProperty;
protected AbstractProperty _aspectIndexProperty;
protected AbstractProperty _aspectCountProperty;

#endregion

#region Properties

public AbstractProperty GenreIdProperty
{
  get{ return _genreIdProperty; }
}

public int? GenreId
{
  get { return (int?) _genreIdProperty.GetValue(); }
  set { _genreIdProperty.SetValue(value); }
}

public AbstractProperty GenreProperty
{
  get{ return _genreProperty; }
}

public string Genre
{
  get { return (string) _genreProperty.GetValue(); }
  set { _genreProperty.SetValue(value); }
}

public AbstractProperty MediaItemProperty
{
  get{ return _mediaItemProperty; }
}

public MediaItem MediaItem
{
  get { return (MediaItem) _mediaItemProperty.GetValue(); }
  set { _mediaItemProperty.SetValue(value); }
}

public AbstractProperty AspectIndexProperty
{
  get{ return _aspectIndexProperty; }
}

public int AspectIndex
{
  get { return (int) _aspectIndexProperty.GetValue(); }
  set { _aspectIndexProperty.SetValue(value); }
}

public AbstractProperty AspectCountProperty
{
  get{ return _aspectCountProperty; }
}

public int AspectCount
{
  get { return (int) _aspectCountProperty.GetValue(); }
  set { _aspectCountProperty.SetValue(value); }
}

#endregion

#region Constructor

public GenreAspectWrapper()
{
  _genreIdProperty = new SProperty(typeof(int?));
  _genreProperty = new SProperty(typeof(string));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
  _aspectIndexProperty = new SProperty(typeof(int));
  _aspectIndexProperty.Attach(AspectIndexChanged);
  _aspectCountProperty = new SProperty(typeof(int));
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

private void AspectIndexChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
{
  Detach();
  base.DeepCopy(source, copyManager);
  var aw = (GenreAspectWrapper)source;
  AspectIndex = aw.AspectIndex;
  Attach();
}

private void Attach()
{
  _aspectIndexProperty.Attach(AspectIndexChanged);
}

private void Detach()
{
  _aspectIndexProperty.Detach(AspectIndexChanged);
}

public void Init(MediaItem mediaItem)
{
  IList<MultipleMediaItemAspect> aspects;
  if (mediaItem == null || !MediaItemAspect.TryGetAspects(mediaItem.Aspects, GenreAspect.Metadata, out aspects) ||
      AspectIndex < 0 || AspectIndex >= aspects.Count)
  {
     SetEmpty();
     return;
  }

  AspectCount = aspects.Count;
  GenreId = (int?) aspects[AspectIndex][GenreAspect.ATTR_ID];
  Genre = (string) aspects[AspectIndex][GenreAspect.ATTR_GENRE];
}

public void SetEmpty()
{
  AspectCount = 0;
  GenreId = null;
  Genre = null;
}

#endregion

}

}
