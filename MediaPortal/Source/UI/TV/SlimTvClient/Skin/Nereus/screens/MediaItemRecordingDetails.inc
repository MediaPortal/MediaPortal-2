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
    <ColumnDefinition Width="420"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <StackPanel Orientation="Vertical" Grid.Row="0" Grid.RowSpan="2" Grid.Column="0" Margin="-10,0,0,0">
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="ProgramStart" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
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
              <Label x:Name="Duration" Color="{ThemeResource TextColor}"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
              <Label Content="[Media.Minutes]" Color="{ThemeResource TextColor}"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Channel" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding Channel}" Color="{ThemeResource TextColor}"/>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                               HorizontalAlignment="Left" VerticalAlignment="Top">
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Image x:Name="ChannelLogo" Width="100" Margin="0,10,0,0"
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Top"
                 IsVisible="{Binding RelativeSource={RelativeSource TemplatedParent},Path=Channel,Converter={StaticResource ReferenceToBoolConverter}}">
            <Image.Source>
              <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                                        fanart:FanArtName="{Binding Channel}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
            </Image.Source>
          </Image>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>
  </StackPanel>

  <Grid Grid.Row="0" Grid.Column="1" >
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>
    <Label x:Name="MediaItemName" Wrap="True" Margin="0,0,0,0" 
           HorizontalAlignment="Stretch" Color="{ThemeResource TextColor}" FontFamily="DefaultBold"
           Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"/>
    <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:SeriesAspectWrapper.Template>
        <ControlTemplate>
          <Label x:Name="Episode" Margin="10,0,10,0" Grid.Column="1"
                 TextTrimming="WordEllipsis" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding Series}" Opacity="1.3" Color="{ThemeResource TextColor}" />
        </ControlTemplate>
      </media:SeriesAspectWrapper.Template>
    </media:SeriesAspectWrapper>
  </Grid>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="1" Grid.Row="1" Opacity="0.6" Color="{ThemeResource TextColor}"
         TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}" Margin="0,0,10,25"/>

</Grid>
