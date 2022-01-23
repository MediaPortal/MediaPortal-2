#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System.Collections.Generic;

namespace MP2BootstrapperApp.WizardSteps
{
  public class Wizard
  {
    private Stack<IStep> stepsStack = new Stack<IStep>();

    public Wizard(IStep step)
    {
      stepsStack.Push(step);
    }

    public IStep Step
    {
      get { return stepsStack.Peek(); }
    }

    public bool GoNext()
    {
      IStep next = Step.Next();
      return Push(next);
    }

    public bool Push(IStep step)
    {
      if (step == null)
        return false;
      stepsStack.Push(step);
      return true;
    }

    public bool GoBack()
    {
      if (stepsStack.Count == 1)
        return false;
      stepsStack.Pop();
      return true;
    }

    public bool CanGoNext()
    {
      return Step.CanGoNext();
    }

    public bool CanGoBack()
    {
      return Step.CanGoBack();
    }
  }
}
