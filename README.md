# Competitive Company 2

_As an effort to combat the declining performance of its employees, the Company is introducing a new form of in-house training. This innovative initiative pits coworkers against each other in a scrap-collecting competition throughout the galaxy. By the end of a few days, the lucky winner will be awarded a continued contract with the Company **and** a brand new group of coworkers!_

_Sign up now to secure your future with the Company!_

---

This mod is a rewrite and continuation of [Competitive Company](https://thunderstore.io/c/lethal-company/p/agmas/CompetitiveCompany/) by agmas.
It features a new game mode, where teams of players compete to collect the most amount of scrap within a set number of days. Credits are automatically added as you collect scrap and there's no quota to worry about.

> [!IMPORTANT]
> All players are required to have the mod installed!

> [!NOTE]
> The gamemode cannot be toggled off; i.e. to play the base game you have to disable the mod.

## Recommended Mods

This mod is intentionally designed to be minimal. To improve your experience, consider also using any/all of the following mods. These are required by everyone in the lobby unless stated otherwise.

- [MoreCompany](https://thunderstore.io/package/notnotnotswipez/MoreCompany/) lets you have up to 32 players in a lobby and customize your player's appearance with cosmetics.
- [LethalConfig](https://thunderstore.io/package/lethal-company/p/LethalConfig/) adds an in-game menu for configuring mods, including this one. Does not need to be installed by everyone.
- [LethalCompanyVariables](https://thunderstore.io/c/lethal-company/p/AMRV/LethalCompanyVariables/) adds a ton of configuration options, including increasing the amount of scrap and monsters in the game to suit larger lobbies. Depending on your settings, this may not need to be installed by everyone.
- Custom moons with [LethalLevelLoader](https://thunderstore.io/package/lethal-company/p/LethalLevelLoader/). Vanilla moons can prove too small and linear for this gamemode, especially with a large number of players. Here are some recommended mods made by the community:
  - TODO

## Terminal Commands

Teams are managed through custom terminal commands:

| Command                  | Description                                                                                            |
| ------------------------ | ------------------------------------------------------------------------------------------------------ |
| `join [player] <team>`   | Join a team. The host can join other players by specifying their username as the second argument.      |
| `list-teams`             | Lists all teams and their scores.                                                                      |
| `create-team <name>`     | Creates a new team with the given name. By default, only the host can use this.                        |
| `delete-team <team>`     | Deletes the specified team. By default, only the host can use this.                                    |
| `set-team-name <name>`   | Changes the name of your current team. The max length for team names is 64 characters.                 |
| `set-team-color <color>` | Changes the color of your current team. The color must be a valid hex color code, e.g. "#FF0000" (red) |
| `spectate`               | Leave your current team and become a spectator.                                                        |

This information can also be found in-game by running the `other` command.

## Configuration

All configuration is done via a normal `.cfg` file. Every option except those in the `Client` section are server-side and can only be set by the host. The options take effect immediately, meaning you can configure match settings on the fly with a mod like [LethalConfig](https://thunderstore.io/package/lethal-company/p/LethalConfig/).

## Planned Features

- [ ] Saving
- [ ] Controlled lever pulling
  - Make it so once a team pulls the lever, the ship leaves within a configurable time frame instead of immediately. The team who pulls will go into spectator mode while the others have to scramble to get on the ship.
- [ ] Controlled moon selection
  - Make it so only one team can select the next moon to go to. This will be configurable to either be the losers pick or cycle through the teams.
- [ ] Better team management
  - Add a new ship object to view and manage the state of the game instead of using the terminal.

## Using the API

This mod exposes an API for other mods to interact with. All classes and methods are documented with XML comments. Here's a couple of quick examples:

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

---

1. Clone the repository
   ```sh
   git clone https://github.com/Kesomannen/CompetitiveCompany.git
   ```
2. Go your Lethal Company installation, then navigate to `Lethal Company_Data/Managed` and copy the following DLLs into the `lib` folder of the repository:

   - `Assembly-CSharp`
   - `Unity.Collections`
   - `Unity.InputSystem`
   - `Unity.Netcode.Components`
   - `Unity.Netcode.Runtime`
   - `Unity.RenderPipelines.HighDefinition.Runtime`
   - `Unity.TextMeshPro`
   - `UnityEngine.UI`

3. Download the following mods from Thunderstore and place their DLLs in the same folder:

   - [LethalAPI.Terminal](https://thunderstore.io/c/lethal-company/p/LethalAPI/LethalAPI_Terminal/)
   - [LethalCompanyInputUtils](https://thunderstore.io/c/lethal-company/p/Rune580/LethalCompany_InputUtils/)
   - [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/)

4. Finally you need to add the `MMHOOK_Assembly-CSharp.dll` to the `lib` folder. This is generated by the [HookGenPatcher](https://thunderstore.io/c/lethal-company/p/Evaisa/HookGenPatcher/) mod on startup and located in the `BepInEx/plugins/MMHOOK` folder.

5. Build the project
   ```sh
   dotnet build
   ```
   The mod will be built to `bin/Debug/netstandard2.1/CompetitiveCompany.dll`.

> [!NOTE]
> Remember to include the mod's asset bundle in the same foler as the DLL. Unfortunately the unity project to build the asset bundle is not included in this repository (yet).
