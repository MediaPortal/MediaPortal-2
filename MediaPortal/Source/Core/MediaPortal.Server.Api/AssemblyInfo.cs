/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU Public License as published by
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

// The sizes a resized picture is made at are a private decision of the picture shelf - nothing outside it names
// them, and nothing outside it should, since a front end asks for a width and the server picks the step. The
// server's test assembly does, because "which step does a front end that said 432 get" is the whole of the
// promise the shelf makes, and it cannot be answered by reading the answer back from a file alone. It is the
// same arrangement the audio tag plugin has.
[assembly: InternalsVisibleTo("Tests.Server")]
