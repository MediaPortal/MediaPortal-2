<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
>

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>

          <StackPanel Orientation="Horizontal">
            <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold">
              <Label.Content>
                <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
              </Label.Content>
            </Label>

            <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} - S{0} E"/>
              </Label.Content>
            </Label>

            <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="Episode"/>
              </Label.Content>
            </Label>

            <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}">
              <Label.Content>
                <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}: {0}"/>
              </Label.Content>
            </Label>

          </StackPanel>

          <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1" Grid.ColumnSpan="3"
                Scroll="Auto" ScrollSpeed="5" ScrollDelay="5"
                HorizontalAlignment="Stretch"
                Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
                Color="{ThemeResource TextColor}"/>

        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

</Grid>
