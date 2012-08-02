#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System.Collections.ObjectModel;

namespace MP2_PluginWizard.Model
{
	/// <summary>
	/// Holds all item data for a plugin register location.
	/// </summary>
	public class PluginRegister
	{
		#region Ctor
		public PluginRegister(string location)
		{
			Items = new ObservableCollection<PluginRegisterItem>();
			Location = location;
		}
		
		#endregion
		
		#region Public properties
		public string Location { get; private set; }
		public ObservableCollection<PluginRegisterItem> Items { get; private set; }
		
		#endregion

    #region Base overrides

    public override string ToString()
    {
      return string.Format("{0}", Location);
    }

    #endregion

	}
}
