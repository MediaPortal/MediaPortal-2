#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using MP2BootstrapperApp.Models;
using System.Text.RegularExpressions;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallationInProgressStep : AbstractInstallStep, IStep
  {
    // Regex to parse out any template parameters in an msi message.
    // e.g. a typical message is:
    // Removing system registry values Key: [1], Name: [2]
    // we want to remove the  Key: [1], Name: [2] part
    protected const string MSI_MESSAGE_PATTERN = @"([A-Z][a-z]*)?:\s*\[[0-9]+\]";
    protected static Regex MSI_MESSAGE_REGEX = new Regex(MSI_MESSAGE_PATTERN);

    public InstallationInProgressStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
    {
    }

    /// <summary>
    /// Parses an MSI action message and returns the action text with any parameter templates removed.
    /// </summary>
    /// <param name="message">The MSI message to parse.</param>
    /// <returns>The message text with any parameter templates removed.</returns>
    /// <remarks>
    /// A typical message is in the form<br/>
    /// Removing system registry values Key: [1], Name: [2]<br/>
    /// This method removes the Key: [1], Name: [2] part of the message.
    /// </remarks>
    public string ParseActionMessage(string message)
    {
      Match m = MSI_MESSAGE_REGEX.Match(message);
      if (m.Success)
        message = message.Remove(m.Index);
      // Some messages just end with a colon without a parameter template,
      // so remove any trailing colons too.
      message = message.TrimEnd().TrimEnd(':');
      return message;
    }

    public IStep Next()
    {
      // not allowed
      return null;
    }

    public bool CanGoNext()
    {
      return false;
    }

    public bool CanGoBack()
    {
      return false;
    }
  }
}
