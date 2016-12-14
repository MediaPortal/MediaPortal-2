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

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// CharacterAspectWrapper wraps the contents of <see cref="CharacterAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class CharacterAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _characterNameProperty;
protected AbstractProperty _actorNameProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty CharacterNameProperty
{
  get{ return _characterNameProperty; }
}

public string CharacterName
{
  get { return (string) _characterNameProperty.GetValue(); }
  set { _characterNameProperty.SetValue(value); }
}

public AbstractProperty ActorNameProperty
{
  get{ return _actorNameProperty; }
}

public string ActorName
{
  get { return (string) _actorNameProperty.GetValue(); }
  set { _actorNameProperty.SetValue(value); }
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

#endregion

#region Constructor

public CharacterAspectWrapper()
{
  _characterNameProperty = new SProperty(typeof(string));
  _actorNameProperty = new SProperty(typeof(string));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public void Init(MediaItem mediaItem)
{
  SingleMediaItemAspect aspect;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, CharacterAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  CharacterName = (string) aspect[CharacterAspect.ATTR_CHARACTER_NAME];
  ActorName = (string) aspect[CharacterAspect.ATTR_ACTOR_NAME];
}

public void SetEmpty()
{
  CharacterName = null;
  ActorName = null;
}

#endregion

}

}
