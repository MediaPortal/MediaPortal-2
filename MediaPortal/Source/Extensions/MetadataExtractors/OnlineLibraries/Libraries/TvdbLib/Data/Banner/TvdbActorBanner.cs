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

namespace TvdbLib.Data.Banner
{
  /// <summary>
  /// An actor poster
  ///     *  Actor images must be 300px x 450px and must fill the entire image. Do not add black bars to the sides to get it to that size.
  ///         * Actor images must be smaller than 100kb
  ///         * Low quality images should not be scaled up to fit the resolution. Use only high quality art.
  ///         * Actor images should show the actor in that particular role, wearing the clothes/makeup they'd wear on the series. Unless it's a cartoon, in which case just a normal picture of the voice actor will do.
  ///         * Try to shy away from full body shots. Ideally include some upper body but don't go to far past the waist.
  ///         * No nudity, even if the actor is playing the role of a striper who is almost always nude, the images must be family safe. 
  /// </summary>
  [Serializable]
  public class TvdbActorBanner: TvdbBanner
  {

  }
}
