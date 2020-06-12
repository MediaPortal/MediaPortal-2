#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Emulators.Common.RawG.Data
{
  /*
    {
        "id": 3498,
        "slug": "grand-theft-auto-v",
        "name": "Grand Theft Auto V",
        "name_original": "Grand Theft Auto V",
        "description": "<p>Rockstar Games went bigger, since their previous installment of the series. You get the complicated and realistic world-building from Liberty City of GTA4 in the setting of lively and diverse Los Santos, from an old fan favorite GTA San Andreas. 561 different vehicles (including every transport you can operate) and the amount is rising with every update. <br />\nSimultaneous storytelling from three unique perspectives: <br />\nFollow Michael, ex-criminal living his life of leisure away from the past, Franklin, a kid that seeks the better future, and Trevor, the exact past Michael is trying to run away from. <br />\nGTA Online will provide a lot of additional challenge even for the experienced players, coming fresh from the story mode. Now you will have other players around that can help you just as likely as ruin your mission. Every GTA mechanic up to date can be experienced by players through the unique customizable character, and community content paired with the leveling system tends to keep everyone busy and engaged.</p>",
        "metacritic": 96,
        "released": "2013-09-17",
        "tba": false,
        "updated": "2019-10-24T00:39:04",
        "background_image": "https://media.rawg.io/media/games/b11/b115b2bc6a5957a917bc7601f4abdda2.jpg",
        "background_image_additional": "https://media.rawg.io/media/screenshots/5f5/5f5a38a222252d996b18962806eed707.jpg",
        "website": "http://www.rockstargames.com/V/",
        "rating": 4.48,
        "rating_top": 5,
        "ratings": [{
                "id": 5,
                "title": "exceptional",
                "count": 1879,
                "percent": 58.85
            }, {
                "id": 4,
                "title": "recommended",
                "count": 1074,
                "percent": 33.64
            }, {
                "id": 3,
                "title": "meh",
                "count": 190,
                "percent": 5.95
            }, {
                "id": 1,
                "title": "skip",
                "count": 50,
                "percent": 1.57
            }
        ],
        "reactions": {
            "1": 13,
            "2": 2,
            "3": 18,
            "4": 8,
            "5": 7,
            "6": 4,
            "7": 10,
            "8": 11,
            "10": 1,
            "11": 7,
            "12": 7,
            "14": 3,
            "15": 2,
            "16": 4,
            "18": 3,
            "20": 1,
            "21": 2
        },
        "added": 10690,
        "added_by_status": {
            "yet": 200,
            "owned": 6848,
            "beaten": 2568,
            "toplay": 307,
            "dropped": 434,
            "playing": 333
        },
        "playtime": 68,
        "screenshots_count": 59,
        "movies_count": 8,
        "creators_count": 11,
        "achievements_count": 369,
        "parent_achievements_count": 75,
        "reddit_url": "https://www.reddit.com/r/GrandTheftAutoV/d",
        "reddit_name": "GrandTheftAutoV: page not found",
        "reddit_description": "/r/GrandTheftAutoV - the subreddit for all GTA V related news, content, and discussions revolving around Rockstar's critically acclaimed single player release and the ongoing multiplayer expansion of Grand Theft Auto Online.",
        "reddit_logo": "",
        "reddit_count": 0,
        "twitch_count": 103,
        "youtube_count": 1000000,
        "reviews_text_count": 28,
        "ratings_count": 3165,
        "suggestions_count": 413,
        "alternative_names": ["GTA V", "GTA5", "GTAV"],
        "metacritic_url": "",
        "parents_count": 0,
        "additions_count": 3,
        "game_series_count": 9,
        "user_game": null,
        "reviews_count": 3193,
        "saturated_color": "0f0f0f",
        "dominant_color": "0f0f0f",
        "parent_platforms": [{
                "platform": {
                    "id": 1,
                    "name": "PC",
                    "slug": "pc"
                }
            }, {
                "platform": {
                    "id": 2,
                    "name": "PlayStation",
                    "slug": "playstation"
                }
            }, {
                "platform": {
                    "id": 3,
                    "name": "Xbox",
                    "slug": "xbox"
                }
            }
        ],
        "platforms": [{
                "platform": {
                    "id": 4,
                    "name": "PC",
                    "slug": "pc",
                    "image": null,
                    "year_end": null,
                    "year_start": null,
                    "games_count": 205785,
                    "image_background": "https://media.rawg.io/media/games/490/49016e06ae2103881ff6373248843069.jpg"
                },
                "released_at": "2013-09-17",
                "requirements": {
                    "minimum": "Minimum:OS: Windows 10 64 Bit, Windows 8.1 64 Bit, Windows 8 64 Bit, Windows 7 64 Bit Service Pack 1, Windows Vista 64 Bit Service Pack 2* (*NVIDIA video card recommended if running Vista OS)Processor: Intel Core 2 Quad CPU Q6600 @ 2.40GHz (4 CPUs) / AMD Phenom 9850 Quad-Core Processor (4 CPUs) @ 2.5GHzMemory: 4 GB RAMGraphics: NVIDIA 9800 GT 1GB / AMD HD 4870 1GB (DX 10, 10.1, 11)Storage: 72 GB available spaceSound Card: 100% DirectX 10 compatibleAdditional Notes: Over time downloadable content and programming changes will change the system requirements for this game.  Please refer to your hardware manufacturer and www.rockstargames.com/support for current compatibility information. Some system components such as mobile chipsets, integrated, and AGP graphics cards may be incompatible. Unlisted specifications may not be supported by publisher.     Other requirements:  Installation and online play requires log-in to Rockstar Games Social Club (13+) network; internet connection required for activation, online play, and periodic entitlement verification; software installations required including Rockstar Games Social Club platform, DirectX , Chromium, and Microsoft Visual C++ 2008 sp1 Redistributable Package, and authentication software that recognizes certain hardware attributes for entitlement, digital rights management, system, and other support purposes.     SINGLE USE SERIAL CODE REGISTRATION VIA INTERNET REQUIRED; REGISTRATION IS LIMITED TO ONE ROCKSTAR GAMES SOCIAL CLUB ACCOUNT (13+) PER SERIAL CODE; ONLY ONE PC LOG-IN ALLOWED PER SOCIAL CLUB ACCOUNT AT ANY TIME; SERIAL CODE(S) ARE NON-TRANSFERABLE ONCE USED; SOCIAL CLUB ACCOUNTS ARE NON-TRANSFERABLE.  Partner Requirements:  Please check the terms of service of this site before purchasing this software.",
                    "recommended": "Recommended:OS: Windows 10 64 Bit, Windows 8.1 64 Bit, Windows 8 64 Bit, Windows 7 64 Bit Service Pack 1Processor: Intel Core i5 3470 @ 3.2GHz (4 CPUs) / AMD X8 FX-8350 @ 4GHz (8 CPUs)Memory: 8 GB RAMGraphics: NVIDIA GTX 660 2GB / AMD HD 7870 2GBStorage: 72 GB available spaceSound Card: 100% DirectX 10 compatibleAdditional Notes:"
                }
            }, {
                "platform": {
                    "id": 18,
                    "name": "PlayStation 4",
                    "slug": "playstation4",
                    "image": null,
                    "year_end": null,
                    "year_start": null,
                    "games_count": 4535,
                    "image_background": "https://media.rawg.io/media/games/490/49016e06ae2103881ff6373248843069.jpg"
                },
                "released_at": "2013-09-17",
                "requirements": null
            }, {
                "platform": {
                    "id": 16,
                    "name": "PlayStation 3",
                    "slug": "playstation3",
                    "image": null,
                    "year_end": null,
                    "year_start": null,
                    "games_count": 3568,
                    "image_background": "https://media.rawg.io/media/games/d1a/d1a2e99ade53494c6330a0ed945fe823.jpg"
                },
                "released_at": "2013-09-17",
                "requirements": null
            }, {
                "platform": {
                    "id": 14,
                    "name": "Xbox 360",
                    "slug": "xbox360",
                    "image": null,
                    "year_end": null,
                    "year_start": null,
                    "games_count": 2491,
                    "image_background": "https://media.rawg.io/media/games/198/1988a337305e008b41d7f536ce9b73f6.jpg"
                },
                "released_at": "2013-09-17",
                "requirements": null
            }, {
                "platform": {
                    "id": 1,
                    "name": "Xbox One",
                    "slug": "xbox-one",
                    "image": null,
                    "year_end": null,
                    "year_start": null,
                    "games_count": 3121,
                    "image_background": "https://media.rawg.io/media/games/fc1/fc1307a2774506b5bd65d7e8424664a7.jpg"
                },
                "released_at": "2013-09-17",
                "requirements": null
            }
        ],
        "stores": [{
                "id": 290375,
                "url": "https://store.playstation.com/en-us/product/UP1004-CUSA00419_00-GTAVDIGITALDOWNL",
                "store": {
                    "id": 3,
                    "name": "PlayStation Store",
                    "slug": "playstation-store",
                    "domain": "store.playstation.com",
                    "games_count": 6208,
                    "image_background": "https://media.rawg.io/media/games/490/49016e06ae2103881ff6373248843069.jpg"
                }
            }, {
                "id": 290378,
                "url": "https://www.microsoft.com/en-us/store/p/grand-theft-auto-v/bpj686w6s0nh?cid=msft_web_chart",
                "store": {
                    "id": 2,
                    "name": "Xbox Store",
                    "slug": "xbox-store",
                    "domain": "microsoft.com",
                    "games_count": 2754,
                    "image_background": "https://media.rawg.io/media/games/b11/b115b2bc6a5957a917bc7601f4abdda2.jpg"
                }
            }, {
                "id": 290377,
                "url": "https://marketplace.xbox.com/en-us/product/gta-v/66acd000-77fe-1000-9115-d802545408a7",
                "store": {
                    "id": 7,
                    "name": "Xbox 360 Store",
                    "slug": "xbox360",
                    "domain": "marketplace.xbox.com",
                    "games_count": 1759,
                    "image_background": "https://media.rawg.io/media/games/f99/f9979698c43fd84c3ab69280576dd3af.jpg"
                }
            }, {
                "id": 290376,
                "url": "http://store.steampowered.com/app/271590/",
                "store": {
                    "id": 1,
                    "name": "Steam",
                    "slug": "steam",
                    "domain": "store.steampowered.com",
                    "games_count": 39794,
                    "image_background": "https://media.rawg.io/media/games/4a0/4a0a1316102366260e6f38fd2a9cfdce.jpg"
                }
            }
        ],
        "developers": [{
                "id": 10,
                "name": "Rockstar Games",
                "slug": "rockstar-games",
                "games_count": 30,
                "image_background": "https://media.rawg.io/media/games/5bf/5bf88a28de96321c86561a65ee48e6c2.jpg"
            }, {
                "id": 3524,
                "name": "Rockstar North",
                "slug": "rockstar-north",
                "games_count": 27,
                "image_background": "https://media.rawg.io/media/games/1bb/1bb86c35ffa3eb0d299b01a7c65bf908.jpg"
            }
        ],
        "genres": [{
                "id": 4,
                "name": "Action",
                "slug": "action",
                "games_count": 80950,
                "image_background": "https://media.rawg.io/media/games/91c/91c4f377c1e09755b60a0102c5252843.jpg"
            }, {
                "id": 2,
                "name": "Shooter",
                "slug": "shooter",
                "games_count": 23151,
                "image_background": "https://media.rawg.io/media/games/34b/34b1f1850a1c06fd971bc6ab3ac0ce0e.jpg"
            }
        ],
        "tags": [{
                "id": 40836,
                "name": "Full controller support",
                "slug": "full-controller-support",
                "language": "eng",
                "games_count": 8167,
                "image_background": "https://media.rawg.io/media/games/310/3106b0e012271c5ffb16497b070be739.jpg"
            }, {
                "id": 40847,
                "name": "Steam Achievements",
                "slug": "steam-achievements",
                "language": "eng",
                "games_count": 17553,
                "image_background": "https://media.rawg.io/media/games/198/1988a337305e008b41d7f536ce9b73f6.jpg"
            }, {
                "id": 13,
                "name": "Atmospheric",
                "slug": "atmospheric",
                "language": "eng",
                "games_count": 7188,
                "image_background": "https://media.rawg.io/media/games/e9c/e9cbc91e2090638ddab6ae0b3d334f90.jpg"
            }, {
                "id": 123,
                "name": "Comedy",
                "slug": "comedy",
                "language": "eng",
                "games_count": 3903,
                "image_background": "https://media.rawg.io/media/games/c6b/c6bfece1daf8d06bc0a60632ac78e5bf.jpg"
            }, {
                "id": 18,
                "name": "Co-op",
                "slug": "co-op",
                "language": "eng",
                "games_count": 4841,
                "image_background": "https://media.rawg.io/media/games/83f/83f6f70a7c1b86cd2637b029d8b42caa.jpg"
            }, {
                "id": 144,
                "name": "Crime",
                "slug": "crime",
                "language": "eng",
                "games_count": 1388,
                "image_background": "https://media.rawg.io/media/screenshots/fd4/fd4da6198e718001cd922f13b2e4d5bf.jpeg"
            }, {
                "id": 8,
                "name": "First-Person",
                "slug": "first-person",
                "language": "eng",
                "games_count": 6488,
                "image_background": "https://media.rawg.io/media/games/73e/73eecb8909e0c39fb246f457b5d6cbbe.jpg"
            }, {
                "id": 4,
                "name": "Funny",
                "slug": "funny",
                "language": "eng",
                "games_count": 7579,
                "image_background": "https://media.rawg.io/media/games/ec3/ec3a7db7b8ab5a71aad622fe7c62632f.jpg"
            }, {
                "id": 42,
                "name": "Great Soundtrack",
                "slug": "great-soundtrack",
                "language": "eng",
                "games_count": 2783,
                "image_background": "https://media.rawg.io/media/games/b7b/b7b8381707152afc7d91f5d95de70e39.jpg"
            }, {
                "id": 192,
                "name": "Mature",
                "slug": "mature",
                "language": "eng",
                "games_count": 544,
                "image_background": "https://media.rawg.io/media/screenshots/386/3866ffe0b96612632fa22c6ecd20f427.jpg"
            }, {
                "id": 62,
                "name": "Moddable",
                "slug": "moddable",
                "language": "eng",
                "games_count": 397,
                "image_background": "https://media.rawg.io/media/games/ed5/ed5b7d01dd68fd8d598c91ad61f153af.jpg"
            }, {
                "id": 7,
                "name": "Multiplayer",
                "slug": "multiplayer",
                "language": "eng",
                "games_count": 18949,
                "image_background": "https://media.rawg.io/media/games/157/15742f2f67eacff546738e1ab5c19d20.jpg"
            }, {
                "id": 36,
                "name": "Open World",
                "slug": "open-world",
                "language": "eng",
                "games_count": 2440,
                "image_background": "https://media.rawg.io/media/games/eaf/eaf4423c35322d6534b92c43858e5c7b.jpg"
            }, {
                "id": 24,
                "name": "RPG",
                "slug": "rpg",
                "language": "eng",
                "games_count": 9010,
                "image_background": "https://media.rawg.io/media/games/088/088b41ca3f9d22163e43be07acf42304.jpg"
            }, {
                "id": 37,
                "name": "Sandbox",
                "slug": "sandbox",
                "language": "eng",
                "games_count": 2339,
                "image_background": "https://media.rawg.io/media/games/48c/48cb04ca483be865e3a83119c94e6097.jpg"
            }, {
                "id": 31,
                "name": "Singleplayer",
                "slug": "singleplayer",
                "language": "eng",
                "games_count": 65131,
                "image_background": "https://media.rawg.io/media/games/b11/b115b2bc6a5957a917bc7601f4abdda2.jpg"
            }, {
                "id": 149,
                "name": "Third Person",
                "slug": "third-person",
                "language": "eng",
                "games_count": 2060,
                "image_background": "https://media.rawg.io/media/games/b11/b115b2bc6a5957a917bc7601f4abdda2.jpg"
            }, {
                "id": 150,
                "name": "Third-Person Shooter",
                "slug": "third-person-shooter",
                "language": "eng",
                "games_count": 806,
                "image_background": "https://media.rawg.io/media/games/a6c/a6ccd34125c594abf1a9c9821b9a715d.jpg"
            }, {
                "id": 411,
                "name": "cooperative",
                "slug": "cooperative",
                "language": "eng",
                "games_count": 2140,
                "image_background": "https://media.rawg.io/media/games/588/588c6bdff3d4baf66ec36b1c05b793bf.jpg"
            }
        ],
        "publishers": [{
                "id": 2155,
                "name": "Rockstar Games",
                "slug": "rockstar-games",
                "games_count": 62,
                "image_background": "https://media.rawg.io/media/screenshots/d53/d53b8c15ac1e9f2aaec384a123f11b7e.jpg"
            }
        ],
        "esrb_rating": {
            "id": 4,
            "name": "Mature",
            "slug": "mature"
        },
        "clip": {
            "clip": "https://media.rawg.io/media/stories-640/5b0/5b0cfff8c606c5e4db4f74f108c4413b.mp4",
            "clips": {
                "320": "https://media.rawg.io/media/stories-320/91d/91d6b5963064a5f686f635c302095b55.mp4",
                "640": "https://media.rawg.io/media/stories-640/5b0/5b0cfff8c606c5e4db4f74f108c4413b.mp4",
                "full": "https://media.rawg.io/media/stories/f64/f64ce0b857918b0c202f2a5d3217848e.mp4"
            },
            "video": "dZubIhK-Z6w",
            "preview": "https://media.rawg.io/media/stories-previews/f65/f6593df6c8df32c7f4763f9cb112a514.jpg"
        },
        "description_raw": "Rockstar Games went bigger, since their previous installment of the series. You get the complicated and realistic world-building from Liberty City of GTA4 in the setting of lively and diverse Los Santos, from an old fan favorite GTA San Andreas. 561 different vehicles (including every transport you can operate) and the amount is rising with every update. \r\nSimultaneous storytelling from three unique perspectives: \r\nFollow Michael, ex-criminal living his life of leisure away from the past, Franklin, a kid that seeks the better future, and Trevor, the exact past Michael is trying to run away from. \r\nGTA Online will provide a lot of additional challenge even for the experienced players, coming fresh from the story mode. Now you will have other players around that can help you just as likely as ruin your mission. Every GTA mechanic up to date can be experienced by players through the unique customizable character, and community content paired with the leveling system tends to keep everyone busy and engaged."
    }
  */

  [DataContract]
  public class GameDetails : Game
  {
    [DataMember(Name = "name_original")]
    public string NameOriginal { get; set; }

    [DataMember(Name = "updated")]
    public string Updated { get; set; }

    [DataMember(Name = "background_image_additional")]
    public string BackgroundImageAdditionalUrl { get; set; }

    [DataMember(Name = "website")]
    public string WebsiteUrl { get; set; }

    [DataMember(Name = "reactions")]
    public Dictionary<string, long> Reactions { get; set; }

    [DataMember(Name = "screenshots_count")]
    public int? ScreenshotsCount { get; set; }

    [DataMember(Name = "movies_count")]
    public int? MoviesCount { get; set; }

    [DataMember(Name = "creators_count")]
    public int? CreatorsCount { get; set; }

    [DataMember(Name = "achievements_count")]
    public int? AchievementsCount { get; set; }

    [DataMember(Name = "parent_achievements_count")]
    public int? ParentAchievementsCount { get; set; }

    [DataMember(Name = "reddit_url")]
    public string RedditUrl { get; set; }

    [DataMember(Name = "reddit_name")]
    public string RedditName { get; set; }

    [DataMember(Name = "reddit_description")]
    public string RedditDescription { get; set; }

    [DataMember(Name = "reddit_logo")]
    public string RedditLogo { get; set; }

    [DataMember(Name = "reddit_count")]
    public int? RedditCount { get; set; }

    [DataMember(Name = "twitch_count")]
    public int? TwitchCount { get; set; }

    [DataMember(Name = "youtube_count")]
    public int? YoutubeCount { get; set; }

    [DataMember(Name = "alternative_names")]
    public string[] AlternativeNames { get; set; }

    [DataMember(Name = "metacritic_url")]
    public string MetacriticUrl { get; set; }

    [DataMember(Name = "parents_count")]
    public int? ParentsCount { get; set; }

    [DataMember(Name = "additions_count")]
    public int? AdditionsCount { get; set; }

    [DataMember(Name = "game_series_count")]
    public int? GameSeriesCount { get; set; }

    [DataMember(Name = "esrb_rating")]
    public EsrbRating EsrbRating { get; set; }

    [DataMember(Name = "description_raw")]
    public string Description { get; set; }

    [DataMember(Name = "developers")]
    public Developer[] Developers { get; set; }

    [DataMember(Name = "publishers")]
    public Publisher[] Publishers { get; set; }
  }
}
