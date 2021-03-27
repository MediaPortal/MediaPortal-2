<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  
  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch"
                             Grid.Column="0" Grid.Row="1" Margin="0,40,10,50">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="MediaItemDescription" Wrap="True" TextTrimming="WordEllipsis" Opacity="0.6" Height="260"
               FontSize="32" Color="{ThemeResource TextColor}" HorizontalContentAlignment="Right"
               Content="{Binding Description}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="60,4,40,2"/>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="1" Margin="10,0,10,0">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment="Top">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" Wrap="True" FontFamily="SegoeUIBold"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" TextTrimming="WordEllipsis">
            <Label.Content>
              <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>

          <StackPanel Orientation="Horizontal">
            <Label Content="[Media.SeasonRootViewName]" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
            <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Margin="7,0,0,0"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Content="{Binding Season}"/>
          </StackPanel>

          <Grid DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
            <Image Source="HomeContentIcons\series.png" Height="25" Width="25" Stretch="Uniform" Margin="2,-1,5,1"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
              <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Episodes}"
                     IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                     Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
                     IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                     Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
            </StackPanel>
          </Grid>

          <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Color="{ThemeResource TextColorForeground}">
            <Label.Content>
              <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
              </MultiBinding>
            </Label.Content>
          </Label>

        </StackPanel>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

</Grid>
