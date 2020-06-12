<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:emulators="clr-namespace:Emulators.MediaExtensions;assembly=Emulators"
  Grid.Column="0" Grid.Row="0" x:Name="MediaItemDetails" Margin="30,220,20,0"
  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
  DataContext="{Binding Source={StaticResource FanArtBackgroundModel},Path=SelectedItem}">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="600"/>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <Grid Grid.Column="1" Grid.Row="1" HorizontalAlignment="Right" VerticalAlignment="Bottom"
         Margin="0,-100,10,100" Width="380" Height="380">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
      <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <Image Grid.Row="0" Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Stretch" Stretch="Uniform"
           Margin="0,10,10,0">
      <Image.Source>
        <ImageSourceWrapper Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"
                            HorizontalTextureAlignment="Right" VerticalTextureAlignment="Top"
                            Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;380;520;False}"/>
      </Image.Source>
      <Image.OpacityMask>
        <VisualBrush>
          <VisualBrush.Visual>
            <Border Name="mask" Background="White" CornerRadius="6" BorderThickness="0" BorderBrush="Black"/>
          </VisualBrush.Visual>
        </VisualBrush>
      </Image.OpacityMask>
    </Image>
  </Grid>

  <Label x:Name="MediaItemDescription" Wrap="True" Grid.Column="0" Grid.Row="1" TextTrimming="WordEllipsis" Opacity="0.6"
        HorizontalAlignment="Stretch" FontSize="{ThemeResource SmallFontSize}"
        Content="{Binding [Emulators.Description]}" IsVisible="{Binding Path=[Emulators.Description],Converter={StaticResource ReferenceToBoolConverter}}"
        Color="{ThemeResource TextColor}" Margin="0,40,20,110"/>
  
  <emulators:GameAspectWrapper Grid.ColumnSpan="2" Grid.Row="0" VerticalAlignment="Stretch"
                               MediaItem="{Binding Source={StaticResource FanArtBackgroundModel},Path=MediaItem}">
    <emulators:GameAspectWrapper.Template>
      <ControlTemplate>
        <StackPanel Margin="0,0,10,0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Label x:Name="GameName" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                 Content="{Binding GameName}" FontSize="{ThemeResource NormalFontSize}" Wrap="True"
                 VerticalAlignment="Stretch" FontFamily="SegoeUIBold"
                 Color="{ThemeResource TextColor}"/>
          <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,0,0,5" VerticalAlignment="Center">
            <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True"
                             StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}" Maximum="5"
                             IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}">
              <StarRatingPanel.Value>
                <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0}/2">
                  <Binding Path="Rating"/>
                </MultiBinding>
              </StarRatingPanel.Value>
              <StarRatingPanel.LayoutTransform>
                <TransformGroup>
                  <ScaleTransform ScaleX="1.6" ScaleY="1.5"/>
                </TransformGroup>
              </StarRatingPanel.LayoutTransform>
            </StarRatingPanel>
            <Label x:Name="RatingCountLabel" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"
                   IsVisible="{Binding Path=Rating,Converter={StaticResource ReferenceToBoolConverter}}"
                   FontSize="{ThemeResource SmallerFontSize}" Margin="15,2,5,1">
              <Label.Content>
                <Binding Path="Rating" Converter="{StaticResource StringFormatConverter}" ConverterParameter="{}({0:F1})"/>
              </Label.Content>
            </Label>
          </StackPanel>
          <Grid IsVisible="{Binding ElementName=Year, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
              <Image Source="HomeContentIcons\year.png" Height="27" Width="27" Stretch="Uniform" Margin="0,-1,5,1" 
                     VerticalAlignment="Center" HorizontalAlignment="Left"/>
              <Label x:Name="Year" HorizontalAlignment="Left" FontSize="{ThemeResource SmallFontSize}" 
                     Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center" Margin="40,0,0,0"
                     Content="{Binding Year}" />
          </Grid>
          <Grid IsVisible="{Binding ElementName=Genre, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\genre.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Genre" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="40,0,0,0"
                   Content="{Binding Genres}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
          </Grid>
          <Grid IsVisible="{Binding ElementName=Platform, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\game.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Platform" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="40,0,0,0"
                   Content="{Binding Platform}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
          </Grid>
          <Grid IsVisible="{Binding ElementName=Developer, Path=Content, Converter={StaticResource EmptyStringToFalseConverter}}">
            <Image Source="HomeContentIcons\brand.png" Height="30" Width="30" Stretch="Uniform" Margin="0,0,5,0" 
                   VerticalAlignment="Center" HorizontalAlignment="Left"/>
            <Label x:Name="Developer" FontSize="{ThemeResource SmallFontSize}" TextTrimming="WordEllipsis" Margin="40,0,0,0"
                   Content="{Binding Developer}" Color="{ThemeResource TextColorForeground}" VerticalAlignment="Center"/>
          </Grid>

          <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Uniform" Height="140" Margin="60,-30,10,-20">
            <Image.Source>
              <ImageSourceWrapper Delay="0.5" Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=ClearArt;140;240}"/>
            </Image.Source>
          </Image>

        </StackPanel>
      </ControlTemplate>
    </emulators:GameAspectWrapper.Template>
  </emulators:GameAspectWrapper>

</Grid>
