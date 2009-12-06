#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Encapsulates a single queried table instance in a query.
  /// </summary>
  public class TableQueryData
  {
    protected readonly string _tableName;

    public TableQueryData(string tableName)
    {
      _tableName = tableName;
    }

    /// <summary>
    /// Creates a table query of the main media item aspect table of the given <paramref name="miaType"/>.
    /// </summary>
    /// <param name="miaManagement">MIA management instance.</param>
    /// <param name="miaType">Type of the MIA to request.</param>
    /// <returns>Table query for the given MIA.</returns>
    public static TableQueryData CreateTableQueryOfMIATable(MIA_Management miaManagement,
        MediaItemAspectMetadata miaType)
    {
      return new TableQueryData(miaManagement.GetMIATableName(miaType));
    }

    /// <summary>
    /// Creates a table query of the external table of an attribute of cardinality <see cref="Cardinality.ManyToOne"/>.
    /// </summary>
    /// <param name="miaManagement">MIA management instance.</param>
    /// <param name="spec">Attribute type of cardinality <see cref="Cardinality.ManyToOne"/> whose table should be requested.</param>
    /// <returns>Table query for the table of the given attribute type.</returns>
    public static TableQueryData CreateTableQueryOfMTOTable(MIA_Management miaManagement,
        MediaItemAspectMetadata.AttributeSpecification spec)
    {
      return new TableQueryData(miaManagement.GetMIACollectionAttributeTableName(spec));
    }

    public string TableName
    {
      get { return _tableName; }
    }

    public string GetAlias(Namespace ns)
    {
      return ns.GetOrCreate(this, "T");
    }

    public string GetDeclarationWithAlias(Namespace ns)
    {
      return _tableName + " " + ns.GetOrCreate(this, "T");
    }

    // Inherit Equals() and GetHashCode() from object class; different objects of this class need to be !=

    public override string ToString()
    {
      return _tableName;
    }
  }
}
