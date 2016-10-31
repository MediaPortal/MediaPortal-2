<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  x:Name="MediaItemDetails" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

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

  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.Row="0" Margin="-10,-15,0,0">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontFamily="DefaultBold" Grid.Column="0" Grid.Row="0"
                 Content="{Binding SeriesName}" />
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>
  
  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.Row="1" Margin="-10,0,35,25">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1"
                 TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
                 Content="{Binding Description}" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

  <StackPanel Grid.Column="1" Grid.Row="1" Margin="10,0,0,0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <DockPanel DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" LastChildFill="False">
            <Label x:Name="Year" DockPanel.Dock="Left" HorizontalAlignment="Left" Margin="0,0,10,0" Color="{ThemeResource TextColor}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}"/>
            <Image DockPanel.Dock="Right" HorizontalAlignment="Right" VerticalAlignment="Center"
                   Stretch="Fill" Width="37" Height="24" Margin="10,0,10,0">
              <Image.Source>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                  <Binding Path="PlayCount"/>
                  <Binding Source=""/>
                  <Binding Source="unwatched_icon.png"/>
                </MultiBinding>
              </Image.Source>
            </Image>
          </DockPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <StackPanel Orientation="Vertical"
                DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <StackPanel Orientation="Horizontal">
        <Label Content="{Binding Seasons}" Margin="0,0,8,0" Color="{ThemeResource TextColor}" />
        <Label Content="[Media.SeasonItemsHeader]" Margin="0,0,8,0" Color="{ThemeResource TextColor}"
               IsVisible="{Binding Path=Seasons,Converter={StaticResource ReferenceToBoolConverter}}"/>
      </StackPanel>
      <StackPanel Orientation="Horizontal">
        <Label Content="{Binding Episodes}" Margin="0,0,8,0" Color="{ThemeResource TextColor}" />
        <Label Content="[Media.EpisodeItemsHeader]" Margin="0,0,8,0" Color="{ThemeResource TextColor}"
               IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"/>
      </StackPanel>
    </StackPanel>

    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <StackPanel Orientation="Horizontal"  Margin="0,6,0,5">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                           Maximum="10"
                                           Value="{Binding TotalRating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>

  </StackPanel>
</Grid>
