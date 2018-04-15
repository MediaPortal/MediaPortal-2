<?xml version="1.0" encoding="utf-8"?>
<!-- Include file for the window buttons in FSC screens -->
<DockPanel
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    LastChildFill="False" DataContext="{Binding RelativeSource={RelativeSource Self},Path=Screen}">
  <DockPanel.Resources>
    <Model x:Key="MouseModel" Id="CA6428A7-A6E2-4dd3-9661-F89CEBAF8E62"/>
  </DockPanel.Resources>
  <DockPanel.IsVisible>
    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} &amp;&amp; {1}">
      <Binding Path="HasInputFocus"/>
      <Binding Source="{StaticResource MouseModel}" Path="IsMouseUsed"/>
    </MultiBinding>
  </DockPanel.IsVisible>
  <Grid x:Name="WindowButtonsPanel" DockPanel.Dock="Right" VerticalAlignment="Top">
    <Grid.Triggers>
      <Trigger Property="IsMouseOver" Value="False">
        <Trigger.Setters>
          <Setter Property="OpacityMask" Value="#B0000000"/>
        </Trigger.Setters>
      </Trigger>
    </Grid.Triggers>
    <StackPanel Margin="10" Orientation="Horizontal" Context="{Service Window}">   
      <Button x:Name="BackButton" Style="{ThemeResource NavigateBackButtonStyle}" Margin="3"
          HorizontalAlignment="Left"
          Command="{Command Source={Service WorkflowManager},Path=NavigatePop,Parameters=1}"/>
      <Button x:Name="PowerButton" Style="{ThemeResource PowerButtonStyle}" Margin="3"
          HorizontalAlignment="Left" Command="{Command Source={Service WorkflowManager},Path=NavigatePush,Parameters=BBFA7DB7-5055-48D5-A904-0F0C79849369}"/>
    </StackPanel>
  </Grid> <!-- WindowButtonsPanel -->
</DockPanel>