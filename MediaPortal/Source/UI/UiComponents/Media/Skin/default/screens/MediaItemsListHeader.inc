<?xml version="1.0" encoding="utf-8"?>
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    VerticalAlignment="Bottom" HorizontalAlignment="Stretch" LastChildFill="false"
    IsVisible="{Binding IsItemsValid}"> <!-- Model = MediaModel -->
  <Label DockPanel.Dock="Left" Content="{Binding Source={DynamicResource ResourceKey=ItemsListTitle,TreeSearchMode=Hybrid},Path=Resource}"/>
  <StackPanel Margin="10,0,10,0" Orientation="Horizontal" DockPanel.Dock="Right">
    <Label Color="{ThemeResource HintTextColor}" Content="(" FontSize="{ThemeResource SmallerFontSize}"/>
    <Label Color="{ThemeResource HintTextColor}" Content="{Binding NumItemsStr}" FontSize="{ThemeResource SmallerFontSize}"/>
    <Label Color="{ThemeResource HintTextColor}" Content=")" FontSize="{ThemeResource SmallerFontSize}"/>
  </StackPanel>
</DockPanel>
