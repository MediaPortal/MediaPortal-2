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
using System.Threading.Tasks;
using System.Xml;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Utilities
{
  /// <summary>
  /// <see cref="XmlReader"/> specifically designed to read nfo-files
  /// </summary>
  internal sealed class XmlNfoReader : XmlWrappingReader
  {
    // Nfo-files sometimes contain more than one root element, which according to xml-specs
    // results in a not "well-formed" xml-file. The XmlNfoReader transparently wrappes everything
    // after the xml-prolog of such nfo-files into a single <root> element.
    // If e.g. the nfo-file looks like this:
    //
    // <?xml version="1.0" encoding="UTF-16"?>
    // <tvshow>
    //    [...]
    // </tvshow>
    // <tvshow>
    //    [...]
    // </tvshow>
    //
    // the XmlNfoReader will make it look like
    //
    // <?xml version="1.0" encoding="UTF-16"?>
    // <root>
    //    <tvshow>
    //       [...]
    //    </tvshow>
    //    <tvshow>
    //       [...]
    //    </tvshow>
    // </root>

    #region Enums

    /// <summary>
    /// Position of the <see cref="XmlNfoReader"/> in the nfo-document
    /// </summary>
    private enum Position
    {
      Initial, // Before Read() was called
      Prolog, // While the reader is on a node in the xml-prolog
      OnBeginRoot, // When the reader is on the virtually inserted <root> node
      WithinRoot, // While the reader is on any element in the original nfo-document which is now within the <root> element
      OnEndRoot, // When the reader is on the virtually inserted </root> node
      EoF // When Read() was called when the reader was on the virtually inserted </root> node
    }

    #endregion

    #region Private fields

    /// <summary>
    /// Name of the virtually inserted root element
    /// </summary>
    private static readonly XmlQualifiedName ROOT_NAME = new XmlQualifiedName("root");

    /// <summary>
    /// Current position of the reader
    /// </summary>
    private Position _position = Position.Initial;

    #endregion

    #region Ctors

    public XmlNfoReader(Stream xmlStream) : base(Create(xmlStream, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }))
    {
    }

    #endregion

    #region Base overrides

    public override bool Read()
    {
      if (_position == Position.Initial || _position == Position.Prolog)
      {
        if (base.Read())
        {
          _position = base.NodeType != XmlNodeType.Element ? Position.Prolog : Position.OnBeginRoot;
          return true;
        }
        _position = Position.EoF;
        return false;
      }

      if (_position == Position.OnBeginRoot)
      {
        _position = Position.WithinRoot;
        return true;
      }

      if (_position == Position.WithinRoot)
      {
        if (!base.Read())
          _position = Position.OnEndRoot;
        return true;
      }

      if (_position == Position.OnEndRoot)
        _position = Position.EoF;

      return false;
    }

    public override async Task<bool> ReadAsync()
    {
      if (_position == Position.Initial || _position == Position.Prolog)
      {
        if (await base.ReadAsync().ConfigureAwait(false))
        {
          _position = base.NodeType != XmlNodeType.Element ? Position.Prolog : Position.OnBeginRoot;
          return true;
        }
        _position = Position.EoF;
        return false;
      }

      if (_position == Position.OnBeginRoot)
      {
        _position = Position.WithinRoot;
        return true;
      }

      if (_position == Position.WithinRoot)
      {
        if (!await base.ReadAsync().ConfigureAwait(false))
          _position = Position.OnEndRoot;
        return true;
      }

      if (_position == Position.OnEndRoot)
        _position = Position.EoF;

      return false;
    }

    public override ReadState ReadState
    {
      get { return _position == Position.OnEndRoot ? ReadState.Interactive : base.ReadState; }
    }

    public override XmlNodeType NodeType
    {
      get
      {
        if (_position == Position.OnBeginRoot)
          return XmlNodeType.Element;
        if (_position == Position.OnEndRoot)
          return XmlNodeType.EndElement;
        return base.NodeType;
      }
    }

    public override int Depth
    {
      get
      {
        if (_position == Position.WithinRoot)
          return base.Depth + 1;
        if (_position == Position.OnBeginRoot || _position == Position.OnEndRoot)
          return 0;
        return base.Depth;
      }
    }

    public override string LocalName
    {
      get { return (_position == Position.OnBeginRoot || _position == Position.OnEndRoot) ? ROOT_NAME.Name : base.LocalName; }
    }

    public override string NamespaceURI
    {
      get { return (_position == Position.OnBeginRoot || _position == Position.OnEndRoot) ? ROOT_NAME.Namespace : base.NamespaceURI; }
    }

    public override string Prefix
    {
      get { return (_position == Position.OnBeginRoot || _position == Position.OnEndRoot) ? String.Empty : base.Prefix; }
    }

    public override string Name
    {
      get
      {
        if (!(_position == Position.OnBeginRoot || _position == Position.OnEndRoot))
          return base.Name;
        // ReSharper disable once PossibleNullReferenceException
        return Prefix.Length == 0 ? LocalName : NameTable.Add(Prefix + ":" + LocalName);
      }
    }

    #endregion
  }
}
