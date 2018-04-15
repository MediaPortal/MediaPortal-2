<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:tv="clr-namespace:MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers;assembly=SlimTv.Client"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="200"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <tv:RecordingAspectWrapper Grid.Row="0" Grid.Column="0"
                         MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
    <tv:RecordingAspectWrapper.Template>
      <ControlTemplate>
        <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
               Stretch="Uniform" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="25,35,20,20">
          <Image.Source>
            <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5" HorizontalTextureAlignment="Right">
              <ImageSourceWrapper.Source>
                <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                              fanart:FanArtName="{Binding Channel}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
              </ImageSourceWrapper.Source>
            </ImageSourceWrapper>  
          </Image.Source>
        </Image>
      </ControlTemplate>
    </tv:RecordingAspectWrapper.Template>
  </tv:RecordingAspectWrapper>

  <Image Grid.Column="0" Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="Uniform" Margin="22,20,20,30">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520;False}"
                     FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
  
  <Grid Grid.Column="1" Grid.Row="0" Grid.RowSpan="2"
            HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="20,25,35,5">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="{ThemeResource ProgramInfoHeaderWidth}"/>
      <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>

    <Label x:Name="ChannelLabel" Grid.Row="1" Grid.Column="0" Content="[SlimTvClient.ChannelLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <tv:RecordingAspectWrapper Grid.Row="1" Grid.Column="1"  MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Channel" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Scroll="Auto" Content="{Binding Channel}" Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <Label x:Name="ProgramLabel" Grid.Row="2" Grid.Column="0" Content="[SlimTvClient.ProgramLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Program" Grid.Row="2" Grid.Column="1"  Scroll="Auto" Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}" Color="{ThemeResource TextColor}"/>

    <Label x:Name="ProgramStartLabel" Grid.Row="3" Grid.Column="0" Content="[SlimTvClient.ProgramStartLabel]" Color="{ThemeResource EmphasisTextColor}"/>    
    <tv:RecordingAspectWrapper Grid.Row="3" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="ProgramStart" Scroll="Auto" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding Path=StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"
                Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <Label x:Name="ProgramEndLabel" Grid.Row="4" Grid.Column="0" Content="[SlimTvClient.ProgramEndLabel]" Color="{ThemeResource EmphasisTextColor}" />
    <tv:RecordingAspectWrapper Grid.Row="4" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="ProgramEnd" Scroll="Auto" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Path=EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
              Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>
    
    <Label x:Name="GenreLabel" Grid.Row="5" Grid.Column="0" Content="[SlimTvClient.GenreLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <media:VideoAspectWrapper Margin="0" Grid.Row="5" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Genre" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Genres}" Color="{ThemeResource TextColor}" Scroll="Auto"/>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>

    <!-- TODO: add all related episode information -->
    <Label x:Name="EpisodeLabel" Grid.Row="6" Grid.Column="0" Content="[SlimTvClient.EpisodeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <media:SeriesAspectWrapper Grid.Row="6" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Episode" Margin="0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Series}" Color="{ThemeResource TextColor}" Scroll="Auto" />
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>

    <Label x:Name="DescriptionLabel" Grid.Row="7" Grid.Column="0" Grid.ColumnSpan="2" Content="[SlimTvClient.DescriptionLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Description" Grid.Row="8" Grid.Column="0" Grid.ColumnSpan="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Margin="0,0,0,35"
           Wrap="True" Scroll="Auto" Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}" Color="{ThemeResource TextColor}"/>
  </Grid>

  <Grid Grid.Column="1" Grid.Row="1" Margin="10,0,-350,-39" HorizontalAlignment="Center" VerticalAlignment="Bottom">
    <media:AudioAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:AudioAspectWrapper.Template>
        <ControlTemplate>
          <Label DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}" Color="Black" FontSize="{ThemeResource NormalFontSize}"
                 HorizontalAlignment="Center"/>
        </ControlTemplate>
      </media:AudioAspectWrapper.Template>
    </media:AudioAspectWrapper>
  </Grid>
  
</Grid>
