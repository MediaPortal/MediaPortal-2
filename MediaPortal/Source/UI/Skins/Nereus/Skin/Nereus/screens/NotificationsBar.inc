<?xml version="1.0" encoding="utf-8"?>
<StackPanel x:Name="NotificationsPanel"
            xmlns="www.team-mediaportal.com/2008/mpf/directx"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:mp_special_controls="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Controls;assembly=SkinEngine"
            Orientation="Horizontal" VerticalAlignment="Center"
            >

  <StackPanel.Resources>
    <Model x:Key="MouseModel" Id="CA6428A7-A6E2-4dd3-9661-F89CEBAF8E62"/>
    <Model x:Key="StateModel" Id="B23D8DC1-405E-4564-92D0-E247C299FFAE"/>
  </StackPanel.Resources>

  <StackPanel Orientation="Horizontal" Context="{Service Window}" IsVisible="{Binding Source={StaticResource MouseModel},Path=IsMouseUsed}" VerticalAlignment="Center">
    <Control Style="{ThemeResource NotificationControlStyle}" Margin="5,2,5,5" IsVisible="{Binding Source={StaticResource NotificationsModel},Path=IsNotificationsHintVisible}"
             HorizontalAlignment="Left" VerticalAlignment="Center"/>
  </StackPanel>

  <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="0,0,5,0"
              DataContext="{Binding Source={StaticResource StateModel}, Path=[C37B62D0-8E87-42A4-877E-B41944DA9FC9]}"
              IsVisible="{Binding Converter={StaticResource ReferenceToBoolConverter}}">
    <Label FontSize="{ThemeResource SmallerFontSize}" Margin="10,0,0,0"
           Color="{ThemeResource TextColor}" VerticalAlignment="Center" HorizontalAlignment="Right">
      <Label.IsVisible>
        <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}>=0">
          <Binding Path="Progress"/>
        </MultiBinding>
      </Label.IsVisible>
      <Label.Content>
        <Binding Path="Progress" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}%"/>
      </Label.Content>
    </Label>
    <Button Style="{ThemeResource ImportingButtonStyle}" IsVisible="{Binding IsImporting}"/>
  </StackPanel>

  <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="0,0,5,0"
              DataContext="{Binding Source={StaticResource StateModel}, Path=[2A58935C-3363-4FA1-B48D-1EF0E81F830D]}"
              IsVisible="{Binding Converter={StaticResource ReferenceToBoolConverter}}">
    <Button Style="{ThemeResource RecordingButtonStyle}" IsVisible="{Binding IsRecording}"/>
  </StackPanel>

  <mp_special_controls:PlayerControl Style="{ThemeResource CPPlayerControlStyle}"
                       VerticalAlignment="Center" AutoVisibility="True" HorizontalAlignment="Left"/>
</StackPanel>
