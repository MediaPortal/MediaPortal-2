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
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MediaPortal.Mock
{
  public class MockReader : IDataReader
  {
    private int _id;
    private IList<string> _columns = new List<string>();
    private readonly IList<IDictionary<int, string>> _results = new List<IDictionary<int, string>>();
    private int _index = -1;

    public MockReader(int id, string[] columns)
    {
      _id = id;
      _columns = columns.ToArray();
    }

    public int Id { get { return _id; } }

    public void AddResult(params object[] values)
    {
      IDictionary<int, string> result = new Dictionary<int, string>();
      for (int index = 0; index < values.Length; index++)
      {
        result[index] = values[index] != null ? values[index].ToString() : null;
      }
      _results.Add(result);
    }

    public void Close()
    {
      throw new NotImplementedException();
    }

    public int Depth
    {
      get { throw new NotImplementedException(); }
    }

    public DataTable GetSchemaTable()
    {
      throw new NotImplementedException();
    }

    public bool IsClosed
    {
      get { throw new NotImplementedException(); }
    }

    public bool NextResult()
    {
      throw new NotImplementedException();
    }

    public bool Read()
    {
      _index++;
      return _index < _results.Count;
    }

    public int RecordsAffected
    {
      get { throw new NotImplementedException(); }
    }

    public void Dispose()
    {
    }

    public int FieldCount
    {
      get { throw new NotImplementedException(); }
    }

    public bool GetBoolean(int i)
    {
      try
      {
        return Boolean.Parse(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as boolean column " + i, e);
      }
    }

    public byte GetByte(int i)
    {
      throw new NotImplementedException();
    }

    public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
      throw new NotImplementedException();
    }

    public char GetChar(int i)
    {
      throw new NotImplementedException();
    }

    public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
    {
      throw new NotImplementedException();
    }

    public IDataReader GetData(int i)
    {
      throw new NotImplementedException();
    }

    public string GetDataTypeName(int i)
    {
      throw new NotImplementedException();
    }

    public DateTime GetDateTime(int i)
    {
      try
      {
        if (_results[_index][i] == null)
          return DateTime.MinValue;

        return DateTime.Parse(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as datetime column " + i, e);
      }
    }

    public decimal GetDecimal(int i)
    {
      throw new NotImplementedException();
    }

    public double GetDouble(int i)
    {
      try
      {
        if (_results[_index][i] == null)
          return double.MinValue;

        return Double.Parse(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as double column " + i, e);
      }
    }

    public Type GetFieldType(int i)
    {
      throw new NotImplementedException();
    }

    public float GetFloat(int i)
    {
      throw new NotImplementedException();
    }

    public Guid GetGuid(int i)
    {
      try
      {
        if (_results[_index][i] == null)
          return Guid.Empty;

        return new Guid(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as GUID column " + i, e);
      }
    }

    public short GetInt16(int i)
    {
      throw new NotImplementedException();
    }

    public int GetInt32(int i)
    {
      try
      {
        if (_results[_index][i] == null)
          return 0;

        return Int32.Parse(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as integer column " + i, e);
      }
    }

    public long GetInt64(int i)
    {
      try
      {
        if (_results[_index][i] == null)
          return 0;

        return Int64.Parse(_results[_index][i]);
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
      catch (FormatException e)
      {
        throw new FormatException("Cannot parse " + _results[_index][i] + " as integer column " + i, e);
      }
    }

    public string GetName(int i)
    {
      return _columns[i];
    }

    public int GetOrdinal(string name)
    {
      int ordinal = _columns.IndexOf(name);
      if (ordinal == -1)
      {
        throw new KeyNotFoundException(string.Format("No ordinal for {0}", name));
      }
      return ordinal;
    }

    public string GetString(int i)
    {
      if (_index >= _results.Count)
      {
        throw new IndexOutOfRangeException();
      }

      string value;
      if (!_results[_index].TryGetValue(i, out value))
      {
        throw new KeyNotFoundException(string.Format("No key {0}", i));
      }

      return value;
    }

    public object GetValue(int i)
    {
      try
      {
        return _results[_index][i];
      }
      catch (KeyNotFoundException e)
      {
        throw new KeyNotFoundException("Column " + i + " not found", e);
      }
    }

    public int GetValues(object[] values)
    {
      throw new NotImplementedException();
    }

    public bool IsDBNull(int i)
    {
      return _results[_index][i] == null;
    }

    public object this[string name]
    {
      get { throw new NotImplementedException(); }
    }

    public object this[int i]
    {
      get { throw new NotImplementedException(); }
    }
  }
}