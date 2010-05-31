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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.Commands;

namespace MediaPortal.UI.SkinEngine.MarkupExtensions
{
  /// <summary>
  /// Implements the MPF Command markup extension.
  /// </summary>
  public class CommandMarkupExtension : CommandBaseMarkupExtension, IExecutableCommand
  {
    #region Protected fields

    protected List<object> _parameters = new List<object>();

    #endregion

    #region Ctor

    public CommandMarkupExtension(): base()
    { }

    public CommandMarkupExtension(string path): base(path)
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandMarkupExtension cme = (CommandMarkupExtension) source;
      foreach (object o in cme._parameters)
        _parameters.Add(copyManager.GetCopy(o));
    }

    #endregion

    #region Public properties

    public IList<object> Parameters
    {
      get { return _parameters; }
    }

    #endregion

    #region Protected properties and methods

    protected override string CommandTypeName
    {
      get { return "Command"; }
    }

    #endregion

    #region IExecutableCommand implementation

    public void Execute()
    {
      Execute(_parameters);
    }

    #endregion
  }
}
