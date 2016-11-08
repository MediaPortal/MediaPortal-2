<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:aspect="clr-namespace:MediaPortal.Common.MediaManagement.DefaultItemAspects;assembly=MediaPortal.Common"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="0,30,20,-200"
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
    <ColumnDefinition Width="590"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill"
         Margin="0,10,10,0" Width="270" Height="390">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}">
      </ImageSourceWrapper>
    </Image.Source>
    <Image.OpacityMask>
      <VisualBrush>
        <VisualBrush.Visual>
          <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
        </VisualBrush.Visual>
      </VisualBrush>
    </Image.OpacityMask>
  </Image>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                              Grid.Column="0" Grid.Row="0" Margin="0,0,10,0">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Vertical" HorizontalAlignment="Stretch" VerticalAlignment="Top">
          <Label x:Name="SeriesNameLabel" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" Wrap="True" FontFamily="SegoeUIBold"
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
          <StackPanel Orientation="Horizontal" DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">
            <Label FontSize="{ThemeResource SmallFontSize}" Content="{Binding Episodes}"
                   Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
            <Label FontSize="{ThemeResource SmallFontSize}" Content="[Media.EpisodeItemsHeader]"
                   IsVisible="{Binding Path=Episodes,Converter={StaticResource ReferenceToBoolConverter}}"
                   Margin="0,0,8,0" Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
          </StackPanel>

          <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
                 Opacity="1.3" Color="{ThemeResource MenuTextBackground}">
            <Label.Content>
              <MultiBinding Converter="{StaticResource MultiAspectAttributeConverter}">
                <Binding Source="{StaticResource FanArtBackgroundModel}" Path="MediaItem"/>
                <Binding Source="{x:Static aspect:GenreAspect.ATTR_GENRE}" />
              </MultiBinding>
            </Label.Content>
          </Label>

          <media:UserDataWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
            <media:UserDataWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                      IsVisible="{Binding Path=PlayPercentage,Converter={StaticResource ReferenceToBoolConverter}}">
                  <Image HorizontalAlignment="Left" VerticalAlignment="Center" Stretch="Fill" Width="95" Height="95" Margin="0,10,10,10" >
                    <Image.Source>
                      <Binding Path="PlayPercentage" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}CircularBar\{0:F0}.png"/>
                    </Image.Source>
                  </Image>
                </Grid>
              </ControlTemplate>
            </media:UserDataWrapper.Template>
          </media:UserDataWrapper>
        </StackPanel>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

  <media:SeasonAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             Grid.Column="0" Grid.ColumnSpan="2" Grid.Row="2" Margin="0,30,0,10" Height="440">
    <media:SeasonAspectWrapper.Template>
      <ControlTemplate>
        <Grid>
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Label x:Name="Description"  Grid.Row="1" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Wrap="True" TextTrimming="WordEllipsis" FontSize="{ThemeResource SmallFontSize}" Margin="0,5,5,0"
                 HorizontalAlignment="Stretch" Content="{Binding Description}"
                 Opacity="1.3" Color="{ThemeResource MenuTextBackground}"/>
        </Grid>
      </ControlTemplate>
    </media:SeasonAspectWrapper.Template>
  </media:SeasonAspectWrapper>

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
