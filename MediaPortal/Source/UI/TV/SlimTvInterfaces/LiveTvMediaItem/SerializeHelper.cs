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

using System.Runtime.Serialization;
using System.Xml;

namespace MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem
{
  public static class SerializeHelper
  {
    public static void SerializeXml<TE>(this TE objectToSerialize, XmlWriter writer)
    {
      DataContractSerializer dsc = new DataContractSerializer(typeof(TE));
      dsc.WriteObject(writer, objectToSerialize);
    }
    public static TE DeserializeXml<TE>(this XmlReader reader)
    {
      DataContractSerializer dsc = new DataContractSerializer(typeof(TE));
      return (TE) dsc.ReadObject(reader);
    }
  }
}
