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
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.UI.Presentation.Players.ResumeState
{
  /// <summary>
  /// Base class for all resume states.
  /// </summary>
  [KnownType(typeof(PositionResumeState))]
  [KnownType(typeof(BinaryResumeState))]
  public abstract class ResumeStateBase : IXmlSerializable, IResumeState
  {
    #region Static methods

    public static string Serialize(IResumeState resumeState)
    {
      if (resumeState == null)
        return null;
      DataContractSerializer serializer = new DataContractSerializer(typeof(ResumeStateBase));
      StringBuilder serialized = new StringBuilder();
      using (XmlWriter writer = XmlWriter.Create(serialized))
        serializer.WriteObject(writer, resumeState);
      return serialized.ToString();
    }

    public static ResumeStateBase Deserialize(string serialized)
    {
      if (string.IsNullOrEmpty(serialized))
        return null;
      DataContractSerializer serializer = new DataContractSerializer(typeof(ResumeStateBase));
      using (StringReader reader = new StringReader(serialized))
      using (XmlReader xmlReader = XmlReader.Create(reader))
        return serializer.ReadObject(xmlReader) as ResumeStateBase;
    }

    #endregion

    #region Abstract members

    public abstract void InitFromString(string value);

    #endregion

    #region Implementation of IXmlSerializable

    public XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(XmlReader reader)
    {
      if (!reader.MoveToAttribute("State"))
        throw new ArgumentException("State attribute not present");

      InitFromString(reader.Value);
    }

    public void WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("State", ToString());
    }

    #endregion
  }
}