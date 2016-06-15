<?xml version="1.0" encoding="utf-8"?>
<!-- TODO: This screen should be adapted to match its special function, i.e. add images etc. -->
<Include
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fanart="clr-namespace:MediaPortal.Extensions.UserServices.FanArtService.Client;assembly=FanArtService.Client"
    Source="screens\master_slidemenu.xaml"
    >
  <Include.Resources>
    <fanart:FanArtImageSourceConverter x:Key="FanArtImageSourceConverter"/>
    <ResourceWrapper x:Key="Header_Text" Resource="[Media.MoviesItemsHeader]"/>
    <ControlTemplate x:Key="Contents_Template">
      <Grid>
        <Grid.Resources>
          <Model x:Key="MediaModel" Id="4CDD601F-E280-43b9-AD0A-6D7B2403C856"/>
        </Grid.Resources>
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <Control Grid.Column="0" Grid.Row="0" HorizontalAlignment="Stretch">
          <Control.TemplateControl>
            <Include Source="screens\MediaItemsListHeader.inc"/>
          </Control.TemplateControl>
        </Control>
        <Grid Grid.Column="0" Grid.Row="1" VerticalAlignment="Stretch" HorizontalAlignment="Stretch"
          DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}">
          <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
          </Grid.RowDefinitions>
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="560"/>
            <ColumnDefinition Width="460"/>
            <ColumnDefinition Width="820"/>
          </Grid.ColumnDefinitions>

          <!-- Screen and plot view -->
          <Border Grid.Column="2" Grid.Row="0" x:Name="FanartBorderShadow" CornerRadius="20" BorderThickness="2" Width="820" Height="570" Background="#FF404040">
            <Border.RenderTransform>
              <TransformGroup>
                <TranslateTransform X="-15" />
                <ScaleTransform ScaleX="1.07" ScaleY="1.035"/>
              </TransformGroup>
            </Border.RenderTransform>
            <Border.Effect>
              <ZoomBlurEffect/>
            </Border.Effect>
          </Border>

          <Border Grid.Column="2" Grid.Row="0" x:Name="FanartBorder" BorderBrush="#000000" CornerRadius="20" BorderThickness="2" Width="820" Height="570" Background="#E4000000">
            <Border.RenderTransform>
              <TranslateTransform X="-20"/>
            </Border.RenderTransform>

            <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
                <RowDefinition Height="400"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>

              <Label Grid.Row="0" Margin="10" x:Name="StoryPlot" Wrap="True" Scroll="Auto" ScrollSpeed="5"
                  HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                  Content="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ItemDescription}"
                  Color="{ThemeResource TextColor}" FontSize="{ThemeResource SmallFontSize}"/>

              <Image Grid.Row="1" HorizontalAlignment="Stretch" VerticalAlignment="Stretch" Stretch="UniformToFill" Margin="15" FallbackSource="{Binding MediaItem}"
                     Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=FanArt;580;360}">
              </Image>
            </Grid>
          </Border>

          <!-- Poster view -->
          <Border Grid.Column="1" Grid.Row="0" x:Name="CoverBorderShadow" CornerRadius="20" BorderThickness="2" Width="460" Height="665" Background="#FF404040">
            <Border.RenderTransform>
              <TransformGroup>
                <TranslateTransform Y="5"/>
                <ScaleTransform ScaleX="1.032" ScaleY="1.025"/>
              </TransformGroup>
            </Border.RenderTransform>
            <Border.Effect>
              <ZoomBlurEffect/>
            </Border.Effect>
          </Border>

          <Border Grid.Column="1" Grid.Row="0" x:Name="CoverBorder" BorderBrush="#000000" CornerRadius="20" BorderThickness="2" Width="460" Height="665" Background="#E4000000">
            <Image VerticalAlignment="Stretch" HorizontalAlignment="Stretch" Stretch="UniformToFill" Margin="15"
                   Source="{Binding Source={StaticResource FanArtBackgroundModel}, Path=ImageSource, Converter={StaticResource FanArtImageSourceConverter}, ConverterParameter=Poster;500;660}">
            </Image>
          </Border>

          <!-- Itemslist -->
          <Border Grid.Column="0" Grid.Row="0" x:Name="ListBorderShadow" CornerRadius="20" BorderThickness="2" Width="560" Height="750" Background="#FF404040">
            <Border.RenderTransform>
              <TransformGroup>
                <TranslateTransform X="10"/>
                <ScaleTransform ScaleX="1.024" ScaleY="1.02"/>
              </TransformGroup>
            </Border.RenderTransform>
            <Border.Effect>
              <ZoomBlurEffect BlurAmount="0.1"/>
            </Border.Effect>
          </Border>

          <Border Grid.Column="0" Grid.Row="0" x:Name="ListBorder" BorderBrush="#000000" CornerRadius="20" BorderThickness="2" Width="560" Height="750" Background="#E4000000">
            <Border.RenderTransform>
              <TranslateTransform X="20"/>
            </Border.RenderTransform>

            <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}">
              <Grid.Resources>
                <Model x:Key="MenuModel" Id="9E9D0CD9-4FDB-4c0f-A0C4-F356E151BDE0"/>
              </Grid.Resources>
              <Grid.Triggers>
                <DataTrigger Binding="{Binding Source={StaticResource MenuModel},Path=IsMenuOpen}" Value="False">
                  <Setter TargetName="MediaItemsList" Property="SetFocusPrio" Value="RestoreState" />
                </DataTrigger>
              </Grid.Triggers>
              <Grid.RowDefinitions>
                <RowDefinition Height="*"/>
              </Grid.RowDefinitions>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
              </Grid.ColumnDefinitions>
              <ListView x:Name="MediaItemsList" Style="{ThemeResource MoviesListViewStyle}" ItemsSource="{Binding Items}"
                HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" Margin="10">
                <ListView.Resources>
                  <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
                  Parameters="{LateBoundValue BindingValue={Binding}}"/>
                </ListView.Resources>
              </ListView>
              <Label IsVisible="{Binding ShowListHint}"
                HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
                Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
            </Grid>

          </Border>

          <Label IsVisible="{Binding !IsItemsValid}"
            HorizontalAlignment="Center" VerticalAlignment="Center"
            Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
          <KeyBinding Key="F5" Command="{Command Reload}"/>
        </Grid>
      </Grid>
    </ControlTemplate>
  </Include.Resources>
</Include>
