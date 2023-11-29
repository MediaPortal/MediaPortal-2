#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using MediaPortal.UI.Presentation.Models;

namespace Webradio.Models
{
  public class WebRadioListModel : BaseContentListModel
  {
    #region Consts

    // Global ID definitions and references
    public const string WEBRADIO_LIST_MODEL_ID_STR = "55623F9E-60EF-4C28-B835-F8E44D9549E7";

    // ID variables
    public static readonly Guid WEBRADIO_LIST_MODEL_ID = new Guid(WEBRADIO_LIST_MODEL_ID_STR);

    #endregion

    public WebRadioListModel() : base("/Content/WebradioListProviders")
    {
    }
    protected override bool ServerConnectionRequired => false;
  }
}
