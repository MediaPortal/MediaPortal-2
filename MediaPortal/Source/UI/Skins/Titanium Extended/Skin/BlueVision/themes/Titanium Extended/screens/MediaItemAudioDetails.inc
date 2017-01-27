<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" MinHeight="630"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
  
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="400"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>

  <StackPanel Grid.Column="1" Margin="30">

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,0,0,5"
                      DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                             Maximum="10" VerticalAlignment="Center">
              <StarRatingPanel.Value>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}">
                  <Binding Path="TotalRating"/>
                </MultiBinding>
              </StarRatingPanel.Value>
              <StarRatingPanel.LayoutTransform>
                <TransformGroup>
                  <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                </TransformGroup>
              </StarRatingPanel.LayoutTransform>
            </StarRatingPanel>
            <Label x:Name="RatingCountLabel" Color="{ThemeResource EmphasisTextColor}" VerticalAlignment="Center" Margin="15,0,5,0"
                   IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
              </Label.Content>
            </Label>
          </StackPanel>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
    
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="200"/>
              <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Label x:Name="YearLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioYearLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Year" Grid.Row="0" Grid.Column="1"
                Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
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

            <Label x:Name="ArtistsLabel" Grid.Row="0" Grid.Column="0"
                Content="[Media.AudioArtistsLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Artists" Grid.Row="0" Grid.Column="1" Scroll="Auto"
                Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="AlbumLabel" Grid.Row="1" Grid.Column="0"
                Content="[Media.AudioAlbumLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Album" Grid.Row="1" Grid.Column="1" Scroll="Auto"
                Content="{Binding Album}" Color="{ThemeResource TextColor}"/>

            <Label x:Name="GenresLabel" Grid.Row="2" Grid.Column="0"
                Content="[Media.AudioGenresLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Genres" Grid.Row="2" Grid.Column="1" Scroll="Auto"
                   Content="{Binding Genres}" Color="{ThemeResource TextColor}">
              <Label.Content>
                <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                  <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                  <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                </MultiBinding>
              </Label.Content>
            </Label>

            <Label x:Name="DurationLabel" Grid.Row="3" Grid.Column="0"
                Content="[Media.AudioDurationLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Duration" Grid.Row="3" Grid.Column="1"
                Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}}" Color="{ThemeResource TextColor}"/>

              <Label x:Name="TrackLabel" Grid.Row="4" Grid.Column="0"
                     Content="[Media.AudioTrackLabel]" Color="{ThemeResource EmphasisTextColor}"/>
              <Label x:Name="Track" Grid.Row="4" Grid.Column="1"
                     Content="{Binding Path=Track}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
  </StackPanel>

  <Grid Grid.Column="1" Margin="10,0,-350,-40" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="TrackName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding TrackName}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                 HorizontalAlignment="Center"/>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
  </Grid>
  
  <Image Grid.Column="0" HorizontalAlignment="Stretch" VerticalAlignment="Top" Stretch="Fill" Margin="20,40,5,5"
      Width="380" Height="380" >
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" 
                          Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="music_filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>



</Grid>
