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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaPortal.Tools.BuildReport
{
  class WriteReport
  {
    StreamWriter report;

    public WriteReport(string filename, string title)
    {
      report = File.CreateText(filename);

      report.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD html 4.01 Transitional//EN\" >");
      report.WriteLine("<html>");
      report.WriteLine("<head>");
      report.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
      report.WriteLine("<title>{0}</title>", title);
      report.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"./css/BuildReport.css\" title=\"Style\">");
      report.WriteLine("</head>");
      report.WriteLine("<body>");
      report.WriteLine("<h2>{0}</h2>", title);
      report.WriteLine("<hr>");
      report.WriteLine("<h2>");
      report.WriteLine("<a name=\"Solution\">Solution</a>");
      report.WriteLine("</h2>");
      report.WriteLine("<p>");
      report.WriteLine("[<a href=\"#Solution\">Solution</a>]");
      report.WriteLine("[<a href=\"#Projects\">Projects</a>]");
      report.WriteLine("[<a href=\"#Output\">Build Output</a>]");
      report.WriteLine("</p>");
      report.WriteLine("<table border=\"0\" rules=\"none\" width=\"100%\">");
      report.WriteLine("<tr align=\"left\" class=\"title\">");
      report.WriteLine("<th width=\"65%\" align=\"left\" colspan=\"2\">Name</th>");
      report.WriteLine("<th width=\"5%\" align=\"left\">Success</th>");
      report.WriteLine("<th width=\"5%\" align=\"left\">Failed</th>");
      report.WriteLine("<th width=\"5%\" align=\"left\">Skipped</th>");
      report.WriteLine("<td width=\"10%\" align=\"left\">Total Errors</th>");
      report.WriteLine("<td width=\"10%\" align=\"left\">Total Warnings</th>");
      report.WriteLine("</tr>");
    }

    public void WriteSolution(Solution solution)
    {
      bool failed = false;
      if (solution.Failed > 0 || solution.TotalErrors > 0)
      {
        failed = true;
        report.WriteLine("<tr align=\"left\" class=\"error\">");
        report.WriteLine("  <td width=\"2%\">");
        report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_error_sml.gif\" alt=\"error\">");
      }
      else
      {
        report.WriteLine("<tr align=\"left\" class=\"success\">");
        report.WriteLine("  <td width=\"2%\">");
        report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_success_sml.gif\" alt=\"success\">");
      }
      report.WriteLine("   </td> ");
      report.WriteLine("  <td>&nbsp;{0}</td>", solution.Name);
      report.WriteLine("  <td>{0}</td>", solution.Succeeded.ToString());
      report.WriteLine("  <td>{0}</td>", solution.Failed.ToString());
      report.WriteLine("  <td>{0}</td>", solution.Skipped.ToString());
      report.WriteLine("  <td>{0}</td>", solution.TotalErrors.ToString());
      if (!failed && solution.TotalWarnings > 0)
        report.WriteLine("  <td class=\"warning\">");
      else
        report.WriteLine("  <td>");
      report.WriteLine("  {0}</td>", solution.TotalWarnings.ToString());
      report.WriteLine(" </tr>");
      report.WriteLine(" </table>");
      report.WriteLine("<hr>");
      report.WriteLine("<h2>");
      report.WriteLine("<a name=\"Projects\">Projects</a>");
      report.WriteLine("</h2>");
      report.WriteLine("<p>");
      report.WriteLine("[<a href=\"#Solution\">Solution</a>]");
      report.WriteLine("[<a href=\"#Projects\">Projects</a>]");
      report.WriteLine("[<a href=\"#Output\">Build Output</a>]");
      report.WriteLine("</p>");
      report.WriteLine("<table border=\"0\" rules=\"none\" width=\"100%\">");
      report.WriteLine("<tr align=\"left\" class=\"title\">");
      report.WriteLine("<th width=\"65%\" align=\"left\" colspan=\"2\">Name</th>");
      report.WriteLine("<th width=\"15%\" align=\"left\">Compile</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Errors</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Warnings</th>");
      report.WriteLine("</tr>");

      foreach (Project project in solution.Projects)
        WriteProject(project);
    }

    public void WriteProject(Project project)
    {
      report.WriteLine(" <tr align=\"left\" class=\"{0}\">", project.Type.ToString());
      report.WriteLine("  <td width=\"2%\">");
      report.WriteLine("   <img width=\"15\" height=\"15\" src=\"./images/icon_{0}_sml.gif\" alt=\"{0}\">", project.Type.ToString());
      report.WriteLine("  </td> ");
      report.WriteLine("  <td>&nbsp;{0}</td>", project.name);
      report.WriteLine("  <td>{0}</td>", project.build.ToString());
      report.WriteLine("  <td>{0}</td>", project.errors.ToString());
      report.WriteLine("  <td>{0}</td>", project.warnings.ToString());
      report.WriteLine(" </tr>");
    }

    public void WriteBuildReport(string buildreport)
    {
      report.WriteLine(" </table>");
      report.WriteLine("<hr>");
      report.WriteLine("<h2>");
      report.WriteLine("<a name=\"Output\">Build Output</a>");
      report.WriteLine("</h2>");
      report.WriteLine("<p>");
      report.WriteLine("[<a href=\"#Solution\">Solution</a>]");
      report.WriteLine("[<a href=\"#Projects\">Projects</a>]");
      report.WriteLine("[<a href=\"#Output\">Build Output</a>]");
      report.WriteLine("</p>");
      report.WriteLine("<table border=\"0\" rules=\"none\" width=\"100%\">");
      report.WriteLine("<tr align=\"left\" class=\"title\">");
      report.WriteLine("<th width=\"70%\" align=\"left\">Build Report</th>");
      report.WriteLine("</tr>");
      report.WriteLine("<tr align=\"left\">");
      report.WriteLine("<td align=\"left\" class=\"buildreport\">");
      report.WriteLine("<pre>");
      report.Write(buildreport);
      report.WriteLine("</pre>");
      report.WriteLine("</td>");
      report.WriteLine("</tr>");
      report.WriteLine("</table>");
      report.WriteLine("<br>");
      report.WriteLine("</body>");
      report.WriteLine("</html>");
      report.Close();
    }
  }
}
