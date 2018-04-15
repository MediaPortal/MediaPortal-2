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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace MediaPortal.Utilities.Xml
{
  /// <summary>
  /// Wraps a <see cref="XmlReader"/> and makes it possible to derive from this class
  /// wihout implementing all abstract members of <see cref="XmlReader"/>
  /// </summary>
  public class XmlWrappingReader : XmlReader, IXmlLineInfo
  {
    #region Private fields

    private readonly XmlReader _reader;
    private bool _disposed = false;

    #endregion

    #region Ctor

    public XmlWrappingReader(XmlReader baseReader)
    {
      if (baseReader == null)
        throw new ArgumentNullException("baseReader");
      _reader = baseReader;
    }

    #endregion

    #region Base overrides

    #region Passthrough properties

    public override XmlReaderSettings Settings { get { return _reader.Settings; } }
    public override XmlNodeType NodeType { get { return _reader.NodeType; } }
    public override string Name { get { return _reader.Name; } }
    public override string LocalName { get { return _reader.LocalName; } }
    public override string NamespaceURI { get { return _reader.NamespaceURI; } }
    public override string Prefix { get { return _reader.Prefix; } }
    public override bool HasValue { get { return _reader.HasValue; } }
    public override string Value { get { return _reader.Value; } }
    public override int Depth { get { return _reader.Depth; } }
    public override string BaseURI { get { return _reader.BaseURI; } }
    public override bool IsEmptyElement { get { return _reader.IsEmptyElement; } }
    public override bool IsDefault { get { return _reader.IsDefault; } }
    public override XmlSpace XmlSpace { get { return _reader.XmlSpace; } }
    public override string XmlLang { get { return _reader.XmlLang; } }
    public override Type ValueType { get { return _reader.ValueType; } }
    public override int AttributeCount { get { return _reader.AttributeCount; } }
    public override bool EOF { get { return _reader.EOF; } }
    public override ReadState ReadState { get { return _reader.ReadState; } }
    public override bool HasAttributes { get { return _reader.HasAttributes; } }
    public override XmlNameTable NameTable { get { return _reader.NameTable; } }
    public override bool CanResolveEntity { get { return _reader.CanResolveEntity; } }
    public override IXmlSchemaInfo SchemaInfo { get { return _reader.SchemaInfo; } }
    public override char QuoteChar { get { return _reader.QuoteChar; } }

    #endregion

    #region Passthrough methods

    public override string GetAttribute(string name) { return _reader.GetAttribute(name); }
    public override string GetAttribute(string name, string namespaceUri) { return _reader.GetAttribute(name, namespaceUri); }
    public override string GetAttribute(int i) { return _reader.GetAttribute(i); }
    public override bool MoveToAttribute(string name) { return _reader.MoveToAttribute(name); }
    public override bool MoveToAttribute(string name, string ns) { return _reader.MoveToAttribute(name, ns); }
    public override void MoveToAttribute(int i) { _reader.MoveToAttribute(i); }
    public override bool MoveToFirstAttribute() { return _reader.MoveToFirstAttribute(); }
    public override bool MoveToNextAttribute() { return _reader.MoveToNextAttribute(); }
    public override bool MoveToElement() { return _reader.MoveToElement(); }
    public override bool  Read() { return _reader.Read(); }
    public override void Close() { _reader.Close(); }
    public override void Skip() { _reader.Skip(); }
    public override string LookupNamespace(string prefix) { return _reader.LookupNamespace( prefix ); }
    public override void ResolveEntity() { _reader.ResolveEntity(); }
    public override bool ReadAttributeValue() { return _reader.ReadAttributeValue(); }
    public override Task<string> GetValueAsync() { return _reader.GetValueAsync(); }
    public override Task<bool> ReadAsync() { return _reader.ReadAsync(); }
    public override Task SkipAsync() { return _reader.SkipAsync(); }

    #endregion

    #endregion

    #region IXmlLineInfo implementation

    public virtual bool HasLineInfo() { return (_reader is IXmlLineInfo) && ((IXmlLineInfo)_reader).HasLineInfo(); }
    public virtual int LineNumber { get { return (_reader is IXmlLineInfo) ? ((IXmlLineInfo)_reader).LineNumber : 0; } }
    public virtual int LinePosition { get { return (_reader is IXmlLineInfo) ? ((IXmlLineInfo)_reader).LinePosition : 0; } }

    #endregion

    #region IDisposable implementation

    protected override void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      _reader.Dispose();
      _disposed = true;
      base.Dispose(disposing);
    }

    #endregion
  }
}
