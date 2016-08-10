<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="620"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Right" Stretch="Fill" Margin="0,10,10,0" Height="280" Width="190">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>

  <StackPanel Grid.Column="0" Grid.Row="0" Margin="0,0,10,0" VerticalAlignment="Stretch">
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold" Wrap="True"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Stretch">
            <Label.Content>
              <Binding Path="SeriesName"/>
            </Label.Content>
          </Label>
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
    
    <StackPanel Orientation="Horizontal">
      <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:SeriesAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Center">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,4,0,2">
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
                      <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                    </TransformGroup>
                  </StarRatingPanel.LayoutTransform>
                </StarRatingPanel>
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColor}" Margin="15,0,5,0" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Opacity="0.5"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:SeriesAspectWrapper.Template>
      </media:SeriesAspectWrapper>
    </StackPanel>
    
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <!--media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="Genre" Opacity="0.5" FontSize="{ThemeResource SmallFontSize}"
                   Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper-->

    <StackPanel Orientation="Vertical" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
      <StackPanel Orientation="Horizontal">
        <Label Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Content="{Binding AvailableSeasons}"
               Margin="0,0,8,0" Opacity="0.5"/>
        <Label Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Content="[Media.SeasonItemsHeader]"
               Margin="0,0,8,0" Opacity="0.5"/>
      </StackPanel>
      <StackPanel Orientation="Horizontal">
        <Label Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Content="{Binding AvailableEpisodes}"
               Margin="0,0,8,0" Opacity="0.5"/>
        <Label Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
               Margin="0,0,8,0" Opacity="0.5"/>
      </StackPanel>
    </StackPanel>

  </StackPanel>
  
  <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch"
                             Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="1" Margin="0,30,10,10">
    <media:SeriesAspectWrapper.Template>
      <ControlTemplate>
        <Grid VerticalAlignment="Stretch">
          <Grid.RowDefinitions>
            <RowDefinition Height="440"/>
          </Grid.RowDefinitions>
          <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="1"
                   Wrap="True" Opacity="0.5" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Margin="0,5,5,0"
                   VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Description}"
                   Color="{ThemeResource TextColor}"/>          
        </Grid>
      </ControlTemplate>
    </media:SeriesAspectWrapper.Template>
  </media:SeriesAspectWrapper>

     <!--media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
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
        </media:MediaAspectWrapper-->
      
</Grid>
