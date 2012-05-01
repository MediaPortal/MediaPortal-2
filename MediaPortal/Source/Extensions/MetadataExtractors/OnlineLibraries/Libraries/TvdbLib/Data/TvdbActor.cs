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
using TvdbLib.Data.Banner;

namespace TvdbLib.Data
{
  /// <summary>
  /// Represents an tvdb actor -> for more information see http://thetvdb.com/wiki/index.php/API:actors.xml
  /// </summary>
  [Serializable]
  public class TvdbActor
  {
    #region private member fields
    private int m_id;
    private TvdbActorBanner m_actorImage;
    private String m_name;
    private String m_role;
    private int m_sortOrder;
    #endregion

    /// <summary>
    /// This matches the First, Second, Third, and Don't Care options on the site, which determine if the actor is shown on the series page or not. First (SortOrder=0), Second (SortOrder=1), and Third (SortOrder=2) generally mean the actor plays a primary role in the series. Don't Care (SortOrder=3) generally means the actor plays a lesser role. In some series there are no primary actors, so all actors will have a SortOrder of 3. The actors are also listed in the report in SortOrder, followed by those with images, and then finally by Name. So using the order they show up in the file is a valid method. 
    /// </summary>
    public int SortOrder
    {
      get { return m_sortOrder; }
      set { m_sortOrder = value; }
    }

    /// <summary>
    /// The name of the actor's character in the series. This may include multiple roles in comma-separated format. 
    /// </summary>
    public String Role
    {
      get { return m_role; }
      set { m_role = value; }
    }

    /// <summary>
    /// The actual name of the actor. 
    /// </summary>
    public String Name
    {
      get { return m_name; }
      set { m_name = value; }
    }

    /// <summary>
    /// The image for the actor in this role.
    /// </summary>
    public TvdbActorBanner ActorImage
    {
      get { return m_actorImage; }
      set { m_actorImage = value; }
    }

    /// <summary>
    /// A unique id per actor. At some point actors will be globally unique but for now they're just unique per series. 
    /// </summary>
    public int Id
    {
      get { return m_id; }
      set { m_id = value; }
    }
  }
}
