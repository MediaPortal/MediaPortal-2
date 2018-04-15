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

    <media:VideoAspectWrapper MediaItem="{Binding MediaItem}" Grid.Column="1" Grid.Row="0" Grid.RowSpan="2" Margin="20,5,20,-92">
      <media:VideoAspectWrapper.Template>
        <ControlTemplate>
          <Grid Grid.Row="0" Grid.Column="0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
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

            <!--Label Grid.Row="0" Grid.Column="0" Margin="20,5,0,5" Content="{Binding AudioLanguages}" Color="{ThemeResource TextColor}"
                   HorizontalAlignment="Stretch" VerticalAlignment="Stretch"/-->

            <!--Label Grid.Row="0" Grid.Column="0" Margin="20,5,0,5" Content="{Binding AspectRatio}" Color="{ThemeResource TextColor}"
                   HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
              <Label.Content>
                <Binding Path="VideoBitRate" ContentStringFormat="{}{0:#}" />
              </Label.Content>
            </Label-->


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
            <Label Grid.Row="2" Grid.Column="0" Margin="20,5,0,5" Content="{Binding VideoBitRate}" Color="{ThemeResource TextColor}"
                   HorizontalAlignment="Center" VerticalAlignment="Stretch">
              <Label.Content>
                <Binding Path="VideoBitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kbps" />
              </Label.Content>
            </Label>

            <Image Grid.Row="1" Grid.Column="0" HorizontalAlignment="Center" VerticalAlignment="Stretch" Stretch="Uniform" Width="300" >
              <Image.Source>
                <Binding Path="VideoEncoding" Converter="{StaticResource SafeFilenameConverter}" ConverterParameter="{}Medialogos\video\{0}.png"/>
              </Image.Source>
            </Image>

            <!--Separator-->
            <Border Grid.Row="1" Grid.RowSpan="5" Grid.Column="1" BorderThickness="0" Background="White" Opacity="0.08"
                    HorizontalAlignment="Stretch" VerticalAlignment="Stretch" />

            <Border Grid.Row="1" Grid.RowSpan="5" Grid.Column="5" BorderThickness="0" Background="White" Opacity="0.08"
                    HorizontalAlignment="Stretch" VerticalAlignment="Stretch" /> 
            
           <!--Audio Infos-->
            <ListView Grid.Row="1" Grid.RowSpan="5" Grid.Column="2" x:Name="AudioLanguageList" ItemsSource="{Binding AudioLanguages}" 
                      HorizontalAlignment="Center" VerticalAlignment="Stretch"
                      Style="{ThemeResource AudioLanguageListViewStyle}" Margin="10,5,10,5" />

            <ListView Grid.Row="1" Grid.RowSpan="5" Grid.Column="3" x:Name="AudioEncodingList" ItemsSource="{Binding Path=AudioEncoding, Converter={StaticResource StringSplitConverter}}"
                      HorizontalAlignment="Center" VerticalAlignment="Stretch"
                      Style="{ThemeResource AudioEncodingListViewStyle}" Margin="10,5,10,5" />

            <Label Grid.Row="1" Grid.Column="4" Margin="10,5,10,5" Content="{Binding AudioBitRate}" Color="{ThemeResource TextColor}"
                   HorizontalAlignment="Center" VerticalAlignment="Center">
              <Label.Content>
                <Binding Path="AudioBitRate" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}{0} kbps"/>
              </Label.Content>
            </Label>
            
          </Grid>
        </ControlTemplate>
      </media:VideoAspectWrapper.Template>
    </media:VideoAspectWrapper>


</Grid>