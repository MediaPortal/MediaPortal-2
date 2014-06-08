#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MediaPortal.PackageServer.Domain.Entities.Helpers
{
  public static class Tags
  {
    #region Nested Classes

    public class TagBase : IEnumerable<string>
    {
      public IEnumerator<string> GetEnumerator()
      {
        return GetType().GetProperties().Select(property => (string)property.GetValue(null)).GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return GetEnumerator();
      }
    }

    public class ProductTags : TagBase
    {
      public string Client
      {
        get { return "client"; }
      }

      public string Server
      {
        get { return "server"; }
      }

      public string TVEngine
      {
        get { return "tv-engine"; }
      }
    }

    public class CategoryTags : TagBase
    {
      #region Skins

      [Description("")]
      public string Skins
      {
        get { return "skins"; }
      }

      [Description("")]
      public string WidescreenAspectRatio
      {
        get { return "skins-16:9"; }
      }

      [Description("")]
      public string StandardDefinitionAspectRatio
      {
        get { return "skins-4:3"; }
      }

      #endregion

      #region Plugins (i.e. not Skins)

      [Description("Plugins")]
      public string Plugins // functionality
      {
        get { return "plugins"; }
      }

      #endregion

      #region Video / Movies / Series / Television

      [Description("Movies, Series & Videos")]
      public string Video
      {
        get { return "video"; }
      }

      [Description("Television")]
      public string Television
      {
        get { return "tv"; }
      }

      #endregion

      #region Audio / Radio

      [Description("Music & Audio")]
      public string Audio
      {
        get { return "audio"; }
      }

      [Description("Radio")]
      public string Radio
      {
        get { return "radio"; }
      }

      #endregion

      #region Pictures

      [Description("Pictures")]
      public string Pictures
      {
        get { return "pictures"; }
      }

      #endregion

      #region I/O Device Support

      [Description("Input & Output")]
      public string InputOutput
      {
        get { return "input-output"; }
      }

      [Description("External Displays")]
      public string Displays
      {
        get { return "displays"; }
      }

      [Description("Remotes")]
      public string Remotes
      {
        get { return "remotes"; }
      }

      [Description("Other Input Devices")]
      public string OtherInputs
      {
        get { return "input-device"; }
      }

      #endregion

      #region Content Providers (News, Weather and Information)

      [Description("News, Weather & Info")]
      public string ContentProvider
      {
        get { return "content-provider"; }
      }

      [Description("News")]
      public string News
      {
        get { return "news"; }
      }

      [Description("Information")]
      public string Information
      {
        get { return "info"; }
      }

      [Description("Weather")]
      public string Weather
      {
        get { return "weather"; }
      }

      #endregion
    }

    public class SystemTags : TagBase
    {
      public string FanArt
      {
        get { return "fan-art"; }
      }

      public string ChannelLogos
      {
        get { return "logo"; }
      }

      public string Touch
      {
        get { return "touch-screen"; }
      }

      public string OnlineService
      {
        get { return "online-service"; }
      }

      public string RegistrationRequired
      {
        get { return "registration-required"; }
      }
    }

    #endregion

    #region Fields (Static)

    private static readonly ProductTags productTags = new ProductTags();
    private static readonly CategoryTags categoryTags = new CategoryTags();
    private static readonly SystemTags systemTags = new SystemTags();

    #endregion

    public static ProductTags Product
    {
      get { return productTags; }
    }

    public static CategoryTags Category
    {
      get { return categoryTags; }
    }

    public static SystemTags System
    {
      get { return systemTags; }
    }
  }
}