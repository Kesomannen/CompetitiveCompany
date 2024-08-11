# Competitive Company 2

_As an initiative to combat the declining performance of its employees, the Company is introducing a new form of in-house training. This groundbreaking procedure pits coworkers against each other in a scrap-collecting competition throughout the galaxy. By the end of a few days, the lucky winner will be awarded a continued contract with the Company **and** a brand new group of coworkers!_

_Sign up now to secure your future with the Company!_

---

This mod is a rewrite and continuation of [Competitive Company](https://thunderstore.io/c/lethal-company/p/agmas/CompetitiveCompany/) by agmas.
It features a new game mode, where teams of players compete to collect the most amount of scrap within a set number of days. Credits are automatically added as you collect scrap and there's no quota to worry about.

> [!IMPORTANT]
> All players are required to have the mod installed.

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

All configuration is done via a normal `.cfg` file. All options except those in the `Client` section are server-side and can only be set by the host. None of the options require a restart to take effect, meaning you can configure match settings on the fly with a mod like [LethalConfig](https://thunderstore.io/package/lethal-company/p/LethalConfig/).

## Planned Features

- [ ] Controlled moon selection
  - Make it so only one team can select the next moon to go to. This will be configurable to either be the losers pick or cycle through the teams.
- [ ] Better team management
  - Add a new ship object to view and manage the state of the game instead of using the terminal.
