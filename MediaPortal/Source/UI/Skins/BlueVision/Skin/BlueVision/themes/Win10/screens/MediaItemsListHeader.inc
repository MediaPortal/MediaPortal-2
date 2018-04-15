<?xml version="1.0" encoding="utf-8"?>
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    VerticalAlignment="Center" HorizontalAlignment="Stretch" LastChildFill="false" Height="30"
    IsVisible="{Binding IsItemsValid}"> <!-- Model = MediaNavigationModel -->
  <!--Label Color="{ThemeResource TextColor}" Content="{Binding NumItemsStr}" FontSize="{ThemeResource SmallFontSize}"
          DockPanel.Dock="Left" FontFamily="DefaultBold"/-->
</DockPanel>
