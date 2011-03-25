<?xml version="1.0" encoding="utf-8"?>
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    VerticalAlignment="Bottom" HorizontalAlignment="Stretch" LastChildFill="false"
    IsVisible="{Binding IsItemsValid}"> <!-- Model = MediaNavigationModel -->
<!--  <Label DockPanel.Dock="Left" Content="{Binding Source={DynamicResource ResourceKey=ItemsListTitle,TreeSearchMode=Hybrid},Path=Resource}"/> -->
  <Label Color="{ThemeResource HintTextColor}" Content="{Binding NumItemsStr}" FontSize="{ThemeResource SmallerFontSize}"
      Margin="10,0,10,0" DockPanel.Dock="Right"/>
</DockPanel>
