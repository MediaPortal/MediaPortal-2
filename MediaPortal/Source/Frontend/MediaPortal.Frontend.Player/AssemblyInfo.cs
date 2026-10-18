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

// The one hazard in this assembly that a test cannot reach from outside is a buffer that an engine acquired and
// has not handed back: only code inside the assembly acquires buffers, because handing an engine a pointer into a
// managed array is exactly what the seam exists to keep off the public surface. And that hazard is the one worth
// testing - the day it is wrong, a decoder writes into memory the collector has moved, which is not an exception
// but a corrupted process. So the front end's test assembly is made a friend for that purpose alone.
[assembly: InternalsVisibleTo("Tests.Frontend")]
