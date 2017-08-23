<?xml version="1.0" encoding="utf-8"?>
<Grid Margin="0,-2,7,6"
    xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    DataContext="{Binding Source={Model 4CDD601F-E280-43b9-AD0A-6D7B2403C856},Path=NavigationData.CurrentScreenData}"
    > <!-- Model = MediaNavigationModel -->

  <Image Source="menu_fade.png" Stretch="Fill" HorizontalAlignment="Left" Opacity="0.2" >
    <Image.IsVisible>
      <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"
               Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
    </Image.IsVisible>
  </Image>

  <Image Source="menu_fade.png" Stretch="Fill" HorizontalAlignment="Left" Opacity="0.4" Width="35">
    <Image.IsVisible>
      <Binding Source="{StaticResource ViewModeModel}" Path="LayoutType"
               Converter="{StaticResource ExpressionValueConverter}" ConverterParameter="{}{0} != {LayoutType}.{CoverLayout}"/>
    </Image.IsVisible>
  </Image>  
  
  <Grid.Resources>
    <Model x:Key="MediaModel" Id="4CDD601F-E280-43b9-AD0A-6D7B2403C856"/>
  </Grid.Resources>
  <Grid.RowDefinitions>
    <RowDefinition Height="*"/>
  </Grid.RowDefinitions>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
  </Grid.ColumnDefinitions>

  <!-- All KeyBindings are included from shared resource -->
  <Include Source="screens\MediaItemsFilterKeyBindings.inc"/>

  <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding IsItemsValid}">
    <Grid.RowDefinitions>
      <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <ListView x:Name="MediaItemsList" Style="{ThemeResource MediaItemsListListViewStyle}" ItemsSource="{Binding Items}" RestoreFocus="True"
        HorizontalAlignment="Stretch" VerticalAlignment="Stretch" IsVisible="{Binding ShowList}" Margin="10,25,-15,10">
      <ListView.Resources>
        <ResourceWrapper x:Key="VerticalScrollbarRenderTransform">
          <TranslateTransform X="40" />
        </ResourceWrapper>
        <Command x:Key="Menu_Command" Source="{StaticResource MediaModel}" Path="Select"
            Parameters="{LateBoundValue BindingValue={Binding}}"/>
      </ListView.Resources>
    </ListView>
    
    <Label IsVisible="{Binding ShowListHint}"
        HorizontalAlignment="Center" VerticalAlignment="Center" Wrap="true"
        Content="{Binding ListHint}" Color="{ThemeResource HintTextColor}"/>
  </Grid>
  <Label IsVisible="{Binding !IsItemsValid}"
      HorizontalAlignment="Center" VerticalAlignment="Center"
      Content="[Media.ViewInvalid]" Color="{ThemeResource ErrorTextColor}"/>
</Grid>
