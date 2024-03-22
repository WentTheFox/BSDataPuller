# DataPuller
Gathers data about the current map you are playing to then be sent out over a websocket for other software to use, e.g. A web overlay like [BSDP-Overlay](https://github.com/ReadieFur/BSDP-Overlay). This mod works with multi PC setups!

## Installation:
To install this mod, download the [latest version](./releases/latest) and place the `DataPuller.dll` into your mods folder. Make sure to also have any of the dependencies listed below installed too.
### Dependencies, these can all be found on the [Mod Assistant](https://github.com/Assistant/ModAssistant) app:
In order for this mod to function properly you must have installed the following mods:
- [BSIPA ^4.3.2](https://github.com/bsmg/BeatSaber-IPA-Reloaded)
- [BeatSaverSharp ^3.4.5](https://github.com/Auros/BeatSaverSharper)
- [WebsocketSharp ^1.0.4](assets/websocket-sharp-1.0.4.zip)
- [SongCore ^3.12.2](https://github.com/Kylemc1413/SongCore)
- [SongDetailsCache ^1.2.2](https://github.com/kinsi55/BeatSaber_SongDetails)
- [SiraUtil ^3.1.6](https://github.com/Auros/SiraUtil)

## Overlays:
There are few overlays that I know of at the moment that work with this mod but here are some:
| Overlay | Creator |
| --- | --- |
| [BSDP-Overlay](https://github.com/ReadieFur/BSDP-Overlay) | ReadieFur |
| [Freakylay](https://github.com/UnskilledFreak/Freakylay) | UnskilledFreak |
| [HyldraZolxy](https://github.com/HyldraZolxy/BeatSaber-Overlay) | HyldraZolxy |
| [Beat-Saber-Overlay](https://github.com/DJDavid98/Beat-Saber-Overlay) | DJDavid98 |

## Project status:
The [original repository](https://github.com/ReadieFur/BSDataPuller) has not been updated for over a year (at the time of writing) and appears to have been abandoned for all intents and purposes.

This fork is actively maintained by me ([DJDavid98](https://github.com/DJDavid98)) and new features/fixes are added on request to the best of my abilities.

Don't hesitate to open an issue or reach out in case you feel there is something that could be added/improved.


## Output data:
This mod outputs quite a bit of data to be used by other mods and overlays. Here is some of the data that the mod exposes:
- Map info:
	- Hash
	- Song name
	- Song sub name
	- Song author
	- Mapper
	- BSR key
	- Cover image
	- Length
	- Time elapsed
- Difficulty info:
	- Map type
	- Difficulty
	- PP
	- Star
	- BPM
	- NJS
	- Modifiers
	- Pratice mode
- Level info:
	- Paused
	- Failed
	- Finished
	- Quit
- Score info:
	- Score
	- Score with modifiers
	- Previous record
	- Full combo
	- Combo
	- Misses
	- Accuracy
	- Block hit score
	- Health

And more!

## Developer documentation:
### Obtaining the data via the Websocket:
Data is broadcasted over an unsecure websocket (plain `ws`) that runs on port `2946`, the path to the data is `/BSDataPuller/<TYPE>`.  
The reason for the use of an unsecure websocket is because it is pratically impossible to get a verified and signed SSL certificate for redistribution, that would break the whole point of SSL.  
Each endpoint will send out a JSON object, check [Data Types](#data-types) for the specific data that each endpoint sends out.

### Obtaining the data via the C#:
It is possible to use the data that this mod exposes within your own mod if you wish to do so.  
To get started, add the [latest version](./releases/latest) of the mod to your project as a reference.  
Data types can be accessed within the `DataPuller.Data` namespace.  
All data types extend the `AData` class which contains an `OnUpdate` event that can be subscribed to that is fired whenever the data is updated.  
Check [Data Types](#data-types) for the specific data that each endpoint sends out.

### Data types:
I will format each entry in the following way:
```
<TYPE>
	<DESCRIPTION>
	<LOCATION>
	<OBJECT>
		///<COMMENT>
		<TYPE> <NAME> = <DEFAULT_VALUE>;
```
All data types contain the following properties:
```cs
///The time that the data was serialized.
long UnixTimestamp;
```
Below is a list of all the specific data types:
<details>
<summary style="font-weight: 600">MapData</summary>
Description: Contains data about the current map and mod.  
Type: `class`  

| Method | Location |
| --- | --- |
| Websocket | `/BSDataPuller/MapData` |
| C# | `DataPuller.Data.MapData` |

This data gets updated whenever:
- The map is changed
- A level is quit/paused/failed/finished

```cs
//====LEVEL====
///This can remain false even if LevelFailed is true, when Modifiers.NoFailOn0Energy is true.
bool LevelPaused = false;

bool LevelFinished = false;

bool LevelFailed = false;

bool LevelQuit = false;

//====MAP====
///The hash ID for the current map.
///null if the hash could not be determined (e.g. if the map is not a custom level).
string? Hash = null;

///The predefined ID for the current map.
///null if the map is not a built-in level.
string? LevelID = null;

///The name of the current map.
string SongName = "";

///The sub-name of the current map.
string SongSubName = "";

///The author of the song.
string SongAuthor = "";

///The mapper of the current chart.
string Mapper = "";

///The BSR key of the current map.
///null if the BSR key could not be obtained.
string? BSRKey = null;

///The cover image of the current map.
///null if the cover image could not be obtained.
string? CoverImage = null;

///The duration of the map in seconds.
int Duration = 0;

//====DIFFICULTY====
///The type of map.
///i.e. Standard, 360, OneSaber, etc.
string MapType = "";

///The standard difficulty label of the map.
///i.e. Easy, Normal, Hard, etc.
string Difficulty = "";

///The custom difficulty label set by the mapper.
///null if there is none.
string? CustomDifficultyLabel = null;

///The beats per minute of the current map.
int BPM = 0;

///The note jump speed of the current map.
double NJS = 0;

///The modifiers selected by the player for the current level.
///i.e. No fail, No arrows, Ghost notes, etc.
Modifiers Modifiers = new Modifiers();

///The score multiplier set by the users selection of modifiers.
float ModifiersMultiplier = 1.0f;

bool PracticeMode = false;

///The modifiers selected by the user that are specific to practice mode.
PracticeModeModifiers PracticeModeModifiers = new PracticeModeModifiers();

///The approximate amount of performance points this map is worth (legacy value for backwards-compatibility)
///0 if the map is unranked or the value was undetermined.
double PP = 0;

///ScoreSaber stars (legacy value for backwards-compatibility)
///0 if the value was undetermined.
double Star = 0;

///Ranked state for the current map.
///0 if the value was undetermined.
SRankedState RankedState = new SRankedState();

///Song rating percentage on BeatSaver (0-100)
///0 if the value was undetermined.
float Rating = 0;

///The color scheme for the currently playing map.
SColorScheme ColorScheme = new SColorScheme();

//====MISC====
string GameVersion = ""; //Will be the current game version, e.g. 1.20.0

string PluginVersion = ""; //Will be the current version of the plugin, e.g. 2.1.0

bool IsMultiplayer = false;

///The previous local record set by the player for this map specific mode and difficulty.
///0 if the map variant hasn't never been played before.
int PreviousRecord = 0;

///The BSR key fore the last played map.
///null if there was no previous map or the previous maps BSR key was undetermined.
///This value won't be updated if the current map is the same as the last.
string? PreviousBSR = null;
```

##### Modifiers
This is a sub-object of `MapData` and it doesn't extend the `AData` class, there is no endpoint for this type.  
Type: `class`
```cs
bool NoFailOn0Energy = false;
bool OneLife = false;
bool FourLives = false;
bool NoBombs = false;
bool NoWalls = false;
bool NoArrows = false;
bool GhostNotes = false;
bool DisappearingArrows = false;
bool SmallNotes = false;
bool ProMode = false;
bool StrictAngles = false;
bool ZenMode = false;
bool SlowerSong = false;
bool FasterSong = false;
bool SuperFastSong = false;
```

##### PracticeModeModifiers
This is a sub-object of `MapData` and it doesn't extend the `AData` class, there is no endpoint for this type.  
Type: `class`
```cs
float SongSpeedMul;
bool StartInAdvanceAndClearNotes;
float SongStartTime;
```


##### SColorScheme
This is a sub-object of `MapData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
/// The color of the primary (typically left) saber, and by extension the notes.
SRGBAColor? SaberAColor = null; 
/// The color of the secondary (typically right) saber, and by extension the notes.
SRGBAColor? SaberBColor = null;
/// The color of the walls.
SRGBAColor? ObstaclesColor = null;
/// The primary enviornment color.
SRGBAColor? EnvironmentColor0 = null;
/// The secondary enviornment color.
SRGBAColor? EnvironmentColor1 = null;
/// The primary enviornment boost color, typically se to the same as the primary environment color.
SRGBAColor? EnvironmentColor0Boost = null;
/// The secondary enviornment boost color, typically se to the same as the secondary environment color.
SRGBAColor? EnvironmentColor1Boost = null;
```

##### SRGBAColor
This is a sub-object of `MapData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
/// Hexadeciaml RGB color code including the # symbol
string HexCode = "#000000";
/// 0 to 255
int Red = 0;
/// 0 to 255
int Green = 0;
/// 0 to 255
int Blue = 0;
/// 0.0  to 1.0
float Alpha = 0.0;
```

##### SRankedState
This is a sub-object of `MapData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
/// Is map ranked on any leaderboards
bool Ranked = false;
/// Is map qualified on any leaderboards
bool Qualified = false;
/// Is map qualified on BeatLeader
bool BeatleaderQualified = false;
/// Is map qualified on ScoreSaber
bool ScoresaberQualified = false;
/// Is map ranked on BeatLeader
bool BeatleaderRanked = false;
/// Is map ranked on ScoreSaber
bool ScoresaberRanked = false;
///BeatLeader stars
///0 if the value was undetermined.
double BeatleaderStars = 0;
///ScoreSaber stars
///0 if the value was undetermined.
double ScoresaberStars = 0;
```

</details>

<details>
<summary style="font-weight: 600">LiveData</summary>
Description: Contains data about the player status within the current map.  
Type: `class`

| Method | Location |
| --- | --- |
| Websocket | `/BSDataPuller/LiveData` |
| C# | `DataPuller.Data.LiveData` |

This data gets updated whenever:
- The players health changes
- A block is hit or missed
- The score changes
- 1 game second passes (this varies depending on the speed multiplier)

```cs
//====SCORE====
///The current raw score.
int Score = 0;

///The current score with the player selected multipliers applied.
int ScoreWithMultipliers = 0;

///The maximum possible raw score for the current number of cut notes.
int MaxScore = 0;

///The maximum possible score with the player selected multipliers applied for the current number of cut notes.
int MaxScoreWithMultipliers = 0;

///The string rank label for the current score.
///i.e. SS, S, A, B, etc.
string Rank = "SSS";

bool FullCombo = true;

///The total number of notes spawned since the start position of the song until the current position in the song.
int NotesSpawned = 0;

///The current note cut combo count without error.
///Resets back to 0 when the player: misses a note, hits a note incorrectly, takes damage or hits a bomb.
int Combo = 0;

///The total number of missed and incorrectly hit notes since the start position of the song until the current position in the song.
int Misses = 0;

double Accuracy = 100;

///The individual scores for the last hit note.
SBlockHitScore BlockHitScore = new SBlockHitScore();

double PlayerHealth = 50;

///The colour of note that was last hit.
///ColorType.None if no note was previously hit or a bomb was hit.
ColorType ColorType = ColorType.None;

///The note cut direction, also known as rotation.
///NoteCutDirection.None if no note was previously hit.
NoteCutDirection CutDirection = NoteCutDirection.None;

//====MISC====
///The total amount of time in seconds since the start of the map.
int TimeElapsed = 0;

///The event that caused the update trigger to be fired.
ELiveDataEventTriggers EventTrigger = ELiveDataEventTriggers.Unknown;
```
##### SBlockHitScore
This is a sub-object of `LiveData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
///0 to 70.
int PreSwing = 0;
///0 to 30.
int PostSwing = 0;
///0 to 15.
int CenterSwing = 0;
```

##### ColorType
This is a sub-object of `LiveData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `enum`
```cs
ColorA = 0,
ColorB = 1,
None = -1
```

##### ELiveDataEventTriggers
This is a sub-object of `LiveData` and it doesn't extend the `AData` class, there is no endpoint for this type.  
Type: `enum`
```cs
Unknown = 0,
TimerElapsed = 1,
NoteMissed = 2,
EnergyChange = 3,
ScoreChange = 4
```

##### NoteCutDirection
This is a sub-object of `LiveData` and it doesn't extend the `AData` class, there is no endpoint for this type.  
Type: `enum`
```cs
Up = 0,
Down = 1,
Left = 2,
Right = 3,
UpLeft = 4,
UpRight = 5,
DownLeft = 6,
DownRight = 7,
Any = 8,
None = 9
```

</details>


<details>
<summary style="font-weight: 600">ModData</summary>
Description: Contains data about the enabled mods.  
Type: `class`  

| Method | Location                  |
| --- |---------------------------|
| Websocket | `/BSDataPuller/ModData`   |
| C# | `DataPuller.Data.ModData` |

This data gets updated whenever:
- the game first starts
- the enabled state of any mod changes

```cs
///List of metadata for all enabled mods
List<SPluginMetadata> EnabledPlugins = new List<SPluginMetadata>();
```

##### SPluginMetadata
This is a sub-object of `ModData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
///Mod name
string Name = '';
///Mod version "major.minor.patch"
string Version = '';
///Mod author
string Author = '';
///Mod description
string Description = '';
///Mod homepage URL
string HomeLink = '';
///Mod source code URL
string SourceLink = '';
///Mod donation URL
string DonateLink = '';
```

</details>

<details>
<summary style="font-weight: 600">PartyData</summary>
Description: Contains data about the local leaderboard in Party mode.  
Type: `class`

| Method    | Location                    |
|-----------|-----------------------------|
| Websocket | `/BSDataPuller/PartyData`   |
| C#        | `DataPuller.Data.PartyData` |

This data gets updated whenever:
- the game adds a new score to the local leaderboard
- the song selection in the menu changes while in Party mode

```cs
///ID of the leaderboard
string? LeaderboardID = null;
///Type of the leaderboard
string? LeaderboardType = null;
///List of scores for the specific leaderboard
List<SLocalLeaderboardScore> Scores = new List<SLocalLeaderboardScore>();
```

##### SLocalLeaderboardScore
This is a sub-object of `PartyData` and it doesn't extend the `AData` class, there is no endpoint for this type.
Type: `struct`
```cs
///Player name
string PlayerName = '';
///Player's score
int Score = 0;
///UNIX timestamp (in seconds) when the score was recorded
long Timestamp = 0;
///Whether the play-through had a full combo (no mistakes)
bool FullCombo = false;
```

</details>
