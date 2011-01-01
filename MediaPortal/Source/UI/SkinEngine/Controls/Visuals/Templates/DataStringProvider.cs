#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Exceptions;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  /// <summary>
  /// Class which holds a property path expression which can be evaluated on a source element to generate a
  /// data string.
  /// </summary>
  public class DataStringProvider : IInitializable
  {
    #region Protected fields

    protected string _path = string.Empty;
    protected PathExpression _compiledPath = null;

    #endregion

    /// <summary>
    /// Gets or sets a property path expression to the data string corresponding to a given object.
    /// </summary>
    public string Path
    {
      get { return _path; }
      set { _path = value; }
    }

    public string GenerateDataString(object source)
    {
      IDataDescriptor result;
      _compiledPath.Evaluate(new ValueDataDescriptor(source), out result);
      return (string) TypeConverter.Convert(result.Value, typeof(string));
    }

    void IInitializable.Initialize(IParserContext context)
    {
      if (_path == null)
        throw new XamlBindingException("DataStringProvider: Path mustn't be null");
      _compiledPath = PathExpression.Compile(context, _path);
    }
  }
}