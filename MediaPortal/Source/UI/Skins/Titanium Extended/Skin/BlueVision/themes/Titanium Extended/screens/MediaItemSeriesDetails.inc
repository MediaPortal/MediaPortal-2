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

    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                               Grid.Column="1" Margin="20,30,20,35" HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
            <Grid.RowDefinitions>
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
            
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2"
                        DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                               Maximum="10" Value="{Binding TotalRating}">
                <StarRatingPanel.LayoutTransform>
                  <TransformGroup>
                    <ScaleTransform ScaleX="1.65" ScaleY="1.5"/>
                  </TransformGroup>
                </StarRatingPanel.LayoutTransform>
              </StarRatingPanel>

              <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}" FontSize="{ThemeResource NormalFontSize}" Margin="8,0,0,0"
                     IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                </Label.Content>
              </Label>
            </StackPanel>

            <Label x:Name="YearLabel" Grid.Row="1" Grid.Column="0"
                   Content="[Media.VideoYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <media:MediaAspectWrapper Grid.Row="1" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
              <media:MediaAspectWrapper.Template>
                <ControlTemplate>
                  <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                    <Grid.RowDefinitions>
                      <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                      <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource NormalFontSize}"
                           Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
                  </Grid>
                </ControlTemplate>
              </media:MediaAspectWrapper.Template>
            </media:MediaAspectWrapper>

            <StackPanel Orientation="Horizontal" Grid.Row="2" Grid.Column="0">
              <Label x:Name="SeasonLabel" Content="[Media.SeasonItemsHeader]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label Content=":" Color="{ThemeResource EmphasisTextColor}"/>
            </StackPanel>
            <Label x:Name="Season" Grid.Row="2" Grid.Column="1" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                   Content="{Binding Seasons}" Color="{ThemeResource TextColor}"/>

            <StackPanel Orientation="Horizontal" Grid.Row="3" Grid.Column="0">
              <Label x:Name="EpisodeLabel" Content="[Media.EpisodeItemsHeader]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label Content=":" Color="{ThemeResource EmphasisTextColor}"/>
            </StackPanel>
            <Label x:Name="Episode" Grid.Row="3" Grid.Column="1" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}"
                Content="{Binding Episodes}" Color="{ThemeResource TextColor}"/>

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
            
            <Label x:Name="MediaItemDescription" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="5" Grid.ColumnSpan="2"
                     Wrap="True" TextTrimming="WordEllipsis" Margin="0,30,0,0" Color="{ThemeResource TextColor}"
                     VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Content="{Binding Description}"/>
          </Grid>
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="SeriesName" 
                  Content="{Binding SeriesName}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                  HorizontalAlignment="Center"/>
          </StackPanel>
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
  </Grid>

  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30"
      Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
