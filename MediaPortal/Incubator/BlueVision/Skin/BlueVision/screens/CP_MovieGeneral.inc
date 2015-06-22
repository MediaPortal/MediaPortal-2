<?xml version="1.0" encoding="utf-8"?>
<Grid
  xmlns="www.team-mediaportal.com/2008/mpf/directx"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
  xmlns:media="clr-namespace:MediaPortal.UiComponents.Media.Models.AspectWrappers;assembly=Media"
  xmlns:mp_special_controls="clr-namespace:MediaPortal.UI.SkinEngine.SpecialElements.Controls;assembly=SkinEngine">

  <Grid.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>


        <Grid Grid.Row="0" Grid.Column="0" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
          <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>          
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="300"/>
          </Grid.ColumnDefinitions>
        
          <mp_special_controls:PlayerControl PlayerContext="CurrentPlayer" Grid.Row="0" Grid.Column="0" Margin="20,15,20,5">
           <mp_special_controls:PlayerControl.TemplateControl>
            <Label x:Name="Title" Scroll="Auto" FontFamily="DefaultBold" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}"
                   Content="{Binding MediaItemTitle}" Color="{ThemeResource TextColor}"/>
            </mp_special_controls:PlayerControl.TemplateControl>
          </mp_special_controls:PlayerControl> 
          
          <mp_special_controls:PlayerControl PlayerContext="CurrentPlayer" Grid.Row="1" Grid.Column="0" Margin="20,5,20,5">
           <mp_special_controls:PlayerControl.TemplateControl>
            <StackPanel Orientation="Horizontal" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
               <Label x:Name="Year" Content="{Binding VideoYear}" Color="{ThemeResource TextColor}"/>
               <Label Content=" • " Color="{ThemeResource TextColor}"/>
               <Label x:Name="Duration" Content="{Binding Duration}" Color="{ThemeResource TextColor}"/>
               <Label Content=" • " Color="{ThemeResource TextColor}"/>
               <Label x:Name="Genre" Content="{Binding VideoGenres}" Color="{ThemeResource TextColor}"/>
            </StackPanel>
           </mp_special_controls:PlayerControl.TemplateControl>
          </mp_special_controls:PlayerControl>             
           
          <media:MediaAspectWrapper MediaItem="{Binding MediaItem}" Grid.Column="0" Grid.Row="2" Margin="20,5,20,5" >
            <media:MediaAspectWrapper.Template>
              <ControlTemplate>
                  <StackPanel Orientation="Horizontal" HorizontalAlignment="Left" Margin="0,6,0,5" >
                    <StarRatingPanel x:Name="RatingReadOnly" IsReadOnly="True" StarReadOnlyTemplate="{StaticResource StarReadOnlyTemplate}"
                                               Maximum="10" Value="{Binding TotalRating}"
                                     DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                      <StarRatingPanel.LayoutTransform>
                        <TransformGroup>
                          <ScaleTransform ScaleX="1.7" ScaleY="1.5"/>
                        </TransformGroup>
                      </StarRatingPanel.LayoutTransform>
                    </StarRatingPanel>
                  </StackPanel>
              </ControlTemplate>
            </media:MediaAspectWrapper.Template>
          </media:MediaAspectWrapper>
        
          <media:MediaAspectWrapper MediaItem="{Binding MediaItem}" Grid.Column="1" Grid.Row="0" Grid.RowSpan="2" Margin="20,5,20,5">
            <media:MediaAspectWrapper.Template>
              <ControlTemplate>
                <Grid DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                  <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                  </Grid.RowDefinitions>
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                  </Grid.ColumnDefinitions>
                  <Image HorizontalAlignment="Right" VerticalAlignment="Center" Stretch="Fill" Width="40" Height="25" Grid.Row="0" Grid.Column="0" Margin="0,15,10,0" >
                    <Image.Source>
                      <MultiBinding Converter="{StaticResource ExpressionMultiValueConverter}" ConverterParameter="{}{0} ? {1} : {2}">
                        <Binding Path="PlayCount"/>
                        <Binding Source=""/>
                        <Binding Source="unwatched_icon.png"/> 
                      </MultiBinding>
                    </Image.Source>
                  </Image>
                </Grid>
              </ControlTemplate>
            </media:MediaAspectWrapper.Template>
          </media:MediaAspectWrapper>
            
          <mp_special_controls:PlayerControl PlayerContext="CurrentPlayer" Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" Margin="20,10,20,5">
           <mp_special_controls:PlayerControl.TemplateControl>
                <ScrollViewer HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                    DockPanel.Dock="Center" HorizontalScrollBarVisibility="Disabled"
                    Focusable="True" DataContext="{Binding RelativeSource={RelativeSource TemplatedParent}}">
                  <Label x:Name="StoryPlot" Wrap="True"
                      HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                      Content="{Binding VideoStoryPlot}"
                      Color="{ThemeResource TextColor}"/>
                </ScrollViewer>
           </mp_special_controls:PlayerControl.TemplateControl>
          </mp_special_controls:PlayerControl>        
  
       </Grid>
</Grid>
