<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:tv="clr-namespace:MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers;assembly=SlimTv.Client"
  Grid.Column="2" Grid.Row="0" x:Name="MediaItemDetails" Margin="{ThemeResource MultiDetailsFooterMargin}"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="240"/>
    <ColumnDefinition Width="110"/>
  </Grid.ColumnDefinitions>

  <StackPanel Orientation="Horizontal" Grid.Column="0" Grid.Row="0" >
    <Label x:Name="MediaItemName" Wrap="True" Margin="-10,-15,0,0"
           HorizontalAlignment="Stretch"
           Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"
           Color="{ThemeResource TextColor}"
           FontFamily="DefaultBold"/>
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Episode" Margin="0,0,-10,0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Series}" Color="{ThemeResource TextColor}" Scroll="Auto" />
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
  </StackPanel>
  
  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1"
         Scroll="Auto" ScrollSpeed="5" ScrollDelay="5" HorizontalAlignment="Stretch"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
         Color="{ThemeResource TextColor}" Margin="-10,0,10,25"/>

  <StackPanel Orientation="Vertical" Grid.Column="1" Grid.Row="1" Margin="0,-30,0,0">
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Channel" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Scroll="Auto" Content="{Binding Channel}" Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="ProgramStart" Scroll="Auto" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                Content="{Binding Path=StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"
                Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <media:VideoAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Duration" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Path=Duration, Converter={StaticResource DurationConverter}, ConverterParameter=hh\:mm\:ss}" Color="{ThemeResource TextColor}" />
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>

    <media:VideoAspectWrapper Margin="0,0,-50,0"
                              MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Genre" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
              Content="{Binding Genres}" Color="{ThemeResource TextColor}" />
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>
  </StackPanel>

    <tv:RecordingAspectWrapper Grid.Row="1" Grid.Column="2" 
                             MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
            <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Stretch="Uniform" HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,-40,0,130">
              <Image.Source>
                <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                              fanart:FanArtName="{Binding Channel}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
              </Image.Source>
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
