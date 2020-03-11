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

using MediaPortal.UI.Presentation.Models;
using System;

namespace MediaPortal.UiComponents.Media.Models
{
  public class MediaListModel : BaseContentListModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_LIST_MODEL_ID_STR = "6121E6CC-EB66-4ABC-8AA0-D952B64C0414";

    // ID variables
    public static readonly Guid MEDIA_LIST_MODEL_ID = new Guid(MEDIA_LIST_MODEL_ID_STR);

    #endregion

    public MediaListModel() : base("/Content/MediaListProviders")
    {
    }
  }
}
