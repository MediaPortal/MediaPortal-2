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
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="380"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Column="1" Grid.Row="0"
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

    <tv:RecordingAspectWrapper Grid.Row="1" Grid.Column="0" MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}" >
      <tv:RecordingAspectWrapper.Template>
        <ControlTemplate>
          <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto"/>
              <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="{ThemeResource ProgramInfoHeaderWidth}"/>
              <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" Grid.Row="0" Grid.Column="0"
                 Stretch="Uniform" HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,-5,0,0">
              <Image.Source>
                <fanart:FanArtImageSource fanart:FanArtMediaType="ChannelTv" fanart:FanArtType="Banner"
                              fanart:FanArtName="{Binding Channel}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
              </Image.Source>
            </Image>


            <Label x:Name="ChannelLabel" Grid.Row="1" Grid.Column="0" Content="[SlimTvClient.ChannelLabel]" Color="{ThemeResource EmphasisTextColor}"/>
            <Label x:Name="Channel" Grid.Row="1" Grid.Column="1"  Scroll="Auto" Content="{Binding Channel}" Color="{ThemeResource TextColor}"/>

          </Grid>
        </ControlTemplate>
      </tv:RecordingAspectWrapper.Template>
    </tv:RecordingAspectWrapper>

    <Label x:Name="ProgramLabel" Grid.Row="2" Grid.Column="0" Content="[SlimTvClient.ProgramLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Program" Grid.Row="2" Grid.Column="1"  Scroll="Auto" Content="{Binding Path=CurrentProgram.Title}" Color="{ThemeResource TextColor}"/>

    <Label x:Name="ProgramStartLabel" Grid.Row="3" Grid.Column="0" Content="[SlimTvClient.ProgramStartLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="ProgramStart" Grid.Row="3" Grid.Column="1" Scroll="Auto"
        Content="{Binding Path=CurrentProgram.StartTime, Converter={StaticResource SlimTvDateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="ProgramEndLabel" Grid.Row="4" Grid.Column="0" Content="[SlimTvClient.ProgramEndLabel]" Color="{ThemeResource EmphasisTextColor}" />
    <Label x:Name="ProgramEnd" Grid.Row="4" Grid.Column="1" Scroll="Auto"
        Content="{Binding Path=CurrentProgram.EndTime, Converter={StaticResource SlimTvDateFormatConverter}}"
        Color="{ThemeResource TextColor}"/>

    <Label x:Name="GenreLabel" Grid.Row="5" Grid.Column="0" Content="[SlimTvClient.GenreLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Genre" Grid.Row="5" Grid.Column="1"  Content="{Binding Path=CurrentProgram.Genre}" Color="{ThemeResource TextColor}" Scroll="Auto"/>

    <!-- TODO: add all related episode information -->
    <Label x:Name="EpisodeLabel" Grid.Row="6" Grid.Column="0" Content="[SlimTvClient.EpisodeLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Episode" Grid.Row="6" Grid.Column="1"  Content="{Binding Path=CurrentProgram.Series}" Color="{ThemeResource TextColor}" Scroll="Auto"/>

    <Label x:Name="DescriptionLabel" Grid.Row="7" Grid.Column="0" Grid.ColumnSpan="2" Content="[SlimTvClient.DescriptionLabel]" Color="{ThemeResource EmphasisTextColor}"/>
    <Label x:Name="Description" Grid.Row="8" Grid.Column="0" Grid.ColumnSpan="2" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
        Wrap="True" Scroll="Auto" Content="{Binding Path=CurrentProgram.Description}" Color="{ThemeResource TextColor}"/>
  </Grid>



  <Image Grid.Column="0" HorizontalAlignment="Center" VerticalAlignment="Stretch" Stretch="Fill" Margin="27,-7,17,30"
      Width="380" Height="520">
    <Image.Source>
      <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520;False}"/>
    </Image.Source>
    <Image.OpacityMask >
      <ImageBrush ImageSource="filmstrip_mask.png"/>
    </Image.OpacityMask>
  </Image>
</Grid>
