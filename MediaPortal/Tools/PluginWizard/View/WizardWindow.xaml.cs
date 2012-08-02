#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;
using MP2_PluginWizard.ViewModel;
using MP2_PluginWizard.Utils;

namespace MP2_PluginWizard.View
{
  /// <summary>
  /// Interaction logic for WizardWindow.xaml
  /// </summary>
  public partial class WizardWindow : INotifyPropertyChanged
  {
    #region Private/protected fields

    private readonly PluginDataViewModel _viewModel;
    private int _currentPage;
    private bool _showFinishButton;
    private bool _showCancelButton;
    private bool _showNextButton;
    private bool _showPrevButton;
    private bool _showHelpButton;
    
    private RelayCommand _nextPageCommand;

    #endregion

    #region Ctor/Dtor
    public WizardWindow()
    {
      InitializeComponent();
      
      ShowHelpButton = false;
      ShowCancelButton = true;
      
      _viewModel = new PluginDataViewModel();

      UserControlCollection = new ObservableCollection<WizardPage>
                                {
      														new WelcomePage { DataContext = _viewModel },
                                  new BasicPluginDataPage { DataContext = _viewModel },
                                  new DependsConflictsBuilderPage { DataContext = _viewModel },
                                  new RegisterPage { DataContext = _viewModel },
                                  new ResultPage { DataContext = _viewModel }
                                };
      _currentPage = -1;
      
      DataContext = this;
    }

    #endregion

    #region Public properties
    public int CurrentPage
    {
      get { return _currentPage; }
      set
      {
        if (_currentPage == value) return;

        _currentPage = value;
        WizardContent.Content = UserControlCollection[_currentPage];
        UserControlCollection[_currentPage].OnSwitchedTo();

        ShowFinishButton = (_currentPage == UserControlCollection.Count - 1);
        ShowNextButton = (_currentPage < UserControlCollection.Count - 1);
        ShowPrevButton = (_currentPage > 0);
      }
    }

    public ObservableCollection<WizardPage> UserControlCollection { get; set; }

    public bool ShowFinishButton
    {
      get { return _showFinishButton; }
      set { SetProperty(ref _showFinishButton, value, "ShowFinishButton"); }
    }

    public bool ShowCancelButton
    {
      get { return _showCancelButton; }
      set { SetProperty(ref _showCancelButton, value, "ShowCancelButton"); }
    }

    public bool ShowNextButton
    {
      get { return _showNextButton; }
      set { SetProperty(ref _showNextButton, value, "ShowNextButton"); }
    }

    public bool ShowPrevButton
    {
      get { return _showPrevButton; }
      set { SetProperty(ref _showPrevButton, value, "ShowPrevButton"); }
    }

    public bool ShowHelpButton
    {
      get { return _showHelpButton; }
      set { SetProperty(ref _showHelpButton, value, "ShowHelpButton"); }
    }
    
    public bool EnableNextButton
    {
      get { return UserControlCollection[_currentPage].EnableNextButton; }
    }
    
    
    #endregion

    #region Commands
    /// <summary>
    /// 
    /// </summary>
    public ICommand NextPageCommand
    {
      get 
      {
        return _nextPageCommand ?? (_nextPageCommand = new RelayCommand(NextPage, param => CanNextPage));
      }
    }

    bool CanNextPage
    {
    	get { return (CurrentPage > -1) ? UserControlCollection[CurrentPage].EnableNextButton : true; }
    }

    void NextPage(object o)
    {
      if (CanNextPage)
      {
        if (CurrentPage < UserControlCollection.Count - 1) CurrentPage++;
      }
    }
    
    #endregion

    #region Private Methods
    private void WizardWindowLoaded(object sender, RoutedEventArgs e)
    {
      if ((UserControlCollection == null) || (UserControlCollection.Count == 0))
        throw new Exception("You must set the UserControlCollection variable with the set of UserControls in the wizard.");
      CurrentPage = 0;
    }
  
    private void HelpButtonClick(object sender, RoutedEventArgs e)
    {
      //ToDo
    }

    private void PrevButtonClick(object sender, RoutedEventArgs e)
    {
      if (CurrentPage > 0) CurrentPage--;
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void FinishButtonClick(object sender, RoutedEventArgs e)
    {
      _viewModel.Save(_viewModel.PluginPathName);
      Close();
    }
    
    #endregion

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, string propertyName)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return;

      field = value;
      OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged(string propertyName)
    {
      var handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion
  }
}
