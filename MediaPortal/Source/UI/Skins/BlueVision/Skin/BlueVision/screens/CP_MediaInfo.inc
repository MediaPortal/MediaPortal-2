<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:mp_special_controls="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Controls;assembly=SkinEngine">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <ExpressionMultiValueConverter x:Key="ExpressionMultiValueConverter"/>
  </Grid.Resources>

  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Column="0" Grid.Row="0" Margin="20,5,20,-92">
    <Grid.RowDefinitions>
      <RowDefinition Height="50"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="25*"/>
      <ColumnDefinition Width="4"/>
      <ColumnDefinition Width="15*"/>
      <ColumnDefinition Width="15*"/>
      <ColumnDefinition Width="15*"/>
      <ColumnDefinition Width="4"/>
      <ColumnDefinition Width="15*"/>
      <ColumnDefinition Width="15*"/>
    </Grid.ColumnDefinitions>

    <!--HeadLines-->
    <Label Grid.Row="0" Grid.Column="0" Margin="0" Content="[Video.Title]" Color="{ThemeResource TextColor}"
           HorizontalAlignment="Center" VerticalAlignment="Center">
    </Label>
    <Label Grid.Row="0" Grid.Column="2" Grid.ColumnSpan="3" Margin="0" Content="[Audio.Title]" Color="{ThemeResource TextColor}"
           HorizontalAlignment="Center" VerticalAlignment="Center">
    </Label>
    <Label Grid.Row="0" Grid.Column="6" Grid.ColumnSpan="2" Margin="0" Content="[Subtitle.Title]" Color="{ThemeResource TextColor}"
           HorizontalAlignment="Center" VerticalAlignment="Center">
    </Label>

    <!--Video Infos-->
    <media:VideoStreamAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="2" HorizontalAlignment="Center">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
          <Label Color="{ThemeResource TextColor}" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 HorizontalAlignment="Center" VerticalAlignment="Stretch">
            <Label.Content>
              <Binding Path="VideoBitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kbps" />
            </Label.Content>
          </Label>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>

    <media:VideoStreamAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="1" HorizontalAlignment="Center">
      <media:VideoStreamAspectWrapper.Template>
        <ControlTemplate>
            <Image HorizontalAlignment="Center" VerticalAlignment="Stretch" Stretch="Uniform" Width="300"
                   DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
              <Image.Source>
                <Binding Path="VideoEncoding" Converter="{StaticResource SafeFilenameConverter}" ConverterParameter="{}Medialogos\video\{0}.png"/>
              </Image.Source>
            </Image>
        </ControlTemplate>
      </media:VideoStreamAspectWrapper.Template>
    </media:VideoStreamAspectWrapper>

    <!--Separator-->
    <Border Grid.Row="1" Grid.RowSpan="5" Grid.Column="1" BorderThickness="0" Background="White" Opacity="0.08"
            HorizontalAlignment="Stretch" VerticalAlignment="Stretch" />

    <Border Grid.Row="1" Grid.RowSpan="5" Grid.Column="5" BorderThickness="0" Background="White" Opacity="0.08"
            HorizontalAlignment="Stretch" VerticalAlignment="Stretch" />

    <!--Audio Infos-->
    <media:VideoAudioStreamAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="1" Grid.RowSpan="5" Grid.Column="2" HorizontalAlignment="Center">
      <media:VideoAudioStreamAspectWrapper.Template>
        <ControlTemplate>
          <ListView x:Name="AudioLanguageList" ItemsSource="{Binding AudioLanguages}"
                    HorizontalAlignment="Center" VerticalAlignment="Stretch"
                    DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                    Style="{ThemeResource AudioLanguageListViewStyle}" Margin="10,5,10,5" />
        </ControlTemplate>
      </media:VideoAudioStreamAspectWrapper.Template>
    </media:VideoAudioStreamAspectWrapper>

    <media:VideoAudioStreamAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="1" Grid.RowSpan="5" Grid.Column="3" HorizontalAlignment="Center">
      <media:VideoAudioStreamAspectWrapper.Template>
        <ControlTemplate>
          <ListView x:Name="AudioEncodingList" ItemsSource="{Binding Path=AudioEncoding, Converter={StaticResource StringSplitConverter}}"
                    HorizontalAlignment="Center" VerticalAlignment="Stretch"
                    DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                    Style="{ThemeResource AudioEncodingListViewStyle}" Margin="10,5,10,5" />
        </ControlTemplate>
      </media:VideoAudioStreamAspectWrapper.Template>
    </media:VideoAudioStreamAspectWrapper>

    <media:VideoAudioStreamAspectWrapper MediaItem="{Binding MediaItem}" Grid.Row="1" Grid.Column="4"  HorizontalAlignment="Center">
      <media:VideoAudioStreamAspectWrapper.Template>
        <ControlTemplate>
          <Label Content="{Binding AudioBitRate}" Color="{ThemeResource TextColor}" Margin="10,5,10,5"
                 HorizontalAlignment="Center" VerticalAlignment="Center" 
                 DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
            <Label.Content>
              <Binding Path="AudioBitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kbps"/>
            </Label.Content>
          </Label>
        </ControlTemplate>
      </media:VideoAudioStreamAspectWrapper.Template>
  </media:VideoAudioStreamAspectWrapper>

  </Grid>

</Grid>