<?xml version="1.0" encoding="utf-8"?>
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    > <!-- Model = MediaNavigationModel -->

  <KeyBinding Key="F5" Command="{Command Reload}"/>

  <!-- Keys for secondary filter-->
  <KeyBinding Key="0" Command="{Command Path=ApplySecondaryFilter,Parameters=0}"/>
  <KeyBinding Key="1" Command="{Command Path=ApplySecondaryFilter,Parameters=1}"/>
  <KeyBinding Key="2" Command="{Command Path=ApplySecondaryFilter,Parameters=2}"/>
  <KeyBinding Key="3" Command="{Command Path=ApplySecondaryFilter,Parameters=3}"/>
  <KeyBinding Key="4" Command="{Command Path=ApplySecondaryFilter,Parameters=4}"/>
  <KeyBinding Key="5" Command="{Command Path=ApplySecondaryFilter,Parameters=5}"/>
  <KeyBinding Key="6" Command="{Command Path=ApplySecondaryFilter,Parameters=6}"/>
  <KeyBinding Key="7" Command="{Command Path=ApplySecondaryFilter,Parameters=7}"/>
  <KeyBinding Key="8" Command="{Command Path=ApplySecondaryFilter,Parameters=8}"/>
  <KeyBinding Key="9" Command="{Command Path=ApplySecondaryFilter,Parameters=9}"/>
  <KeyBinding Key="*" Command="{Command Path=ApplySecondaryFilter,Parameters=*}"/>
  <KeyBinding Key="(" Command="{Command Path=ApplySecondaryFilter,Parameters=(}"/>
  <KeyBinding Key="#" Command="{Command Path=ApplySecondaryFilter,Parameters=#}"/>
  <KeyBinding Key="§" Command="{Command Path=ApplySecondaryFilter,Parameters=§}"/>

  <KeyBinding Key="Escape" Command="{Command Path=ApplySecondaryFilter,Parameters=*}" IsEnabled="{Binding IsFiltered}"/>

</Grid>
