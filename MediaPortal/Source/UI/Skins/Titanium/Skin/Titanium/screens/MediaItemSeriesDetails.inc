<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Grid.Column="1" Margin="10" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <media:EpisodeAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:EpisodeAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Label x:Name="SeriesLabel" Grid.Row="0" Grid.Column="0" Scroll="Auto"
                Content="[Media.SeriesTitleLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="SeriesName" Grid.Row="0" Grid.Column="1"
                Content="{Binding SeriesName}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="SeasonLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.SeriesSeasonLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Season" Grid.Row="1" Grid.Column="1"
                Content="{Binding Season}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="EpisodeLabel" Grid.Row="2" Grid.Column="0"
                Content="[Media.SeriesEpisodeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Episode" Grid.Row="2" Grid.Column="1"
                Content="{Binding Episode}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="EpisodeNameLabel" Grid.Row="3" Grid.Column="0" Scroll="Auto"
                Content="[Media.SeriesEpisodeNameLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="EpisodeName" Grid.Row="3" Grid.Column="1"
                Content="{Binding EpisodeName}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="RatingLabel" Grid.Row="4" Grid.Column="0"
                Content="[Media.Rating]" Color="{ThemeResource EmphasisTextColor}"/>

            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Row="4" Grid.Column="1">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                           Maximum="10"
                                           Value="{Binding TotalRating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.5" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>

              <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource TinyFontSize}" Margin="3,5,0,0">
                <Label.Content>
                  <Binding Path="RatingCount" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0})"/>
                </Label.Content>
              </Label>
            </StackPanel>

          </Grid>
        </ControlTemplate>
      </media:EpisodeAspectWrapper.Template>
    </media:EpisodeAspectWrapper>

    <Label Margin="0,10,0,0" x:Name="StoryPlot" Wrap="True" Scroll="Auto" ScrollSpeed="5" Height="350"
      HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
      Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
      Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>

  </StackPanel>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="5"
      Width="380" Height="560"
      FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
      Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;560}">
  </Image>
</Grid>
