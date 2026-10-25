<div align="center">

# Manual

**Build it, run it, and set it up for the household.**

</div>

> **What this file is.** The **Manual** chapter, living in `ACCESSIBILITY.md` so that it joins the row
> of pages above the file list. That row shows six pages and no more - README, Contributing, Code of
> conduct, the licence, Security and Accessibility - and GitHub writes the label itself, so nothing a
> repository names its own file can appear there. `SUPPORT.md`, which is the obvious place for a
> manual, is read by GitHub when someone opens an issue but is not one of the six: four repositories
> that carry it in their root show no tab for it. Accessibility is the one free slot left, so the
> manual takes it.

---

## What you need

| | |
|---|---|
| To build | the **.NET 10 SDK**, x64. PowerShell 5 or newer for the build and gate scripts |
| To run the server | the built server layout and one folder to keep its data in |
| To run the screen | the built front end. A Windows desktop today; it builds and its tests run on Linux |
| To play anything | **libVLC** installed on the machine (VLC's own installation is enough). Nothing is bundled |
| For live TV | a **TVHeadend** server reachable from the front end. No TV card, no MP2 TV service |
| For weather | nothing - the built-in keys are tried first; your own **OpenWeatherMap** key is an escape hatch |
| Ports | `55555/tcp` for the server; TVHeadend's own port for TV (HTTP API) |

## Build

```powershell
# the two solutions that matter, the way the gates build them
.\scripts\ci\build.ps1 -Solution Server,Frontend -Ratchet

# or plain dotnet, if you only want to get something on screen
dotnet build .\MediaPortal\Source\MP2-Frontend.sln -c Debug
```

`-Ratchet` compares every warning against the recorded floor for that solution and fails if the
number grew. Without a solution argument all three solutions build, `MP2-Client.sln` included.

## Run the server

The server is published into a folder with its plugins beside it, and then started against a data
folder. Nothing is installed and nothing is registered.

```powershell
# 1 - publish: server, its plugins (SQLite store, NFO reader, audio-tag reader) into build\artifacts\server-layout
.\scripts\ci\deploy-server-layout.ps1

# 2 - start it. The data folder holds the SQLite library; it is created on first run
dotnet .\build\artifacts\server-layout\MP2-Server-Headless.dll --data .\build\artifacts\mp2-live\data
```

| Switch | What it does |
|---|---|
| `--data`, `-d <folder>` | where the SQLite database and its files live |
| `--check` | start, answer, and stop again - the "does this build run at all" question |
| `--duration <sec>` | hold the port open for that long, then exit; what the CI scripts use |

Proof it is alive: `http://127.0.0.1:55555/MediaPortal/Server/health`. To check the whole API in one
go, run `.\scripts\ci\run-server-check.ps1` - six assertions: the state change, the store, the
endpoints, and the three answers an unauthenticated caller must get.

## Run the front end

```powershell
.\MediaPortal\Source\Frontend\MediaPortal.Frontend\bin\Debug\net10.0\MediaPortal.Frontend.exe `
  --server http://127.0.0.1:55555/MediaPortal/Server --theme nereus
```

### Command line

| Switch | What it does |
|---|---|
| `--server <url>` | the server to talk to. Otherwise `MP2_SERVER`, otherwise the address saved on the *which server?* screen, otherwise `http://127.0.0.1:55555/MediaPortal/Server` |
| `--check` | read the server and the player, print what was found, exit - no window |
| `--discover [sec]` | ask the network which MediaPortal servers are there, print them, exit. Listens for `sec` seconds (default 3); exit code 0 only if one answered |
| `--play <file> [sec]` | open the playback screen and play that file - the same screen the library opens, so a film works before any library exists. With `sec`, leave after that many seconds and print how many frames were drawn |
| `--start <time>` | with `--play`, begin at seconds, `mm:ss` or `hh:mm:ss`. Films open dark; asking for a scene with a picture in it is the only honest test |
| `--shot <file>` | with `--play`, save a photograph of what the screen drew to that PNG before leaving |
| `--theme <name>` | wear `default`, `nereus` or `wmc`. Otherwise `MP2_THEME`, otherwise `default`; an unknown name is said out loud |
| `--help` | the same list, from the binary itself |

Exit codes of `--check` and `--discover`: **0** everything answered, **1** nothing to talk to,
**2** the address was not a URL, **3** it answered but was not ready.

### Environment

| Variable | What it chooses |
|---|---|
| `MP2_SERVER` | the server address |
| `MP2_THEME` | the palette: `default`, `nereus`, `wmc` |
| `MP2_SKINS_DIR` | a folder of skins to wear instead of the ones laid beside the executable; the start-up log says which copy is on screen |
| `MP2_SETTINGS_FILE` | where `frontend.json` lives (the server address and a few decisions) |
| `MP2_CLIENT_FILE` | where `client.json` lives (this machine's own settings) |
| `MP2_KEYBINDINGS_FILE` | which `keybindings.json` to read |
| `MP2_NEWS_FEEDS_FILE` | which news feeds to read |
| `MP2_WEATHER_FILE` | which weather places and key to use |

## First run

1. Start the server, then the front end. The **wake screen** comes first - a clock, the last things
   watched, and the question of who is watching.
2. The **which server?** screen lists what answered SSDP. Servers that announced themselves but did
   not answer HTTP are not listed, because offering a dead address helps nobody. Type an address if
   the machine is across a router that swallows multicast.
3. Ask for a server and the library appears: the main menu as tiles on the left, the covers of the
   selected shelf beside them, and what a highlighted title is next to that.
4. Nothing in the library yet? Open **Folders** and add one: pick the folder, say what kind of media
   it holds, and start the import. The same screen shows the shares the server was given and the
   credentials a share on somebody's SMB box needs.
5. Add the household under **Settings → Users** (name, PIN, picture, age ceiling). The PIN is what
   the wake screen asks for, and the ceiling is what a child may be shown.

## The keys

Everything works with a keyboard, and the same keys are what a remote sends. The arrows and `Enter`
belong to the screen that has them; the rest is bound in `keybindings.json` so a remote's odd keys can
be fitted without recompiling.

| Key | What it does |
|---|---|
| `Esc` / `Back` | away with the newest thing on the screen; the shell pops the screen when nothing is open |
| `F16` | the remote's **Menu** key: the list menu on a title screen, the on-screen keyboard on a field |
| `Apps` / `I` | **Details** - what the selected title is, next to it instead of playing it |
| `MediaPlayPause` | the transport key; on a title screen it plays the selected row |
| `F` | move the focus to the search field the screen has |
| `W` | round through the watched states |
| `V` | change the view of the wall |
| `M` | the media key |
| `Volume Up` / `Down`, `+` / `-` | five steps of volume, with the skin's own volume bar |
| `0`-`9` on the live screen | the channel number being typed; the zap fires when the two-second pause runs out |
| `F12` | save what the application itself painted as a PNG |
| `Arrows`, `PageUp/Down`, `Home`, `End` in the guide | walk the grid |
| `Enter` / `Space` in the guide | the programme, or the channel |

The screen's arrows move focus **by geometry** - "the next one to the right" means the next one the
skin drew to the right, which is the only answer that survives a wall of covers.

A mouse works too: a **double-click is Enter**, and the pointer fades out of sight after ten seconds
of quiet so it never sits on a 10-foot picture.

## The screens

| Screen | What it is for |
|---|---|
| Wake | the clock, what was last watched, and the PIN |
| Who is watching | the household's faces, picked before the library opens |
| Home | the main menu as tiles and the shelves of the server beside them |
| Library | the titles of one part of the library, and the way out of them into the player |
| Browse | the three columns a shelf is browsed through: the shelf, the grouping, the value |
| Folders | where the library comes from, and the import that fills it |
| Now playing | the picture, the strip, the skip ladder, the subtitles, the volume |
| Party | start and steer the music, see the queue and the song being played |
| Pictures | a grid of folders and photographs, and the picture being looked at |
| Live TV | the picture, the programme strip, the digits being typed |
| Guide | the programme grid, walked with the arrows |
| Programme actions | what a programme in the guide can be asked: record, and how |
| Plans and recordings | the timers and what has been recorded |
| TV settings | the TVHeadend address and the twelve answers the guide and the zap buttons need |
| News | the feeds, their headlines, the story |
| Weather | the watched places, the days coming, and the search that adds a place |
| Settings | the wall of places a household configures |
| Notifications | the queue, one at a time |
| System status | what the server says about itself, on a list the arrows can walk |
| Server, Client, Users, Playback, Media, Metadata, Plugin, Credentials | the settings screens; see below |
| Skins, Theme | the worn skin's own switches, and the choice of palette |
| Edit the menu | the Nereus main menu, tile by tile |
| On-screen keyboard | the reference's four key grids, for typing with a remote |
| Goodbye | the screen that ends the application |

## Settings

The hub is a wall of doors, and each door opens the pages it really has. A branch with a single screen
is opened straight, so `TV` and `Users` never ask you to choose first.

| Door | Pages behind it |
|---|---|
| **General** | System (what both machines are made of, the shutdown menu, autostart), Connectivity (the server's address - the very screen the start answers with), Server settings (read from the server as the door opens) |
| **Media library** | FanArt cache (this machine's shelf of pictures), Import settings (what the server asks the internet on the way in), then the server's own sections and picture store in the server's order |
| **Media sources** | Media sources configuration (the folders the library is built from, and the import that fills it), Network credentials (the user and password a share on somebody's box needs) |
| **Appearance** | General (fullscreen, start screen, screensaver, skin sounds, culture), Skin (which palette, each row wearing the colours it would give you), Skin Settings (one row per switch the worn skin declares) |
| **Players** | General (what counts as watched, and what happens when a title ends), Audio Player, Video Player (the skip ladder, the volume step, the subtitle and audio languages), Image Player (slideshow length, Ken Burns) |
| **TV** | the TVHeadend address and the twelve answers the guide and the zap buttons need |
| **Plugins** | one plate per settings class the server's parts keep, read from the server every time the door opens |
| **Users** | the household: name, PIN, picture, age ceiling, groups. The door hides from anyone who was handed a PIN - that is the point of a key only one person in the house has |
| **Close** | not a page: it leaves the application, whatever is playing |

The Nereus skin's own switchboard, behind **Appearance → Skin Settings**: transparency and the colour
choices, the cover and menu details, the grid details, auto-scrolling with its speed and delay, loop
scrolling, the clock, date, temperature and condition on the shelf, and how much of the fanart is
thrown out of focus behind the picture.

### The files behind the screens

Everything is a plain file, and every one may be edited while the front end is closed.

| Where | What lives there |
|---|---|
| `%APPDATA%\MediaPortal\frontend.json` | the server's address, and the decisions the front end cannot start without |
| `%APPDATA%\MediaPortal\client.json` | this machine's name, fullscreen, skin sounds, start screen, culture, screensaver minutes, whether to ask who is watching, and which viewer was remembered |
| `%APPDATA%\MediaPortal\keybindings.json` | the remote's keys; delete the file and the defaults come back |
| the news feeds file | the feeds that are read |
| the weather file | the places being watched and `"api_key"` |
| the FanArt cache folder | the cached pictures, capped at the size the page names |

## Live TV

The front end talks to **TVHeadend** by itself: the channel list, the grid, the programme types, the
timers and the recordings all come from its JSON API. MP2's old TV service is gone from the tree and
does not need to come back.

1. **Settings → TV** and give it the host and port of TVHeadend's HTTP interface, with a username and
   password if it asks for one.
2. The same screen holds the twelve answers the guide and the zap buttons need: how many days to
   fetch, how many hours and rows fit on the screen, which channel numbers to show beside the names,
   the zap behaviour, whether TV starts on launch, and how a finished recording is announced.
3. A **401 or 403** says TVHeadend wants credentials; a **404** is answered as "that is not a TV
   backend", because pointing the TV screen at the MediaPortal server is the easiest mistake to make.
4. No TVHeadend to try it with? The switch **demo data** fills the guide and the channel list from a
   built-in backend, so the grid can be walked and laid out before any hardware exists.

Recordings are TVHeadend timers, so they carry on recording when the television is off.

## Weather and news

- **Weather** reads OpenWeatherMap: geocoding for the search, current conditions, and the days
  coming. It works out of the box - three public keys have been committed with the tree since 2021
  and are tried for you. Hitting their limit? Put your own free key in the weather file as
  `"api_key"`; it is tried before the built-in ones, and it is the only line in that file you would
  ever edit by hand.
- **News** reads RSS. The default list comes from
  `http://install.team-mediaportal.com/MP2/News/DefaultFeeds.xml`, and your own list replaces it in
  the news feeds file.

## A picture that is not the picture you want

`Frontend.RenderProbe` draws one screen into a PNG without anybody pressing anything - the same tool
the gates use to prove that four screens still render.

```powershell
dotnet .\MediaPortal\Tests\Frontend.RenderProbe\bin\Release\net10.0\Frontend.RenderProbe.dll `
  --out frame.png --screen home --theme nereus --server http://127.0.0.1:55555/MediaPortal/Server
```

`--screen` takes `home`, `continue`, `nowplaying`, `connect`, `browser`, `cover`, `details`,
`search`, `seen`, `folders`, `pictures`, `browse`, `news`, `weather` or `party`; `--aspect` says which
part of the library a library screen lists, and `--width`/`--height` change the frame.

## When something does not answer

| What you see | What it means |
|---|---|
| `--check` exits **1** | nothing is listening. Start the server, or fix the address |
| `--check` exits **3** | something answered but is not ready; the server was still starting |
| `--check` exits **2** | the address was not a URL |
| 403 on an `api/...` call | the token was missing or is stale; sign in again |
| `unsupported_grant_type` / `invalid_grant` from `/Token` | the server was asked the wrong way, or the user or PIN is wrong |
| "that is not a TV backend" | the TV address is not TVHeadend - usually the MediaPortal server was typed in twice |
| 401/403 from TVHeadend | TVHeadend wants a username and password |
| No picture while playing | libVLC was not found. Install VLC, or point `PATH` at its directory |
| A cover that never arrives | the server is still making it; fanart is warmed on demand and cached locally afterwards |
| A skin that is not the one you edited | the folder beside the executable wins. Rebuild, or set `MP2_SKINS_DIR` |
| Something on the screen looks wrong | `F12` writes a PNG of what the application painted, and the log beside it says what it read |

The front end keeps its settings in `%APPDATA%\MediaPortal\`. Deleting `frontend.json` is the honest
reset: the address, the remembered server and the start screen come back as if for the first time.

## Testing a change

```powershell
.\scripts\ci\tests.ps1                      # the five Windows suites, front end included
.\scripts\ci\test-frontend-check.ps1        # a real server, then --check, then four rendered screens
.\scripts\ci\test-frontend-discovery.ps1    # SSDP end to end
.\scripts\ci\build.ps1 -Solution Frontend -Ratchet
```

The front end's 1250 tests need no display, no keyboard and no server: they drive the view models,
which is the reason the views were kept thin.
