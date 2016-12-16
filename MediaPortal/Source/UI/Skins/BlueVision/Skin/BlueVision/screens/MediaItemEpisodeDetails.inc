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
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="420"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>
  
  <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                              Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Margin="-10,-15,140,0">
    <media:EpisodeAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Horizontal" VerticalAlignment="Stretch" HorizontalAlignment="Stretch"
                    DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
                 VerticalAlignment="Stretch">
            <Label.Content>
              <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>
          <Label Content=" – " Color="{ThemeResource TextColor}" FontFamily="DefaultBold"/>
          <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
                 VerticalAlignment="Stretch">
            <Label.Content>
              <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>
        </StackPanel>
      </ControlTemplate>
    </media:EpisodeAspectWrapper.Template>
  </media:EpisodeAspectWrapper>

  <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                            Grid.Column="1" Grid.Row="1" Margin="30,0,35,25" VerticalAlignment="Stretch"
                            HorizontalAlignment="Stretch">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Description" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Wrap="True" Opacity="0.6" TextTrimming="WordEllipsis" VerticalAlignment="Stretch"
               HorizontalAlignment="Stretch" Content="{Binding StoryPlot}" Color="{ThemeResource TextColor}"/>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>
          
  <StackPanel Grid.Column="0" Grid.Row="1" Margin="-10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <StackPanel Orientation="Horizontal">
      <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:MediaAspectWrapper.Template>
          <ControlTemplate>
              <Label x:Name="FirstAired" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
                     DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,10,0"
                     Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}" />
          </ControlTemplate>
        </media:MediaAspectWrapper.Template>
      </media:MediaAspectWrapper>
      <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:EpisodeAspectWrapper.Template>
          <ControlTemplate>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="10,6,0,5"
                        DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="5" VerticalAlignment="Center"
                               IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <StarRatingPanel.Value>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                    <Binding Path="TotalRating"/>
                  </MultiBinding>
                </StarRatingPanel.Value>
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Opacity="0.6" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>
          </ControlTemplate>
        </media:EpisodeAspectWrapper.Template>
      </media:EpisodeAspectWrapper>      
    </StackPanel>

    
        <StackPanel Orientation="Horizontal">
          <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
            <media:EpisodeAspectWrapper.Template>
              <ControlTemplate>
                <StackPanel Orientation="Horizontal">
                  <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" Content="[Media.SeasonRootViewName]" Opacity="0.6"
                         IsVisible="{Binding Path=Season,Converter={StaticResource ReferenceToBoolConverter}}"/>
                  <Label x:Name="SeasonNumber" Color="{ThemeResource TextColor}" Opacity="0.6"
                         IsVisible="{Binding Path=Season,Converter={StaticResource ReferenceToBoolConverter}}"
                         DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                    <Label.Content>
                      <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} {0} , "/>
                    </Label.Content>
                  </Label>
                  <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}" Content="[Media.EpisodeRootViewName]" Opacity="0.6"
                         IsVisible="{Binding Path=Episode,Converter={StaticResource ReferenceToBoolConverter}}"/>
                  <Label Color="{ThemeResource TextColor}" Content=" "/>
                  <Label x:Name="EpisodeNumber" Color="{ThemeResource TextColor}" Opacity="0.6"
                         DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                    <Label.Content>
                      <Binding Path="Episode"/>
                    </Label.Content>
                  </Label>
                </StackPanel>
              </ControlTemplate>
            </media:EpisodeAspectWrapper.Template>
          </media:EpisodeAspectWrapper>
        </StackPanel>

    <Label x:Name="Genre" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}" Opacity="0.6">
      <Label.Content>
        <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
          <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
          <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
        </MultiBinding>
      </Label.Content>
    </Label>

    <StackPanel Orientation="Horizontal">
      <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:VideoStreamAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal">
                <Label x:Name="Duration" Color="{ThemeResource TextColor}" Opacity="0.6"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                      <Binding Path="Duration"/>
                    </MultiBinding>
                  </Label.Content>
                </Label>
                <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label Content="[Media.Minutes]" Color="{ThemeResource TextColor}" Opacity="0.6"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:VideoStreamAspectWrapper.Template>
      </media:VideoStreamAspectWrapper>

      <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             HorizontalAlignment="Left" VerticalAlignment="Center">
        <media:UserDataWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  VerticalAlignment="Center" HorizontalAlignment="Left" Margin="15,5,0,5"
                  IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
              </Grid.ColumnDefinitions>
              <ProgressBar Height="12" Width="120" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,10,0"
                           Value="{Binding PlayPercentage}" Style="{ThemeResource PercentageProgressBarStyle}"/>
              <Label Grid.Column="1" Color="{ThemeResource TextColor}" Opacity="0.6" VerticalAlignment="Center"
                     FontSize="{ThemeResource SmallerFontSize}" Margin="0,0,0,5">
                <Label.Content>
                  <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                </Label.Content>
              </Label>
            </Grid>
          </ControlTemplate>
        </media:UserDataWrapper.Template>
      </media:UserDataWrapper>
    </StackPanel>
    
  </StackPanel>
  
</Grid>
