<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,220,20,-60"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

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
    <Image x:Name="SeasonPoster" HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Fill" Margin="8" Width="278" Height="412">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;278;412}"/>
      </Image.Source>
    </Image>
    <Image HorizontalAlignment="Center" VerticalAlignment="Center" Stretch="Uniform" Width="200" Height="200"
           Source="video.png" IsVisible="{Binding ElementName=SeasonPoster, Path=!HasImage}"/>
  </Grid>
  
  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" VerticalAlignment="Stretch"
                             Grid.Column="0" Grid.Row="1" Margin="0,40,10,50">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Grid>
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Label x:Name="Description" Grid.Row="1" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Margin="0,5,5,0"
                 HorizontalAlignment="Stretch" Content="{Binding Description}"
                 Opacity="0.6" Color="{ThemeResource TextColor}"/>
        </Grid>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="0" Margin="0,0,10,0">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment="Top">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource NormalFontSize}" Wrap="True" FontFamily="Noto-Bold"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" TextTrimming="WordEllipsis">
            <Label.Content>
              <Binding Path="SeriesName" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0}"/>
            </Label.Content>
          </Label>

          <StackPanel Orientation="Horizontal">
            <Label Content="[Media.SeasonRootViewName]" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>
            <Label x:Name="SeasonLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Margin="7,0,0,0"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Content="{Binding Season}"/>
          </StackPanel>
          <Grid DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
            <Image Source="HomeContentIcons\series.png" Height="25" Width="25" Stretch="Uniform" Margin="2,-1,5,1"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal" Margin="40,0,0,0">
              <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Episodes}"
                     IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                     Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
              <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
                     IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                     Margin="0,0,8,0" Color="{ThemeResource TextColorForeground}"/>
            </StackPanel>
          </Grid>
          <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Color="{ThemeResource TextColorForeground}">
            <Label.Content>
              <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
              </MultiBinding>
            </Label.Content>
          </Label>

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
        </StackPanel>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

</Grid>
