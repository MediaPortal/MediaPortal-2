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

using MediaPortal.UI.SkinEngine.Controls.Panels;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using SharpDX;
using SharpDX.Mathematics.Interop;
using Xunit;

namespace Tests
{
  public class SkinEngineTests
  {
    [Fact]
    public void Should_Measure_And_Arrange_StackPanel_When_No_Margin()
    {
      // Arrange
      StackPanel panel = new StackPanel { Height = 12, Width = 12, Margin = new Thickness { Right = 0, Left = 0, Top = 0, Bottom = 0 } };
      Size2F size = new Size2F
      {
        Height = 50,
        Width = 50
      };

      RawRectangleF rec = new RawRectangleF
      {
        Left = 100,
        Top = 100,
        Right = 150,
        Bottom = 150
      };

      // Act
      panel.Measure(ref size);
      panel.Arrange(rec);

      // Assert
      Assert.Equal(12, panel.DesiredSize.Width);
      Assert.Equal(12, panel.DesiredSize.Height);

      Assert.Equal(50, panel.ActualWidth);
      Assert.Equal(50, panel.ActualHeight);

      float expectedPosX = 100;
      float expectedPosY = 100;
      float actualPosX = panel.ActualPosition.X;
      float actualPosY = panel.ActualPosition.Y;
      Assert.Equal(expectedPosX, actualPosX);
      Assert.Equal(expectedPosY, actualPosY);
    }

    [Fact]
    public void Should_Measure_And_Arrange_StackPanel_When_Margin_Is_Set()
    {
      // Arrange
      StackPanel panel = new StackPanel { Height = 12, Width = 12, Margin = new Thickness { Right = 1, Left = 1, Top = 1, Bottom = 1 } };
      Size2F size = new Size2F
      {
        Height = 50,
        Width = 50
      };

      RawRectangleF rec = new RawRectangleF
      {
        Left = 100,
        Top = 100,
        Right = 150,
        Bottom = 150
      };

      // Act
      panel.Measure(ref size);
      panel.Arrange(rec);

      // Assert
      Assert.Equal(14, panel.DesiredSize.Width);
      Assert.Equal(14, panel.DesiredSize.Height);

      Assert.Equal(48, panel.ActualWidth);
      Assert.Equal(48, panel.ActualHeight);

      float expectedPosX = 101;
      float expectedPosY = 101;
      float actualPosX = panel.ActualPosition.X;
      float actualPosY = panel.ActualPosition.Y;
      Assert.Equal(expectedPosX, actualPosX);
      Assert.Equal(expectedPosY, actualPosY);
    }
  }
}