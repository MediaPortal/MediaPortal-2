#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageCore.Package.Root
{
  [DebuggerDisplay("InstallOptions({Options.Count})")]
  partial class InstallOptionsModel : ICheckable
  {
    #region private fields

    private WeakReference<PackageModel> _package;

    #endregion

    #region public properties

    [XmlIgnore]
    public PackageModel Package
    {
      get
      {
        PackageModel package;
        if (_package != null && _package.TryGetTarget(out package))
        {
          return package;
        }
        return null;
      }
      private set { _package = new WeakReference<PackageModel>(value); }
    }

    #endregion


    #region public methods

    /// <summary>
    /// Gets the default option of the package.
    /// </summary>
    /// <returns>Returns the default option or <c>null</c> if the package does not contain any option.
    /// If no option is labeled as default. the 1st option isreturned.
    /// Since a default option with all contents is generated automatically if the package has no options defined, <c>null</c> should not be at ay time.
    /// </returns>
    public InstallOptionModel GetDefaultOption()
    {
      foreach (var option in Options)
      {
        if (option.IsDefault)
        {
          return option;
        }
      }
      return Options.FirstOrDefault();
    }

    /// <summary>
    /// Gets the option with the given name.
    /// </summary>
    /// <param name="name">Name of the option. Option names are not case sensitive.</param>
    /// <returns>Returns the option or <c>null</c> if no option with the given name is found.</returns>
    public InstallOptionModel GetOption(string name)
    {
      return Options.FirstOrDefault(model => String.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion


    #region private/internal methods

    internal void Initialize(PackageModel package, ILogger log)
    {
      Package = package;
      foreach (var option in Options)
      {
        option.Initialize(this, log);
      }
    }

    #endregion

    #region Implementation of ICheckable

    [XmlIgnore]
    public string ElementsName
    {
      get { return "InstallOptions"; }
    }

    public bool CheckElements(ILogger log)
    {
      return Options.Aggregate(true, (current, option) => current & option.CheckElements(log));
    }

    #endregion
  }
}