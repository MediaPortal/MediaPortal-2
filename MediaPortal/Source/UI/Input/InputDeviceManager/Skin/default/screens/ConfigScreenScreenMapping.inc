<?xml version="1.0" encoding="utf-8"?>
<Grid IsVisible="{Binding ShowKeyMapping}"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <!--Column 1 is "Name" and Column 2 is "KeyMap"-->
  <ListView Grid.Row="0" Style="{StaticResource InputManagerListViewStyle}"
            ItemsSource="{Binding Path=ScreenItems,Mode=OneTime}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true"
            CurrentItem="{Binding Path=SelectedItem, Mode=TwoWay}">
    <ListView.Resources>
      <CommandBridge x:Key="Menu_Command" Command="{Binding Path=Command,Mode=OneTime}"/>
    </ListView.Resources>
  </ListView>
  
  <Grid Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

  </Grid>
</Grid>
