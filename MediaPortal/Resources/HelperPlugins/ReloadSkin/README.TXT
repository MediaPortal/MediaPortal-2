ReloadSkin plugin
-----------------
This plugin can be used during the skin/theme development.
When active, it provides hotkeys for reloading screen contents:

F3  - Reload the current screen
F4  - Reload the saved skin/theme
F12 - Stick to current skin/theme

With that plugin in action, you can develop and test a theme or screens while MP2
is running. Navigate into the appropriate theme/screen folder in the MP2 runtime
directory and change the appropriate files, then press F3 to reload the current
screen or F4 to reload the complete skin and theme.

If there is a problem with the theme currently being used, the ScreenManager
automatically falls back to the default skin/default theme.
This is annoying if you are testing a new skin or theme because after fixing
the problem, you would need to navigate to the skin/theme settings screen to
reload (and retest) the skin/theme being tested.
To help in that situation, this plugin provides a function (F12) to stick to the
skin/theme combination which is in use when you press that key. F4 will
then always try to reload that specific skin/theme instead of
just reloading the active (fallback) skin/theme.

Albert, 2011-10-03