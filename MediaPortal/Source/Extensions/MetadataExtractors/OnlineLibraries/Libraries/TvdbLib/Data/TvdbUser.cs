/*
 *   TvdbLib: A library to retrieve information and media from http://thetvdb.com
 * 
 *   Copyright (C) 2008  Benjamin Gmeiner
 * 
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TvdbLib.Data
{
  /// <summary>
  /// Class that holds all user information
  /// </summary>
  [Serializable]
  public class TvdbUser
  {
    #region private properties
    private String m_userName;
    private String m_userIdentifier;
    private TvdbLanguage m_userPreferredLanguage;
    private List<int> m_userFavorites;
    #endregion

    /// <summary>
    /// TvdbUser constructor
    /// </summary>
    /// <param name="_username">Name of the user, can be choosen freely</param>
    /// <param name="_userIdentifier">User identifier from http://thetvdb.com</param>
    public TvdbUser(String _username, String _userIdentifier)
      : this()
    {
      m_userName = _username;
      m_userIdentifier = _userIdentifier;
    }

    /// <summary>
    /// TvdbUser constructor
    /// </summary>
    public TvdbUser()
    {

    }

    /// <summary>
    /// Preferred language of the user
    /// </summary>
    public TvdbLanguage UserPreferredLanguage
    {
      get { return m_userPreferredLanguage; }
      set { m_userPreferredLanguage = value; }
    }

    /// <summary>
    /// This is the unique identifier assigned to every user. They can access this value by visiting the account settings page on the site. This is a 16 character alphanumeric string, but you should program your applications to handle id strings up to 32 characters in length. 
    /// </summary>
    public String UserIdentifier
    {
      get { return m_userIdentifier; }
      set { m_userIdentifier = value; }
    }

    /// <summary>
    /// Username
    /// </summary>
    public String UserName
    {
      get { return m_userName; }
      set { m_userName = value; }
    }

    /// <summary>
    /// List of user favorites
    /// </summary>
    public List<int> UserFavorites
    {
      get { return m_userFavorites; }
      set { m_userFavorites = value; }
    }
  }
}
