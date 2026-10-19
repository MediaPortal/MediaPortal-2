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

// Two things on the folders screen belong to nobody but a test: the task that watches the server's import queue
// (a screen that stops watching when it closes has to be proven to stop, which means waiting for the watch) and
// how long it waits between looks at that queue (the real figure is a and a half seconds, and a suite that slept
// through it would take minutes to say nothing interesting). Both are kept off the surface a screen can reach and
// opened to the front end's own test assembly, which is the only place that asks for them.
[assembly: InternalsVisibleTo("Tests.Frontend")]
