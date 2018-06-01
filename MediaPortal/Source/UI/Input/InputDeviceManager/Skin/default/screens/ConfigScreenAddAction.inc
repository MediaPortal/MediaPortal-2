<?xml version="1.0" encoding="utf-8"?>
<Grid IsVisible="{Binding ShowAddAction}"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="500"/>
      <ColumnDefinition Width="600"/>
    </Grid.ColumnDefinitions>
    
    <Label Content="[InputDeviceManager.Key]" Grid.Column="0" Grid.Row="0" Color="{ThemeResource TextColor}" FontSize="36" DockPanel.Dock="Top" HorizontalAlignment="Left"/>
    <ListView Style="{StaticResource ContentsMenuListViewStyle}" Grid.Column="0" Grid.Row="1" ItemsSource="{Binding Path=KeyItems,Mode=OneTime}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true">
      <ListView.Resources>
        <CommandBridge x:Key="Menu_Command" Command="{Binding Path=Command,Mode=OneTime}" />
      </ListView.Resources>
    </ListView>

    <Label Content="[InputDeviceManager.Screen]" Grid.Column="1" Grid.Row="0" Color="{ThemeResource TextColor}" FontSize="36" DockPanel.Dock="Top" HorizontalAlignment="Left"/>
    <ListView Style="{StaticResource ContentsMenuListViewStyle}" Grid.Column="1" Grid.Row="1" ItemsSource="{Binding Path=ScreenItems,Mode=OneTime}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="true">
      <ListView.Resources>
        <CommandBridge x:Key="Menu_Command" Command="{Binding Path=Command,Mode=OneTime}" />
      </ListView.Resources>
    </ListView>
  </Grid>
</Grid>
