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

using System;
using System.Xml;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Plugins.SlimTv.Interfaces.UPnP.DataTypes
{
  public class UPnPDtScheduleRule : UPnPExtendedDataType
  {
    public static UPnPDtScheduleRule Instance = new UPnPDtScheduleRule();

    public const string DATATYPE_NAME = "UPnPDtScheduleRule";

    public UPnPDtScheduleRule()
      : base(DataTypesConfiguration.DATATYPES_SCHEMA_URI, DATATYPE_NAME)
    {
    }

    public override bool SupportsStringEquivalent
    {
      get { return false; }
    }

    public override bool IsNullable
    {
      get { return true; }
    }

    public override bool IsAssignableFrom(Type type)
    {
      return typeof(ScheduleRule).IsAssignableFrom(type);
    }

    protected override void DoSerializeValue(object value, bool forceSimpleValue, XmlWriter writer)
    {
      ScheduleRule rule = (ScheduleRule)value;
      rule.Serialize(writer);
    }

    protected override object DoDeserializeValue(XmlReader reader, bool isSimpleValue)
    {
      if (SoapHelper.ReadEmptyStartElement(reader)) // Read start of enclosing element
        return null;

      ScheduleRule result = null;
      while (reader.NodeType != XmlNodeType.EndElement)
        result = ScheduleRule.Deserialize(reader);
      reader.ReadEndElement(); // End of enclosing element
      return result;
    }    
  }
}
