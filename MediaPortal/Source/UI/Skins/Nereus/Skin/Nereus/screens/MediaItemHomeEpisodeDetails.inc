<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="Auto"/>
  </Grid.ColumnDefinitions>
  
   <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,5,20,-5">
    <media:EpisodeAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="Auto"/>
            <ColumnDefinition Width="Auto"/>
          </Grid.ColumnDefinitions>
          <Label x:Name="EpisodeName" HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallerFontSize}"
                 Content="{Binding Path=EpisodeName}" Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis"/>
          <Label x:Name="SeriesName" HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallerFontSize}" Grid.Column="1"
                 Color="{ThemeResource TextColor}" TextTrimming="WordEllipsis" Opacity="0.6" Margin="10,0,0,0">
            <Label.Content>
              <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0})"/>
            </Label.Content>
          </Label>
        </Grid>
      </ControlTemplate>
    </media:EpisodeAspectWrapper.Template>
  </media:EpisodeAspectWrapper>

  <StackPanel Orientation="Horizontal" Grid.Row="1" Margin="-15,0,0,0">

    <Grid DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}" Margin="15,0,15,0">
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>

      <Image Source="HomeContentIcons\series.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"/>
      <StackPanel Orientation="Horizontal" Grid.Column="1">
        <Label FontSize="{ThemeResource SmallerFontSize}" Content="S"
               IsVisible="{Binding Path=Season,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="5,0,1,0" Opacity="0.6" Color="{ThemeResource TextColor}"/>
        <Label FontSize="{ThemeResource SmallerFontSize}" Content="{Binding Season}"
               Margin="0,0,10,0" Opacity="0.6" Color="{ThemeResource TextColor}"/>
        <Label FontSize="{ThemeResource SmallerFontSize}" Content="E"
               IsVisible="{Binding Path=EpisodeNumber,Converter={StaticResource ReferenceToBoolConverter}}"
               Margin="0,0,1,0" Opacity="0.6" Color="{ThemeResource TextColor}"/>
        <Label FontSize="{ThemeResource SmallerFontSize}" Content="{Binding EpisodeNumber}"
               Margin="0,0,10,0" Opacity="0.6" Color="{ThemeResource TextColor}"/>
      </StackPanel>
    </Grid>
    
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\year.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="0,-1,0,1"/>
            <Label x:Name="Year" Grid.Column="1" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}" Margin="5,0,0,0" Opacity="0.6"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:EpisodeAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\Star.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" Margin="30,-1,0,1"/>
            <Label x:Name="RatingCountLabel" Margin="5,0,0,0" VerticalAlignment="Center" Grid.Column="1"
                   FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F1}"/>
              </Label.Content>
            </Label>
          </Grid>
        </ControlTemplate>
      </media:EpisodeAspectWrapper.Template>
    </media:EpisodeAspectWrapper>

    <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\duration.png" Stretch="Uniform" Height="25" VerticalAlignment="Center" 
                   IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Margin="30,-1,0,1"/>
            <Label x:Name="Duration" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" Opacity="0.6"
                   IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Margin="5,0,0,0"
                   Grid.Column="1" VerticalAlignment="Center">
              <Label.Content>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                  <Binding Path="Duration"/>
                </MultiBinding>
              </Label.Content>
            </Label>  
            <Label Content=" " IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"
                   FontSize="{ThemeResource SmallerFontSize}" Grid.Column="2" VerticalAlignment="Center"/>
            <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}" VerticalAlignment="Center"
                   IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Grid.Column="3" Opacity="0.6"/>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>

    <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:UserDataWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image Source="HomeContentIcons\watch.png" Stretch="Uniform" Height="35" VerticalAlignment="Center" Margin="30,-1,0,1"/>
            <Label VerticalAlignment="Center" FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColor}"
                   Grid.Column="1" Margin="5,0,0,0" Opacity="0.6">
              <Label.Content>
                <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
              </Label.Content>
            </Label>
          </Grid>
        </ControlTemplate>
      </media:UserDataWrapper.Template>
    </media:UserDataWrapper>
  </StackPanel>

</Grid>

