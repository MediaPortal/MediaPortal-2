using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Tools.BuildReport
{
  class Solution
  {
    public enum Compile
    {
      Success,
      Failed,
      Skipped
    }

    string _name;
    int _succeeded = 0;
    int _failed = 0;
    int _skipped = 0;
    int _totalWarnings = 0;
    int _totalErrors = 0;
    List<Project> _projects;

    public Solution()
    {
      _succeeded = 0;
      _failed = 0;
      _skipped = 0;
      _totalWarnings = 0;
      _totalErrors = 0;
      _projects = new List<Project>();
    }

    public String Name
    {
      get { return _name; }
      set { _name = value; }
    }

    public int Succeeded
    {
      get { return _succeeded; }
      set { _succeeded = value; }
    }

    public int Failed
    {
      get { return _failed; }
      set { _failed = value; }
    }

    public int Skipped
    {
      get { return _skipped; }
      set { _skipped = value; }
    }

    public int TotalWarnings
    {
      get { return _totalWarnings; }
    }

    public int TotalErrors
    {
      get { return _totalErrors; }
    }

    public List<Project> Projects
    {
      get { return _projects; }
    }

    public void AddProject(Project newProject)
    {
      _totalErrors += newProject.errors;
      _totalWarnings += newProject.warnings;
      switch (newProject.build)
      {
        case Compile.Success:
          _succeeded++;
          break;
        case Compile.Failed:
          _failed++;
          break;
        case Compile.Skipped:
          _skipped++;
          break;
        default:
          break;
      }
      _projects.Add(newProject);
    }
  }
}
