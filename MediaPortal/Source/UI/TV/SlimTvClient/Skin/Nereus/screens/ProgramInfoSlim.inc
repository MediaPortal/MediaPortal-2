<?xml version="1.0" encoding="utf-8"?>
<Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    x:Name="ProgramInfoPanel" Height="170" Margin="0,0,40,0"
    HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
    >
  
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="Auto"/>
      <ColumnDefinition Width="*"/> 
    </Grid.ColumnDefinitions>

   <StackPanel Orientation="Vertical" Grid.RowSpan="2" MinWidth="420">
      <!-- Common control for program duration formatting -->
      <Control Style="{StaticResource ProgramDurationControlStyle}"
               IsVisible="{Binding !NoProgramData}"/>
      <Label x:Name="GenreLabel" Content="{Binding Path=Genre}" Color="{ThemeResource TextColor}" Opacity="0.6" 
             IsVisible="{Binding Path=Genre,Converter={StaticResource ReferenceToBoolConverter}}" Margin="0,0,10,0"/>
      <Label x:Name="Channel" Scroll="Auto" Content="{Binding ChannelName}" Color="{ThemeResource TextColor}"
             HorizontalAlignment="Left" Opacity="0.6" IsVisible="{Binding ElementName=ChannelLogo, Path=!HasImage}"/>
      <Image x:Name="ChannelLogo" Width="{ThemeResource ChannelLogoLarge}" Margin="0,10,0,0"
             Stretch="Uniform" HorizontalAlignment="Left" VerticalAlignment="Top" >
        <Image.Source>
          <fanart:FanArtImageSource fanart:FanArtMediaType="{Binding ChannelLogoType}" fanart:FanArtType="Banner"
                                    fanart:FanArtName="{Binding ChannelName}" fanart:MaxWidth="0" fanart:MaxHeight="0"/>
        </Image.Source>
      </Image>
    </StackPanel>

    <StackPanel Orientation="Horizontal" Grid.Row="0" Grid.Column="1">
      <Label x:Name="Program" Scroll="Auto" FontFamily="Noto-Bold" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
             Content="{Binding Title}" Color="{ThemeResource TextColor}"/>
      <Label x:Name="SeriesLabel" Color="{ThemeResource TextColor}" Margin="5,0,0,0" Opacity="0.6"
             IsVisible="{Binding Path=Series, Converter={StaticResource EmptyStringToFalseConverter}}">
        <Label.Content>
          <Binding Path="Series" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{} ({0})"/>
        </Label.Content>
      </Label>
    </StackPanel>

    <Label Grid.Row="1" Grid.Column="1" x:Name="Description" HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
           Wrap="True" Scroll="Auto" Opacity="0.6" Content="{Binding Path=Description}"
           ScrollSpeed="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].AutoScrollSpeed}"
           ScrollDelay="{Binding Source={StaticResource SkinSettingsModel}, Path=[Nereus].AutoScrollDelay}"
           Color="{ThemeResource TextColor}" Margin="0,0,0,-40"/>
</Grid>
