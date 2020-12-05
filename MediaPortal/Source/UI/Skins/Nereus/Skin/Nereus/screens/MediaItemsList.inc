<?xml version="1.0" encoding="utf-8"?>
<Grid MinHeight="510"
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    > <!-- Model = MediaNavigationModel -->
  <Grid.Resources>
    <Model x:Key="MediaModel" Id="4CDD601F-E280-43b9-AD0A-6D7B2403C856"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <Include Source="screens\MediaItemsFilterKeyBindings.inc"/>

    <ListView x:Name="MediaItemsList" Style="{ThemeResource MediaItemsListListViewStyle}" ItemsSource="{Binding Items}"
              HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" RestoreFocus="True"
              GroupingValueProvider="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData}">
      <ListView.Resources>
        <ResourceWrapper x:Key="VerticalScrollbarRenderTransform">
          <TranslateTransform X="0" />
          <TranslateTransform Y="0" />
        </ResourceWrapper>
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
    </ListView>

    <DockPanel DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
               VerticalAlignment="Bottom" HorizontalAlignment="Right" LastChildFill="false" IsVisible="{Binding IsItemsValid}">
      <!---Grid+ListView-->
      <Grid DockPanel.Dock="Right" Margin="120,-20,-125,40" Width="100">
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" >
          <Label VerticalAlignment="Center" Margin="0,0,10,0"
                 Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}">
            <Label.Content>
              <MultiBinding Converter="{StaticResource GetIndexMultiConverter}" ConverterParameter="1">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="SelectedItem"/>
                <Binding Source="{Binding ElementName=MediaItemsList}" Path="ItemsSource"/>
              </MultiBinding>
            </Label.Content>
          </Label>
          <Label Color="{ThemeResource TextColorForeground}" Content="| " FontSize="25" VerticalAlignment="Center" Margin="0,-3,0,3"/>
          <Label Color="{ThemeResource TextColorForeground}" Content="{Binding NumItems}"
                 FontSize="{ThemeResource SmallerFontSize}" VerticalAlignment="Center"/>
          <StackPanel.IsVisible>
            <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"
                     Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
          </StackPanel.IsVisible>
        </StackPanel>
      </Grid>
    </DockPanel>
    
    <!---CoverView-->
    <StackPanel HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="-300,30,200,-30" Orientation="Horizontal" Width="100"
                DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}">
      <Label HorizontalAlignment="Center" VerticalAlignment="Center" Margin="0,0,10,0"
             Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallerFontSize}">
        <Label.Content>
          <MultiBinding Converter="{StaticResource GetIndexMultiConverter}" ConverterParameter="1">
            <Binding Source="{StaticResource FanArtBackgroundModel}" Path="SelectedItem"/>
            <Binding Source="{Binding ElementName=MediaItemsList}" Path="ItemsSource"/>
          </MultiBinding>
        </Label.Content>
      </Label>
      <Label Content="| " FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColorForeground}"/>
      <Label Content="{Binding NumItems}" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColorForeground}"/>
      <StackPanel.IsVisible>
        <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"
                 Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} == {LayoutType}.{CoverLayout}"/>
      </StackPanel.IsVisible>
    </StackPanel>

    <Label IsVisible="{Binding ShowListHint}"
       HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
       Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
    
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
         HorizontalAlignment="Center" VerticalAlignment="Center"
         Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
  <KeyBinding Key="F5" Command="{Command Reload}"/>
</Grid>
