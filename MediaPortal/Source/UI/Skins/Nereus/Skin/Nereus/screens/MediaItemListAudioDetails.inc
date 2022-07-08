<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,200,20,-10"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <MultiAspectAttributeConverter x:Key="MultiAspectAttributeConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Height="456" Width="456" HorizontalAlignment="Right" VerticalAlignment="Bottom">
    <Image Stretch="Fill" Margin="-230,0,230,0" Width="420" Height="420" HorizontalAlignment="Right" VerticalAlignment="Center">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" VerticalTextureAlignment="Center"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=DiscArt;420;420}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Margin="0" Width="456" Height="456">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="Square_large.png"/>
      </Image.Source>
    </Image>
    <Image x:Name="AlbumCover" HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Margin="8" Width="440" Height="440">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
           Source="audio.png" IsVisible="{Binding ElementName=AlbumCover, Path=!HasImage}"/>
  </Grid>

  <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
    <media:AudioAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0">

          <Label x:Name="Title" FontSize="{ThemeResource NormalFontSize}" Wrap="True" TextTrimming="WordEllipsis"
                 Content="{Binding TrackName}" Color="{ThemeResource TextColor}" FontFamily="Noto-Bold"/>

          <Label x:Name="Artists" FontSize="{ThemeResource SmallFontSize}"
                 Content="{Binding Artists}" Color="{ThemeResource TextColor}"/>

          <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,3,0,3"
                      IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
            <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                             Maximum="5" VerticalAlignment="Center">
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
            <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"
                   FontSize="{ThemeResource SmallerFontSize}" Margin="15,0,5,1"
                   IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
              <Label.Content>
                <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
              </Label.Content>
            </Label>
          </StackPanel>

          <Grid Margin="0,0,20,0" IsVisible="{Binding ElementName=Album, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\album.png" Height="26" Width="26" Stretch="Uniform" Margin="2,-1,5,1" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Album" Grid.Row="0" Grid.Column="1" FontSize="{ThemeResource SmallFontSize}"
                   Content="{Binding Album}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"/>
          </Grid>

          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,20,0"
                IsVisible="{Binding ElementName=AlbumArtists, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\person.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="AlbumArtists" FontSize="{ThemeResource SmallFontSize}" VerticalAlignment="Center"
                   Content="{Binding Path=AlbumArtists}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"/>
          </Grid>

          <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\audiogenre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" 
                   Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0">
              <Label.Content>
                <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                  <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                  <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
                </MultiBinding>
              </Label.Content>
            </Label>
          </Grid>

          <StackPanel Orientation="Horizontal">
            <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="30,-5,0,0">
              <media:UserDataWrapper.Template>
                <ControlTemplate>
                  <Grid RenderTransformOrigin="0.5,0.5" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                    <Grid.RenderTransform>
                      <TransformGroup>
                        <ScaleTransform ScaleX="0.7" ScaleY="0.7"/>
                      </TransformGroup>
                    </Grid.RenderTransform>
                    <Control Style="{ThemeResource CircularBackgroundStyle_0}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_18}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_36}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_54}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_72}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_90}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_108}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_126}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_144}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_162}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_180}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_198}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_216}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_234}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_252}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_270}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_288}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_306}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_324}"/>
                    <Control Style="{ThemeResource CircularBackgroundStyle_342}"/>

                    <Control Style="{ThemeResource CircularIndicatorStyle_0}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>0"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_18}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>5"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_36}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>10"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_54}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>15"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_72}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>20"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_90}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>25"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_108}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>30"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_126}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>35"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_144}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>40"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_162}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>45"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_180}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>50"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_198}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>55"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_216}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>60"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_234}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>65"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_252}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>70"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_270}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>75"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_288}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>80"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_306}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>85"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_324}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>90"/>
                      </Control.IsVisible>
                    </Control>
                    <Control Style="{ThemeResource CircularIndicatorStyle_342}">
                      <Control.IsVisible>
                        <Binding Path="PlayPercentage" Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0}>95"/>
                      </Control.IsVisible>
                    </Control>
                    <Grid Margin="-70,-6,70,0">
                      <Grid.RowDefinitions>
                        <RowDefinition Height="*"/>
                      </Grid.RowDefinitions>
                      <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                      </Grid.ColumnDefinitions>
                      <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="28" Color="White" FontFamily="Noto-Bold"
                             HorizontalContentAlignment="Center" VerticalContentAlignment="Center">
                        <Label.Content>
                          <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0:F0}%"/>
                        </Label.Content>
                      </Label>
                    </Grid>
                  </Grid>
                </ControlTemplate>
              </media:UserDataWrapper.Template>
            </media:UserDataWrapper>
            <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-40,-280,-40">
              <Image.Source>
                <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
              </Image.Source>
            </Image>
          </StackPanel>
          
          <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" Margin="0,60,20,0">
            <media:MediaAspectWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                      IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
                  <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,0,5,0" 
                         VerticalAlignment="Center" HorizontalAlignment="Left"/>
                  <Label x:Name="Year" FontSize="{ThemeResource SmallFontSize}" VerticalAlignment="Center" Color="{ThemeResource TextColorForeground}"
                         Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=yyyy}" Margin="40,0,0,0"/>
                </Grid>
              </ControlTemplate>
            </media:MediaAspectWrapper.Template>
          </media:MediaAspectWrapper>
          
          <Grid Margin="0,0,20,0" IsVisible="{Binding ElementName=TrackLabel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\track.png" Height="29" Width="28" Stretch="Uniform" Margin="0,0,5,0"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="TrackLabel" Content="{Binding Track}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                   VerticalAlignment="Center" FontSize="{ThemeResource SmallFontSize}"/>
          </Grid>

          <Grid IsVisible="{Binding ElementName=Duration, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\duration.png" Height="26" Width="26" Stretch="Uniform" Margin="2,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                   Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=mm\:ss}"/>
          </Grid>

          <Grid Margin="0,0,20,0" IsVisible="{Binding ElementName=Disc, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\disc.png" Height="28" Width="28" Stretch="Uniform" Margin="2,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="40,0,0,0">
              <Label x:Name="Disc" FontSize="{ThemeResource SmallFontSize}" Content="{Binding Path=DiscId}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
              <Label FontSize="{ThemeResource SmallerFontSize}" Content=" | " Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"
                     IsVisible="{Binding ElementName=NumDiscs, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}"/>
              <Label x:Name="NumDiscs" FontSize="{ThemeResource SmallFontSize}" Content="{Binding Path=NumDiscs}" 
                     Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
            </StackPanel>
          </Grid>

          <Grid Margin="0,0,20,0" IsVisible="{Binding ElementName=Bitrate, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\settings.png" Height="28" Width="28" Stretch="Uniform" Margin="2,0,5,0" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Bitrate" Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}" 
                   VerticalAlignment="Center" Margin="40,0,0,0">
              <Label.Content>
                <Binding Path="BitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kBit"/>
              </Label.Content>
            </Label>
          </Grid>

        </StackPanel>
      </ControlTemplate>
    </media:AudioAspectWrapper.Template>
  </media:AudioAspectWrapper>

</Grid>
