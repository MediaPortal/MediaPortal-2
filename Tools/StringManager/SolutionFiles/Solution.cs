#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.Tools.StringManager.SolutionFiles
{
  class Solution
  {
    List<CSProject> _projects;

    public List<CSProject> Projects
    {
      get { return _projects; }
    }

    public Solution(string path)
    {
      string baseDir = Path.GetDirectoryName(path);
      

      FileList solution = new FileList("Project(", ".csproj", ',');

      string[] projectFiles = solution.GetList(path);

      FileList project = new FileList("Compile Include=", ".cs", '"');

      _projects = new List<CSProject>();

      for(int i=0; i < projectFiles.Length; i++)
      {
        CSProject csProject = new CSProject();
        csProject.dir = Path.GetDirectoryName(Path.Combine(baseDir, projectFiles[i]));
        csProject.name = projectFiles[i];
        csProject.csList = project.GetList(Path.Combine(baseDir, projectFiles[i]));
        _projects.Add(csProject);
      }
    }
  }
}
