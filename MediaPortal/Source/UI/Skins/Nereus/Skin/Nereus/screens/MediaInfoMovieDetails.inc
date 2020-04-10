<?xml version="1.0" encoding="utf-8"?>
<Grid
  Margin="40,10,20,10"
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  xmlns:mp_special_controls="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Controls;assembly=SkinEngine">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="600"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Row="1" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Bottom" Width="294" Height="428" Margin="0,-30,10,30">
    <Image HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Fill" Width="294" Height="428">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="Poster_large.png"/>
      </Image.Source>
    </Image>
    <Image x:Name="MoviePoster" HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Fill" Margin="8" Width="278" Height="412">
      <Image.Source>
        <fanart:FanArtImageSource fanart:FanArtMediaType="Movie" fanart:FanArtType="Poster" fanart:FanArtName="{Binding MediaItem.MediaItemId}"
                                  fanart:MaxWidth="{StaticResource FANART_POSTER_WIDTH}" fanart:MaxHeight="{StaticResource FANART_POSTER_HEIGHT}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
           Source="video.png" IsVisible="{Binding ElementName=MoviePoster, Path=!HasImage}"/>
  </Grid>

  <media:MovieAspectWrapper MediaItem="{Binding MediaItem}" Grid.Column="1"
                            IsVisible="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].EnableAgeCertificationLogos}">
    <media:MovieAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              IsVisible="{Binding Path=Certification,Converter={StaticResource ReferenceToBoolConverter}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
          </Grid.ColumnDefinitions>
          <Image HorizontalAlignment="Right" VerticalAlignment="Bottom" Stretch="Uniform" Height="80" MaxWidth="150" Margin="0,-10,15,10">
            <Image.Source>
              <ImageSourceWrapper Delay="0" DelayInOut="false" VerticalTextureAlignment="Bottom" HorizontalTextureAlignment="Right">
                <ImageSourceWrapper.Source>
                  <Binding Path="Certification" Converter="{StaticResource SafeFilenameConverter}" ConverterParameter="{}Certification\{0}.png"/>
                </ImageSourceWrapper.Source>
              </ImageSourceWrapper>
            </Image.Source>
          </Image>
        </Grid>
      </ControlTemplate>
    </media:MovieAspectWrapper.Template>
  </media:MovieAspectWrapper>

  <media:VideoAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Stretch" Grid.Row="1">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="StoryPlot" Wrap="True" Margin="0,40,10,20" TextTrimming="WordEllipsis" Color="{ThemeResource MenuTextBackground}"
                 HorizontalAlignment="Stretch" VerticalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}" Height="370"
                 Content="{Binding StoryPlot}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"/>
        </Grid>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>

  <StackPanel Grid.Row="0" Margin="0,0,-70,0" VerticalAlignment="Stretch" Orientation="Vertical">

    <media:MovieAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Stretch">
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label x:Name="MediaItemName" Wrap="True" TextTrimming="WordEllipsis"
                   VerticalAlignment="Stretch" FontSize="{ThemeResource NormalFontSize}" FontFamily="SegoeUIBold"
                   Content="{Binding MovieName}" Color="{ThemeResource TextColor}"/>
          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>
    
    <StackPanel Orientation="Horizontal">
      <media:MovieAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Center">
        <media:MovieAspectWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,3,0,3">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                 Maximum="5" VerticalAlignment="Center" IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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
                <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColorForeground}" Margin="15,2,5,1" 
                       FontSize="{ThemeResource SmallerFontSize}" VerticalAlignment="Center"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </Grid>
          </ControlTemplate>
        </media:MovieAspectWrapper.Template>
      </media:MovieAspectWrapper>      
    </StackPanel>

    <media:MediaAspectWrapper MediaItem="{Binding MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}" 
                   Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center" Margin="40,0,0,0"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" />
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
      <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
             VerticalAlignment="Center" HorizontalAlignment="Left"/>
      <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="40,0,0,0"
             Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center">
        <Label.Content>
          <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
            <Binding Path="MediaItem"/>
            <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
          </MultiBinding>
        </Label.Content>
      </Label>
    </Grid>

    <media:MovieAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Center" >
      <media:MovieAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" 
                IsVisible="{Binding ElementName=Duration, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\duration.png" Height="26" Width="26" Stretch="Uniform" Margin="3,-1,4,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal">
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                     IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}"
                     Content="{Binding Path=Runtime}" VerticalAlignment="Center" Margin="40,0,0,0"/>
              <Label Content=" " IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}"
                     IsVisible="{Binding Path=Runtime,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:MovieAspectWrapper.Template>
    </media:MovieAspectWrapper>

    <StackPanel Orientation="Horizontal">
      <media:UserDataWrapper MediaItem="{Binding MediaItem}">
        <media:UserDataWrapper.Template>
          <ControlTemplate>
            <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                  IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
              <Image VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,40,0" >
                <Image.Source>
                  <Binding Path="PlayPercentage" Converter="{StaticResource RoundingFormatConverter}" ConverterParameter="{}5;CircularBar\{0}.png"/>
                </Image.Source>
              </Image>
              <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="20" Color="White" FontFamily="SegoeUIBold"
                     Width="95" Height="95" Margin="0,0,40,0" HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                <Label.Content>
                  <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                </Label.Content>
              </Label>
            </Grid>
          </ControlTemplate>
        </media:UserDataWrapper.Template>
      </media:UserDataWrapper>
      <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,-280,-40">
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="Movie" fanart:FanArtType="ClearArt" fanart:FanArtName="{Binding MediaItem.MediaItemId}"
                                    fanart:MaxWidth="240" fanart:MaxHeight="140"/>
        </Image.Source>
      </Image>
    </StackPanel>

  </StackPanel>

  <Button x:Name="PlayButton" Grid.Column="1" Margin="0,-60,50,0" Command="{Command Play}">
    <Image HorizontalAlignment="Right" VerticalAlignment="Top" Stretch="Uniform" Width="190" Height="190"
          Source="playback.png" Opacity="0.8" />
  </Button>
</Grid>
