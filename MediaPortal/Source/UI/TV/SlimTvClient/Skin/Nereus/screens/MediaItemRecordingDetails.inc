<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:tv="clr-namespace:MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers;assembly=SlimTv.Client"
  x:Name="MediaItemDetails" Margin="30,220,40,60"
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
    <ColumnDefinition Width="455"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <tv:RecordingAspectWrapper Grid.Row="1" Grid.Column="1" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                             HorizontalAlignment="Right" VerticalAlignment="Bottom">
    <tv:RecordingAspectWrapper.Template>
      <ControlTemplate>
        <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Image HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill" Width="456" Height="316">
            <Image.Source>
              <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                  Source="Thumbnail_large.png"/>
            </Image.Source>
          </Image>
          <Image HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Fill" Margin="8" Width="440" Height="300">
            <Image.Source>
              <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                                  FallbackSource="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}"
                                  Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Thumbnail;440;300}"/>
            </Image.Source>
          </Image>
          </Grid>
      </ControlTemplate>
    </tv:RecordingAspectWrapper.Template>
  </tv:RecordingAspectWrapper>

  <StackPanel Orientation="Vertical" Grid.Row="0" Grid.Column="0">
    <Grid>
      <Grid.RowDefinitions>
        <RowDefinition Height="*"/>
      </Grid.RowDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <Label x:Name="MediaItemName" Wrap="True" HorizontalAlignment="Stretch" FontSize="{ThemeResource NormalFontSize}" Color="{ThemeResource TextColor}"
             FontFamily="SegoeUIBold" Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=SimpleTitle}"/>
      <media:SeriesAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
        <media:SeriesAspectWrapper.Template>
          <ControlTemplate>
            <Label x:Name="Episode" Margin="10,0,10,0" Grid.Column="1"
                   TextTrimming="WordEllipsis" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                   Content="{Binding Series}" Color="{ThemeResource TextColor}" />
          </ControlTemplate>
        </media:SeriesAspectWrapper.Template>
      </media:SeriesAspectWrapper>
    </Grid>

    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=ProgramStart, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="ProgramStart" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                   Content="{Binding Path=StartTime, Converter={StaticResource SlimTvDateFormatConverter}}" />
          </Grid>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>
    <media:VideoStreamAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Duration, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\duration.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <StackPanel Orientation="Horizontal">
              <Label x:Name="Duration" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" Margin="40,0,0,0">
                <Label.Content>
                  <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/60">
                    <Binding Path="Duration"/>
                  </MultiBinding>
                </Label.Content>
              </Label>
              <Label Content=" " IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
              <Label Content="[Media.Minutes]" Color="{ThemeResource TextColorForeground}" FontSize="{ThemeResource SmallFontSize}"
                     IsVisible="{Binding Path=Duration,Converter={StaticResource ReferenceToBoolConverter}}" VerticalAlignment="Center"/>
            </StackPanel>
          </Grid>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>
    <tv:RecordingAspectWrapper MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                IsVisible="{Binding ElementName=Channel, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\channel.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1"
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Channel" FontSize="{ThemeResource SmallFontSize}" Color="{ThemeResource TextColorForeground}" Margin="40,0,0,0"
                   Content="{Binding Channel}" />
          </Grid>
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

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Row="1" Opacity="0.6" Color="{ThemeResource TextColor}"
         TextTrimming="WordEllipsis" HorizontalAlignment="Stretch"
         Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}" Margin="0,50,20,25"/>

</Grid>
