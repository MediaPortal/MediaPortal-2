#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System.Reflection;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.Controls.Bindings;
using Presentation.SkinEngine.XamlParser;

namespace Presentation.SkinEngine.MarkupExtensions
{

  /// <summary>
  /// Implements the MPF Command markup extension.
  /// </summary>
  public class CommandMarkupExtension: BindingMarkupExtension
  {
    protected string _parameter = null;

    public CommandMarkupExtension()
    {
      Mode = BindingMode.OneTime;
    }

    public CommandMarkupExtension(string path): base(path)
    {
      Mode = BindingMode.OneTime;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandMarkupExtension cme = source as CommandMarkupExtension;
      CommandParameter = copyManager.GetCopy(cme.CommandParameter);
    }

    #region Public properties

    public string CommandParameter
    {
      get { return _parameter; }
      set { _parameter = value; }
    }

    #endregion

    #region Protected methods

    protected override bool UpdateSourceValue()
    {
      IDataDescriptor evaluatedValue;
      if (!GetSourceDataDescriptor(out evaluatedValue))
        // Do nothing if not all necessary properties can be resolved at the current time
        return false;
      if (_compiledPath == null)
        return false;
        try
        {
          object obj;
          MethodInfo mi;
          if (!_compiledPath.GetMethod(evaluatedValue, out obj, out mi))
            return false;
          Command cmd = new Command();
          cmd.Object = obj;
          cmd.Method = mi;
          cmd.Parameter = CommandParameter;
          _evaluatedSourceValue.SourceValue = new ValueDataDescriptor(cmd);
          return true;
        }
        catch (XamlBindingException)
        {
          return false;
        }
    }

    #endregion
  }
}
