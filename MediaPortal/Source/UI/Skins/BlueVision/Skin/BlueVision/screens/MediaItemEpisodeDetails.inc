<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="420"/>
  </Grid.ColumnDefinitions>

  <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                              Grid.Column="0" Grid.Row="0" Margin="-10,-15,0,0">
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
                            Grid.Column="0" Grid.Row="1" Margin="-10,0,35,25">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <Label x:Name="Description" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Wrap="True" Opacity="1.3" TextTrimming="WordEllipsis"
                 HorizontalAlignment="Stretch" Content="{Binding StoryPlot}"
                 Color="{ThemeResource TextColor}"/>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>
          
  <StackPanel Grid.Column="1" Grid.Row="1" Margin="10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

        <StackPanel Orientation="Horizontal">
          <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
            <media:EpisodeAspectWrapper.Template>
              <ControlTemplate>
                <StackPanel Orientation="Horizontal">
                  <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}"  Content="[Media.SeasonRootViewName]"
                         IsVisible="{Binding Path=Season,Converter={StaticResource ReferenceToBoolConverter}}"/>
                  <Label x:Name="SeasonNumber" Color="{ThemeResource TextColor}" 
                         IsVisible="{Binding Path=Season,Converter={StaticResource ReferenceToBoolConverter}}"
                         DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                    <Label.Content>
                      <Binding Path="Season" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} {0} , "/>
                    </Label.Content>
                  </Label>
                  <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}" Content="[Media.EpisodeRootViewName]"
                         IsVisible="{Binding Path=Episode,Converter={StaticResource ReferenceToBoolConverter}}"/>
                  <Label Color="{ThemeResource TextColor}" Content=" "/>
                  <Label x:Name="EpisodeNumber" Color="{ThemeResource TextColor}"
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

    <media:GenreAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:GenreAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Genre" Content="{Binding Genre}" Color="{ThemeResource TextColor}" Margin="-10,0,0,0"
                   IsVisible="{Binding Path=Genre,Converter={StaticResource ReferenceToBoolConverter}}"/>
          </Grid>
        </ControlTemplate>
      </media:GenreAspectWrapper.Template>
    </media:GenreAspectWrapper>

    <DockPanel LastChildFill="False">
      <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                      DockPanel.Dock="Left">
        <media:VideoStreamAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal">
                <Label x:Name="Duration" Color="{ThemeResource TextColor}"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                      <Binding Path="Duration"/>
                    </MultiBinding>
                  </Label.Content>
                </Label>
                <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
                <Label Content="[Media.Minutes]" Color="{ThemeResource TextColor}"
                       IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:VideoStreamAspectWrapper.Template>
      </media:VideoStreamAspectWrapper>
      <Grid HorizontalAlignment="Right" VerticalAlignment="Center" DockPanel.Dock="Right">
        <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
          <media:MediaAspectWrapper.Template>
            <ControlTemplate>
              <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="37" Height="24" Margin="-5,0,15,0" >
                  <Image.Source>
                    <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                      <Binding Path="PlayCount"/>
                      <Binding Source=""/>
                      <Binding Source="unwatched_icon.png"/>
                    </MultiBinding>
                  </Image.Source>
                </Image>
              </Grid>
            </ControlTemplate>
          </media:MediaAspectWrapper.Template>
        </media:MediaAspectWrapper>
      </Grid>
    </DockPanel>

      <Grid HorizontalAlignment="Left" VerticalAlignment="Center">
        <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
          <media:EpisodeAspectWrapper.Template>
            <ControlTemplate>
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,6,0,5"
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
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Opacity="0.4" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,0">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </ControlTemplate>
          </media:EpisodeAspectWrapper.Template>
        </media:EpisodeAspectWrapper>
      </Grid>

  </StackPanel>
</Grid>
