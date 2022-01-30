<?xml version="1.0" encoding="utf-8"?>
<Grid Margin="40,10,20,10"
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
    <ColumnDefinition Width="455"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Row="1" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Bottom" Width="456" Height="316" Margin="0,-30,10,30">
    <Image HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
           Margin="0" Width="456" Height="316">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="Thumbnail_large.png"/>
      </Image.Source>
    </Image>
    <Image x:Name="EpisodePoster" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
           Margin="8" Width="440" Height="300">
      <Image.Source>
        <fanart:FanArtImageSource fanart:FanArtMediaType="Episode" fanart:FanArtType="Thumbnail" fanart:FanArtName="{Binding MediaItem.MediaItemId}"
                                  fanart:MaxWidth="{StaticResource FANART_POSTER_WIDTH}" fanart:MaxHeight="{StaticResource FANART_POSTER_HEIGHT}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
           Source="video.png" IsVisible="{Binding ElementName=EpisodePoster, Path=!HasImage}"/>
  </Grid>
  
  <StackPanel Orientation="Vertical" Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="0" Margin="0,0,0,5">
    <media:EpisodeAspectWrapper MediaItem="{Binding MediaItem}" >
      <media:EpisodeAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" VerticalAlignment="Center">
            <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment="Bottom" >
              <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" 
                     Wrap="True" FontFamily="SegoeUIBold" TextTrimming="WordEllipsis">
                <Label.Content>
                  <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
                </Label.Content>
              </Label>
              <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                <Label Content="[Media.SeasonRootViewName]" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
                <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Margin="7,0,0,0"
                       DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Content="{Binding Season}"/>
                <Label Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Content=" | "/>
                <Label Content="[Media.EpisodeRootViewName]" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
                <Label x:Name="EpisodeLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Margin="7,0,0,0"
                       DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Content="{Binding Episode}"/>
              </StackPanel>

              <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" VerticalAlignment="Center" Margin="0,3,0,3">
                <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}" Maximum="5" 
                                 VerticalAlignment="Center" IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
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
                <Label x:Name="RatingCountLabel" Margin="15,0,5,0" VerticalAlignment="Center"
                       FontSize="{ThemeResource SmallerFontSize}" Color="{ThemeResource TextColorForeground}"
                       IsVisible="{Binding Path=TotalRating,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Label.Content>
                    <Binding Path="TotalRating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
                  </Label.Content>
                </Label>
              </StackPanel>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:EpisodeAspectWrapper.Template>
    </media:EpisodeAspectWrapper>
    
    <media:MediaAspectWrapper MediaItem="{Binding MediaItem}" >
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=FirstAired, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="FirstAired" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=d}" />
          </Grid>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <Grid IsVisible="{Binding ElementName=Duration, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
      <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
             VerticalAlignment="Center" HorizontalAlignment="Left"/>
      <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" 
             Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0">
        <Label.Content>
          <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
            <Binding Path="MediaItem"/>
            <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
          </MultiBinding>
        </Label.Content>
      </Label>
    </Grid>

    <media:VideoStreamAspectWrapper MediaItem="{Binding MediaItem}">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Duration, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\duration.png" Height="26" Width="26" Stretch="Uniform" Margin="2,-1,5,1" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" 
                     VerticalAlignment="Center">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" 
                     VerticalAlignment="Center"/>              
            </StackPanel>

          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>

    <StackPanel Orientation="Horizontal" Margin="30,0,0,0">
      <media:UserDataWrapper MediaItem="{Binding MediaItem}">
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
                <Label VerticalAlignment="Center" HorizontalAlignment="Center" FontSize="28" Color="White" FontFamily="SegoeUIBold"
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
      <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,-280,-40" >
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="Episode" fanart:FanArtType="ClearArt" fanart:FanArtName="{Binding MediaItem.MediaItemId}"
                                    fanart:MaxWidth="240" fanart:MaxHeight="140"/>
        </Image.Source>
      </Image>
    </StackPanel>
  </StackPanel>

  <media:EpisodeAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="2" Margin="0,30,10,0">
    <media:EpisodeAspectWrapper.Template>
      <ControlTemplate>
        <Grid>
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Label x:Name="EpisodeNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Grid.Row="0" Margin="0,0,-500,0">
            <Label.Content>
              <Binding Path="EpisodeName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>
          <media:VideoAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Stretch" Grid.Row="1" Margin="0,5,5,50"
                                    IsVisible="{Binding Source={StaticResource SkinSettingsModel}, Path=![Nereus].EnableAutoScrolling}">
            <media:VideoAspectWrapper.Template>
              <ControlTemplate>
                 <ScrollViewer CanContentScroll="True" HorizontalScrollBarVisibility="Disabled" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                               Focusable="True" Margin="0,10,10,0" Height="380" Width="460">
                    <ScrollViewer.Triggers>
                      <Trigger Property="HasFocus" Value="True">
                        <Setter Property="VerticalScrollBarVisibility" Value="Auto"/>
                        <Setter Property="Opacity" Value="1"/>
                      </Trigger>
                      <Trigger Property="HasFocus" Value="False">
                        <Setter Property="VerticalScrollBarVisibility" Value="Hidden"/>
                        <Setter Property="Opacity" Value="0.7"/>
                      </Trigger>
                     </ScrollViewer.Triggers>
                   <ScrollViewer.Resources>
                      <ResourceWrapper x:Key="VerticalScrollbarRenderTransform">
                        <TranslateTransform X="0" />
                      </ResourceWrapper>
                    </ScrollViewer.Resources>
                    <Label x:Name="StoryPlot" Wrap="True" Color="White"
                           FontSize="{ThemeResource SmallFontSize}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                           Content="{Binding StoryPlot}"/>
                  </ScrollViewer>
              </ControlTemplate>
            </media:VideoAspectWrapper.Template>
          </media:VideoAspectWrapper>

          <media:VideoAspectWrapper MediaItem="{Binding MediaItem}" VerticalAlignment="Stretch" Grid.Row="1" 
                                    IsVisible="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].EnableAutoScrolling}">
            <media:VideoAspectWrapper.Template>
              <ControlTemplate>
                    <Label x:Name="StoryPlot" Wrap="True" Color="White" Width="460" MaxHeight="380" Margin="0,30,10,30"
                           DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Scroll="Auto" 
                           ScrollSpeed="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].AutoScrollSpeed}"
                           ScrollDelay="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].AutoScrollDelay}"
                           FontSize="{ThemeResource SmallFontSize}" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                           Content="{Binding StoryPlot}" Opacity="0.7"/>
              </ControlTemplate>
            </media:VideoAspectWrapper.Template>
          </media:VideoAspectWrapper>
        </Grid>
      </ControlTemplate>
    </media:EpisodeAspectWrapper.Template>
  </media:EpisodeAspectWrapper>
      
</Grid>
