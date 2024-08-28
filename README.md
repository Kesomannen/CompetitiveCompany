# Competitive Company

_In an effort to combat the declining performance of its employees, the Company is introducing a form of in-house training. This new initiative pits coworkers against each other in a scrap-collecting competition across the galaxy. By the end of a few days, the lucky winner will be awarded a continued contract with the Company and a **brand new** group of coworkers!_

_Sign up now to secure your future with the Company!_

---

This mod is a rewrite and continuation of [Competitive Company by agmas](https://thunderstore.io/c/lethal-company/p/agmas/CompetitiveCompany/).
It features a new game mode, where teams of players compete to collect the most amount of scrap within a set number of days. Credits are automatically added as you collect scrap and there's no quota to worry about.

> [!IMPORTANT]
> All players in the lobby are required to have the mod installed!

> [!NOTE]
> The gamemode cannot be toggled off; i.e. to play the base game you have to disable the whole mod.

## Recommended Mods

This mod is intentionally designed to be somewhat minimal. To improve your experience, consider also using any/all of the following mods. These are required by everyone in the lobby unless stated otherwise.

- [MoreCompany](https://thunderstore.io/c/lethal-company/p/notnotnotswipez/MoreCompany/) lets you have up to 32 players in a lobby and customize your player's appearance with cosmetics.
- [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/) adds an in-game menu for configuring mods, including this one. Does not need to be installed by everyone.
- [LethalCompanyVariables](https://thunderstore.io/c/lethal-company/p/AMRV/LethalCompanyVariables/) adds a ton of configuration options, including increasing the amount of scrap and monsters in the game to suit larger lobbies. Depending on your settings, this may not need to be installed by everyone.
- [BetterEmotes](https://thunderstore.io/c/lethal-company/p/KlutzyBubbles/BetterEmotes/) adds more emotes to the game, which can be used in the end of match cutscene (see the Client config section).
- Custom moons with [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/). Vanilla moons can prove too small and linear for this gamemode, especially with a large number of players. Here are some recommended mods made by the community:
  - TODO

## Terminal Commands

Teams are managed through custom terminal commands:

| Command                  | Description                                                                                                                                                   |
| ------------------------ |---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `join <team> [player]`   | Join a team. The host can join other players by specifying their username as the last argument.                                                               |
| `list-teams`             | Lists all teams and their scores.                                                                                                                             |
| `create-team <name>`     | Creates a new team with the given name. By default, only the host can use this.                                                                               |
| `delete-team <team>`     | Deletes the specified team. By default, only the host can use this.                                                                                           |
| `set-team-name <name>`   | Changes the name of your current team. The max length for team names is 64 characters.                                                                        |
| `set-team-color <color>` | Changes the color of your current team. The color can be one of red, green, blue, yellow, cyan, magenta, white or black, or a custom hex code (e.g. #FF0000). |
| `settings`               | View the current match settings.                                                                                                                              |
| `scramble-teams`         | Randomly assigns players to teams. Only the host can use this.                                                                                                |

This information can also be found in-game by running the `other` command.

## Configuration

Most configuration is done via a normal `.cfg` file. Every option except those in the `Client` section are server-side and can only be set by the host. The options take effect immediately, meaning you can configure match settings on the fly with a mod like [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/).

Additionally you can define a custom set of default teams that will be created each time you start a lobby. This is done via a `default_teams.json` file in the config folder. Any players you specify will be put into that team after joining. For example:
```json
[
  {
    "name": "Milk enjoyers",
    "color": "white",
    "players": [
      "Kesomannen"
    ]
  },
  {
    "name": "Orange juice fans",
    "color": "#e0701b"
  },
  {
    "name": "Water drinkers",
    "color": "aqua",
    "players": [
      "agmas",
      "frostycirno"
    ]
  }
]
```

## Planned Features

- [ ] Controlled lever pulling
  - Make it so once a team pulls the lever, the ship leaves within a configurable time frame instead of immediately. The team who pulls will go into spectator mode while the others have to scramble to get on the ship.
- [ ] Spectator role
- [ ] Saving & loading
- [ ] Controlled moon selection
  - Make it so only one team can select the next moon to go to. This will be configurable to either be the losers pick or cycle through the teams.
- [ ] Better team management
  - Add a new ship object to view and manage the state of the game instead of using the terminal.

## Using the API

This mod exposes an API for other mods to interact with.

1. Download the mod from Thunderstore.
2. Copy the `CompetitiveCompany.dll` and `CompetitiveCompany.xml` files to a folder in your project, for example `lib`.
3. Add the following to your .csproj file:

```xml
<ItemGroup>
  <Reference Include="CompetitiveCompany" HintPath="relative/path/to/CompetitiveCompany.dll" />
</ItemGroup>
```

4. Add a BepInDependency attribute to your plugin class:

```csharp
[BepInDependency(CompetitiveCompany.Plugin.Guid)]
public class MyPlugin : BaseUnityPlugin {
    ...
}
```

Here's a couple of quick examples:

```csharp
using CompetitiveCompany.Game;

// item that shows the position of all enemies when activated
public class XRayItem : GrabbableObject {
    public override void ItemActivate(bool used, bool buttonDown = true) {
        if (!used) return;

        var localTeam = Player.Local.Team;
        if (localTeam == null) return;

        foreach (var player in Session.Current.Players) {
            if (player.Team == localTeam) continue;

            var position = player.transform.position;
            var username = player.Controller.playerUsername;
            Debug.Log($"Enemy {username} is at {position}");

            // draw an indicator at the enemy's position
        }
    }
}
```

```csharp
using CompetitiveCompany.Game;

// only allow combat inside of the facility
[BepInPlugin(...)]
public class Plugin : BaseUnityPlugin {
    void Awake() {
        Session.OnSessionStarted += OnSessionStarted;
    }

    void OnSessionStarted() {
        Session.Current.Combat.AddPredicate((attacker, victim) => {
            if (!attacker.Controller.isInsideFactory) {
                return DamagePredicateResult.Deny("PvP is only allowed inside the facility");
            }

            return DamagePredicateResult.Allow();
        });
    }
}
```

## Contributing

Make sure you have the following installed:

- [Git](https://git-scm.com/)
- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/6.0)
- [UnityNetcodePatcher CLI](https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#cli)
- [Thunderstore CLI]() (only needed for the `Release` configuration)

---

1. Clone the repository
   ```sh
   git clone https://github.com/Kesomannen/CompetitiveCompany.git

2. Download the following mods from Thunderstore and place their DLLs in the `lib` folder:

   - [LethalAPI.Terminal](https://thunderstore.io/c/lethal-company/p/LethalAPI/LethalAPI_Terminal/)
   - [LethalCompanyInputUtils](https://thunderstore.io/c/lethal-company/p/Rune580/LethalCompany_InputUtils/)
   - [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/)
   - [LethalConfig](https://thunderstore.io/c/lethal-company/p/AinaVT/LethalConfig/)
   - [Runtime_Netcode_Patcher](https://thunderstore.io/c/lethal-company/p/Ozone/Runtime_Netcode_Patcher/)

3. Add the `MMHOOK_Assembly-CSharp.dll` to the `lib` folder. This is generated by the [HookGenPatcher](https://thunderstore.io/c/lethal-company/p/Evaisa/HookGenPatcher/) mod on startup and located in the `BepInEx/plugins/MMHOOK` folder.

4. Build the project with
   ```sh
   dotnet build
   ```
   The mod will be built to `bin/Debug/netstandard2.1/CompetitiveCompany.dll`.

> [!IMPORTANT]
> Don't forget to include the asset bundle and FlowTween.dll in the same folder as the mod's DLL before running the game. The unity project to build the asset bundle is not included in the repository (yet).
