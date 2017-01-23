<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                              Grid.Column="1" Margin="20,30,20,45" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
    <media:EpisodeAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="190"/>
            <ColumnDefinition Width="500"/>
          </Grid.ColumnDefinitions>

          <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                 Grid.Column="1" Grid.Row="0" Grid.RowSpan="3" HorizontalAlignment="Right" VerticalAlignment="Top">
            <media:UserDataWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                      IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,0,0,0" >
                    <Image.Source>
                      <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                    </Image.Source>
                  </Image>
                </Grid>
              </ControlTemplate>
            </media:UserDataWrapper.Template>
          </media:UserDataWrapper>

          <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2">
            <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                             Maximum="10" Value="{Binding TotalRating}">
              <StarRatingPanel.LayoutTransform>
                <TransformGroup>
                  <ScaleTransform ScaleX="1.65" ScaleY="1.5"/>
                </TransformGroup>
              </StarRatingPanel.LayoutTransform>
            </StarRatingPanel>

            <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}" FontSize="{ThemeResource NormalFontSize}" Margin="8,0,0,0">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0})"/>
              </Label.Content>
            </Label>
          </StackPanel>

          <Label x:Name="FirstAiredLabel" Grid.Row="1" Grid.Column="0"
                 Content="[Media.FirstAiredLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                    Grid.Row="1" Grid.Column="1">
            <media:MediaAspectWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                  <Label x:Name="FirstAired" Color="{ThemeResource TextColor}"
                         Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}"/>
                </Grid>
              </ControlTemplate>
            </media:MediaAspectWrapper.Template>
          </media:MediaAspectWrapper>

          <Label x:Name="SeasonLabel" Grid.Row="2" Grid.Column="0"
              Content="[Media.SeriesSeasonLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <Label x:Name="Season" Grid.Row="2" Grid.Column="1"
              Content="{Binding Season}" Color="{ThemeResource TextColor}"/>

          <Label x:Name="EpisodeLabel" Grid.Row="3" Grid.Column="0"
              Content="[Media.SeriesEpisodeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <Label x:Name="Episode" Grid.Row="3" Grid.Column="1"
              Content="{Binding Episode}" Color="{ThemeResource TextColor}"/>

          <Label x:Name="GenreLabel" Grid.Row="4" Grid.Column="0"
                 Content="[Media.VideoGenreLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <Label x:Name="Genre" TextTrimming="WordEllipsis" Color="{ThemeResource TextColor}"
                 Grid.Row="4" Grid.Column="1">
            <Label.Content>
              <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
              </MultiBinding>
            </Label.Content>
          </Label>

          <Label x:Name="DurationLabel" Grid.Row="5" Grid.Column="0"
                 Content="[Media.VideoDurationLabel]" Color="{ThemeResource EmphasisTextColor}"/>
          <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                          Grid.Row="5" Grid.Column="1">
            <media:VideoStreamAspectWrapper.Template>
              <ControlTemplate>
                <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                  <Label x:Name="Duration" FontSize="{ThemeResource NormalFontSize}"
                         Color="{ThemeResource TextColor}" IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                    <Label.Content>
                      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                        <Binding Path="Duration"/>
                      </MultiBinding>
                    </Label.Content>
                  </Label>
                  <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
                  <Label Content="[Media.Minutes]" FontSize="{ThemeResource NormalFontSize}" Color="{ThemeResource TextColor}"
                         IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
                </StackPanel>
              </ControlTemplate>
            </media:VideoStreamAspectWrapper.Template>
          </media:VideoStreamAspectWrapper>

          <Label x:Name="StoryPlot" Wrap="True" Grid.Row="6" Grid.ColumnSpan="2" Margin="0,30,0,0"
                 HorizontalAlignment="Stretch" VerticalAlignment="Stretch" TextTrimming="WordEllipsis"
                 Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
                 Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}"/>
        </Grid>
      </ControlTemplate>
    </media:EpisodeAspectWrapper.Template>
  </media:EpisodeAspectWrapper>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:EpisodeAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="SeriesName" 
                  Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                  HorizontalAlignment="Center"/>
          </StackPanel>
        </ControlTemplate>
      </media:EpisodeAspectWrapper.Template>
    </media:EpisodeAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30"
      Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560}"
                          FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
