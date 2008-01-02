using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BuildReport
{
  class WriteReport
  {
    StreamWriter report;

    public WriteReport(string filename, string svn)
    {
      report = File.CreateText(filename);

      report.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD html 4.01 Transitional//EN\" >");
      report.WriteLine("<html>");
      report.WriteLine("<head>");
      report.WriteLine("<META http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">");
      report.WriteLine("<title>MediaPortal - Build Results {0}</title>", svn);
      report.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"./css/BuildReport.css\" title=\"Style\">");
      report.WriteLine("</head>");
      report.WriteLine("<body>");
      report.WriteLine("<h2>MediaPortal Build Results {0}</h2>", svn);
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
      report.WriteLine("<th width=\"70%\" align=\"left\" colspan=\"2\">Name</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Succeeded</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Failed</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Skipped</th>");
      report.WriteLine("</tr>");
    }

    public void WriteSolution(Solution solution)
    {
      if (solution.failed > 0)
      {
        report.WriteLine("<tr align=\"left\" class=\"failure\">");
        report.WriteLine("  <td width=\"4%\">");
        report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_error_sml.gif\" alt=\"error\">");
      }
      else
      {
        report.WriteLine("<tr align=\"left\" class=\"success\">");
        report.WriteLine("  <td width=\"4%\">");
        report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_success_sml.gif\" alt=\"success\">");
      }
      report.WriteLine("   </td> ");
      report.WriteLine("  <td>{0}</td>", solution.name);
      report.WriteLine("  <td>{0}</td>", solution.succeeded.ToString());
      report.WriteLine("  <td>{0}</td>", solution.failed.ToString());
      report.WriteLine("  <td>{0}</td>", solution.skipped.ToString());
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
      report.WriteLine("<th width=\"70%\" align=\"left\" colspan=\"2\">Name</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Compile</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Errors</th>");
      report.WriteLine("<th width=\"10%\" align=\"left\">Warnings</th>");
      report.WriteLine("</tr>");
    }

    public void WriteProject(Project project)
    {
      if (project.errors > 0)
      {
        report.WriteLine("<tr align=\"left\" class=\"failure\">");
        report.WriteLine("  <td width=\"4%\">");
        report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_error_sml.gif\" alt=\"error\">");
      }
      else
      {
        if (project.warnings > 0)
        {
          report.WriteLine("<tr align=\"left\" class=\"warning\">");
          report.WriteLine("  <td width=\"4%\">");
          report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_warning_sml.gif\" alt=\"warning\">");

        }
        else
        {
          report.WriteLine("<tr align=\"left\" class=\"success\">");
          report.WriteLine("  <td width=\"4%\">");
          report.WriteLine("    <img width=\"15\" height=\"15\" src=\"./images/icon_success_sml.gif\" alt=\"success\">");
        }
      }
      report.WriteLine("   </td> ");
      report.WriteLine("  <td>{0}</td>", project.name);
      report.WriteLine("  <td>{0}</td>", project.build);
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
