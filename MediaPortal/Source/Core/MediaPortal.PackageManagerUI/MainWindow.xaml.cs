#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MediaPortal.Common.Logging;

namespace MediaPortal.PackageManagerUI
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, ILogger
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    private void LogText(string text, Brush foreground, Brush background = null)
    {
      if (!Dispatcher.CheckAccess())
      {
        Dispatcher.BeginInvoke(new Action<string, Brush, Brush>(LogText), text, foreground, background);
        return;
      }
      TextRange tr = new TextRange(LogBox.Document.ContentEnd, LogBox.Document.ContentEnd);
      tr.Text = text.Replace("\n", "\r") + "\r";
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);
      tr.ApplyPropertyValue(TextElement.BackgroundProperty, background ?? Brushes.Transparent);

      LogBox.ScrollToEnd();
    }

    #region Implementation of ILogger

    void ILogger.Debug(string format, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.DarkGray);
    }

    void ILogger.Debug(string format, Exception ex, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.DarkGray);
      LogText(ex.ToString(), Brushes.DarkGray);
    }

    void ILogger.Info(string format, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.CornflowerBlue);
    }

    void ILogger.Info(string format, Exception ex, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.CornflowerBlue);
      LogText(ex.ToString(), Brushes.CornflowerBlue);
    }

    void ILogger.Warn(string format, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.DarkOrange);
    }

    void ILogger.Warn(string format, Exception ex, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.DarkOrange);
      LogText(ex.ToString(), Brushes.DarkOrange);
    }

    void ILogger.Error(string format, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.Red);
    }

    void ILogger.Error(string format, Exception ex, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.Red);
      LogText(ex.ToString(), Brushes.Red);
    }

    void ILogger.Error(Exception ex)
    {
      LogText(ex.ToString(), Brushes.Red);
    }

    void ILogger.Critical(string format, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.Yellow, Brushes.Red);
    }

    void ILogger.Critical(string format, Exception ex, params object[] args)
    {
      LogText(String.Format(format, args), Brushes.Yellow, Brushes.Red);
      LogText(ex.ToString(), Brushes.Yellow, Brushes.Red);
    }

    void ILogger.Critical(Exception ex)
    {
      LogText(ex.ToString(), Brushes.Yellow, Brushes.Red);
    }

    #endregion

    private void MainWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      DragMove();
    }
  }
}
