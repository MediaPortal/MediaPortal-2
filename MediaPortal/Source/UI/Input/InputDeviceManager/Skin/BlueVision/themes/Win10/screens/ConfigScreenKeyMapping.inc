<?xml version="1.0" encoding="utf-8"?>

<Grid IsVisible="{Binding ShowKeyMapping}" Margin="-488,0,0,0"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <Grid.Resources>
    <!-- Input device manager model -->
    <Model x:Key="Model" Id="CC11183C-01A9-4F96-AF90-FAA046981006"/> 
  </Grid.Resources>

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="407"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Button Grid.Column="0" x:Name="ShowConfigButton" Style="{ThemeResource MenuButtonWideStyle}" FontSize="{ThemeResource SmallFontSize}" VerticalAlignment="Top"
          Content="[InputDeviceManager.DefaultConfig.Title]" Command="{Command Source={StaticResource Model},Path=OpenDefaultConfigurationDialog}">
  </Button>
    
  <ListView Grid.Column="1" Style="{StaticResource InputManagerListViewStyle}" SetFocus="True" Margin="80,0,0,0"
            ItemsSource="{Binding Path=KeyItems,Mode=OneTime}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true"
            CurrentItem="{Binding Path=SelectedItem, Mode=TwoWay}">
    <ListView.Resources>
      <ResourceWrapper x:Key="VerticalScrollbarRenderTransform">
        <TranslateTransform X="40" />
      </ResourceWrapper>
      <CommandBridge x:Key="Menu_Command" Command="{Binding Path=Command,Mode=OneTime}"/>
    </ListView.Resources>
  </ListView>

  <!--Column 1 is "Name" and Column 2 is "KeyMap"-->
  <Grid Grid.Row="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

  </Grid>
</Grid>
