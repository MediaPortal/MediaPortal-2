<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:tv="clr-namespace:MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers;assembly=SlimTv.Client"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="90,30,40,-80"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

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
  
  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1" Grid.ColumnSpan="2"
         HorizontalAlignment="Stretch" Opacity="0.5" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
         Color="{ThemeResource TextColor}" Margin="0,50,0,5"/>

  <StackPanel Orientation="Vertical" Grid.Column="0" Grid.Row="0" Margin="0" HorizontalAlignment="Left">
    
    <Label x:Name="MediaItemName" Wrap="True" Margin="0"
           HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}"
           Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
           Color="{ThemeResource TextColor}"/>
    
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Episode" Margin="0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 FontSize="{ThemeResource SmallFontSize}"
              Content="{Binding Series}" Color="{ThemeResource TextColor}" Wrap="True" />
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
      
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" HorizontalAlignment="Left">
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Center" Height="80" Margin="-20,0,0,0">
            <Image.Source>
              <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                            fanart:FanArtName="{Binding Channel}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
            </Image.Source>
          </Image>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Channel" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 FontSize="{ThemeResource SmallFontSize}"
                 Content="{Binding Channel}" Opacity="0.5" Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="ProgramStart" Scroll="Auto" Opacity="0.5" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding Path=StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"
                Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

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

    <media:VideoAspectWrapper Margin="0,0,-50,0"
                              MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Genre" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Genres}" Color="{ThemeResource TextColor}" Opacity="0.5" />
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>
  </StackPanel>

    <tv:RecordingAspectWrapper Grid.Row="0" Grid.Column="1"
                             MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
            <Image DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Stretch="Uniform" HorizontalAlignment="Right" VerticalAlignment="Top" MaxHeight="280">
             <Image.Source>
                <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" HorizontalTextureAlignment="Right" VerticalTextureAlignment="Top"
                          FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                          Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520}"/>
                </Image.Source>
                <Image.OpacityMask>
                  <VisualBrush>
                    <VisualBrush.Visual>
                      <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
                    </VisualBrush.Visual>
                  </VisualBrush>
              </Image.OpacityMask>
            </Image>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

  <!--media:MediaAspectWrapper Grid.Row="3" Grid.Column="2" Margin="0,0,0,0"
                            MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <media:MediaAspectWrapper.Template>
      <ControlTemplate>
        <Image HorizontalAlignment="Right" VerticalAlignment="Center" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Width="37" Height="24" Stretch="Uniform"  >
          <Image.Source>
            <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
              <Binding Path="PlayCount"/>
              <Binding Source=""/>
              <Binding Source="unwatched_icon.png"/>
            </MultiBinding>
          </Image.Source>
        </Image>
      </ControlTemplate>
    </media:MediaAspectWrapper.Template>
</media:MediaAspectWrapper-->


</Grid>
