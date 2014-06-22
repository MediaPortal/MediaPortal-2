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

using System.Linq;
using System.Web.Mvc;
using MediaPortal.PackageServer.Utility.Hooks;
using Newtonsoft.Json;
using MediaPortal.PackageServer.Initialization.Core;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MediaPortal.PackageServer.Initialization
{
  public class SerializerConfig : IConfigurationTask
  {
    public void Configure()
    {
      // configure Json.Net serialization
      JsonConvert.DefaultSettings = () => new JsonSerializerSettings
      {
          DateTimeZoneHandling = DateTimeZoneHandling.Utc,
          ContractResolver = new CamelCasePropertyNamesContractResolver(),
          Converters = new JsonConverter[] { new IsoDateTimeConverter(), new StringEnumConverter() }
      };

      // register Json.Net as value factory (so we use also when receiving json)
      var factories = ValueProviderFactories.Factories;
      factories.Remove(factories.OfType<JsonValueProviderFactory>().FirstOrDefault());
      factories.Add(new JsonNetValueProviderFactory());
    }
  }
}
