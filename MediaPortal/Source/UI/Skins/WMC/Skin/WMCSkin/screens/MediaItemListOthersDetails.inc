<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,30,30,-200">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="540"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Image Grid.Column="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="0,10,10,0" Height="210">
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

  <StackPanel Orientation="Vertical" Grid.Row="0" Margin="0,0,20,0" VerticalAlignment="Stretch">
    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal">
            <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}" FontFamily="SegoeUIBold"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Label.Content>
                <Binding Path="Title"/>
              </Label.Content>
            </Label>
          </StackPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:MediaAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:MediaAspectWrapper.Template>
        <ControlTemplate>
          <StackPanel Orientation="Horizontal">
            <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}" Opacity="0.5"
                   Content="{Binding Path=RecordingTime, Converter={StaticResource DateFormatConverter}, ConverterParameter=g}" Color="{ThemeResource TextColor}"/>
          </StackPanel>
        </ControlTemplate>
      </media:MediaAspectWrapper.Template>
    </media:MediaAspectWrapper>

    <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <StackPanel Orientation="Horizontal">
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Opacity="0.5"
                     Color="{ThemeResource TextColor}">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" min." FontSize="{ThemeResource SmallFontSize}" Opacity="0.5" Color="{ThemeResource TextColor}"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>
  </StackPanel>

  <media:VideoAspectWrapper Grid.Row="1" Grid.ColumnSpan="2" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:VideoAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Orientation="Horizontal">
          <Label x:Name="MediaItemName" Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"
                 Opacity="0.5" TextTrimming="WordEllipsis" Margin="0,40,5,25"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label.Content>
              <Binding Path="StoryPlot"/>
            </Label.Content>
          </Label>
        </StackPanel>
      </ControlTemplate>
    </media:VideoAspectWrapper.Template>
  </media:VideoAspectWrapper>

</Grid>
