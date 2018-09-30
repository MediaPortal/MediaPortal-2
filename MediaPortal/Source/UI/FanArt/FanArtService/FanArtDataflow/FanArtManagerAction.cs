#region Copyright (C) 2007-2018 Team MediaPortal

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

using MediaPortal.Common.MediaManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaPortal.Extensions.UserServices.FanArtService.FanArtDataflow
{
  public enum ActionType
  {
    Collect,
    Delete
  }

  public class FanArtManagerAction
  {
    public FanArtManagerAction(ActionType actionType, Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      ActionId = Guid.NewGuid();
      Type = actionType;
      MediaItemId = mediaItemId;
      Aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      foreach (var aspect in aspects) //Remove dependency on the media item list
        Aspects.Add(aspect.Key, aspect.Value.ToList());
    }

    public Guid ActionId { get; set; }

    public ActionType Type { get; set; }

    public Guid MediaItemId { get; set; }

    [XmlIgnore]
    public IDictionary<Guid, IList<MediaItemAspect>> Aspects { get; set; }

    #region Additional members for the XML serialization

    internal FanArtManagerAction() { }

    #endregion
  }
}
