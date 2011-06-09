#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Settings;
using MediaPortal.UiComponents.BackgroundManager.Settings;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class LayoutManagerModel
  {
    #region Consts

    public const string LM_MODEL_ID_STR = "35C31826-4159-4CF9-B4F5-98DEBE04E93C";
    public static Guid LM_MODEL_ID = new Guid(LM_MODEL_ID_STR);

    #endregion

    private readonly AbstractProperty _thumbnailSizeMode;


    public LayoutManagerModel()
    {
      LayoutManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LayoutManagerSettings>();
      _thumbnailSizeMode = new WProperty(typeof(int), settings.SelectedLayoutIndex);
    }

    public Guid ModelId
    {
      get { return LM_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

    public AbstractProperty ThumbnailSizeModeProperty
    {
      get { return _thumbnailSizeMode; }
    }

    public int ThumbnailSizeMode
    {
      get { return (int) _thumbnailSizeMode.GetValue(); }
      set { _thumbnailSizeMode.SetValue(value); }
    }

    public void ChangeLayout()
    {
      if (ThumbnailSizeMode < 3)
        ThumbnailSizeMode++;
      else
        ThumbnailSizeMode = 1;

      LayoutManagerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<LayoutManagerSettings>();
      settings.SelectedLayoutIndex = ThumbnailSizeMode;
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
    }

    #endregion
  }
}
