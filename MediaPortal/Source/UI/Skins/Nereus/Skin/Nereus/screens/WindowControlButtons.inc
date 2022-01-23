<?xml version="1.0" encoding="utf-8"?>
<Grid x:Name="WindowControlPanel"
      xmlns="www.team-mediaportal.com/2008/mpf/directx"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,0,5,0"
      >
  <Grid.Resources>
    <Model x:Key="ScreenModeModel" Id="6A232D95-9BDE-4005-905E-69CB092B012B"/>
    <Model x:Key="MouseModel" Id="CA6428A7-A6E2-4dd3-9661-F89CEBAF8E62"/>
  </Grid.Resources>

  <!-- Mouse control buttons -->
  <StackPanel Orientation="Horizontal"
              Context="{Service Window}"  IsVisible="{Binding Source={StaticResource MouseModel},Path=IsMouseUsed}">
    <Button x:Name="WindowedButton" Style="{ThemeResource MinMaxButtonStyle}" Margin="5,2,0,0" HorizontalAlignment="Right"
            Command="{Command Source={Service Window},Path=SwitchMode,Parameters=NormalWindowed}">
      <Button.IsVisible>
        <Binding Source="{StaticResource ScreenModeModel}" Path="CurrentScreenMode" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} == {ScreenMode}.{FullScreen}"/>
      </Button.IsVisible>
    </Button>
    <Button x:Name="MaximizedButton" Style="{ThemeResource MinMaxButtonStyle}" Margin="5,2,0,0" HorizontalAlignment="Right"
            Command="{Command Source={Service Window},Path=SwitchMode,Parameters=FullScreen}">
      <Button.IsVisible>
        <Binding Source="{StaticResource ScreenModeModel}" Path="CurrentScreenMode" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {ScreenMode}.{FullScreen}"/>
      </Button.IsVisible>
    </Button>
    <Button x:Name="ExitButton" Style="{ThemeResource ExitButtonStyle}" Margin="5,2,0,0" HorizontalAlignment="Right"
            Command="{Command Source={Service Window},Path=Shutdown}">
      <Button.IsVisible>
        <Binding Source="{StaticResource ScreenModeModel}" Path="CurrentScreenMode" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} == {ScreenMode}.{FullScreen}"/>
      </Button.IsVisible>
    </Button>
  </StackPanel>
</Grid>
