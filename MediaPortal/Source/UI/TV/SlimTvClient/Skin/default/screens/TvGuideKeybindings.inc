<?xml version="1.0" encoding="utf-8"?>
  <Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    >
    <Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      >
      <KeyBinding Key="Guide" Command="{Command Source={Service WorkflowManager},Path=TryExecuteAction,Parameters=9C3E6701-6856-49ec-A4CD-0CEB15F385F6}"/>
      <KeyBinding Key="Yellow" Command="{Command Path=NextGroup}"/>
      <KeyBinding Key="Green" Command="{Command Path=PrevGroup}"/>
      <KeyBinding Key="Fwd" Command="{Command Path=ScrollForward1Screen}"/>
      <KeyBinding Key="Rew" Command="{Command Path=ScrollBackward1Screen}"/>
      <KeyBinding Key="Next" Command="{Command Path=ScrollForward12Hours}"/>
      <KeyBinding Key="Previous" Command="{Command Path=ScrollBackward12Hours}"/>
    </Grid>
    <Grid xmlns="www.team-mediaportal.com/2008/mpf/directx"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      DataContext="{Binding Source={Model 1C7DCFFE-E34E-41FD-9104-9AA594E49375}}"
      >
      <!-- Channel zapping by numbers -->
      <KeyBinding Key="0" Command="{Command Path=ZapByNumber,Parameters=0}"/>
      <KeyBinding Key="1" Command="{Command Path=ZapByNumber,Parameters=1}"/>
      <KeyBinding Key="2" Command="{Command Path=ZapByNumber,Parameters=2}"/>
      <KeyBinding Key="3" Command="{Command Path=ZapByNumber,Parameters=3}"/>
      <KeyBinding Key="4" Command="{Command Path=ZapByNumber,Parameters=4}"/>
      <KeyBinding Key="5" Command="{Command Path=ZapByNumber,Parameters=5}"/>
      <KeyBinding Key="6" Command="{Command Path=ZapByNumber,Parameters=6}"/>
      <KeyBinding Key="7" Command="{Command Path=ZapByNumber,Parameters=7}"/>
      <KeyBinding Key="8" Command="{Command Path=ZapByNumber,Parameters=8}"/>
      <KeyBinding Key="9" Command="{Command Path=ZapByNumber,Parameters=9}"/>
    </Grid>
  </Grid>
