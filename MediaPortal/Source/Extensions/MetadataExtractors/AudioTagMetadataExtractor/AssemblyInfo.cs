/*
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

using System.Runtime.CompilerServices;

// The role extractors that build an album or a person out of a track's tag are reached only through the
// relationship extractor this plugin registers, so nothing outside the plugin names them. The server's
// test assembly does, because whether "Track->Album" hands back the album the tag describes is a question
// that cannot be answered by looking at the aggregator alone - and it is the same arrangement the NFO
// plugin has, whose role extractors are internal too.
[assembly: InternalsVisibleTo("Tests.Server")]
