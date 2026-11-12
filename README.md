# Peasant Revenge

Peasant Revenge is a Mount & Blade II mod that aims to implement peasant revenge after a village has been raided. Player and AI heroes who capture the criminal can allow a notable peasant to behead them.

This mod uses the original game quest system, so the peasants will not take revenge until the player accepts the revenge quest.

## Status

Finishing basic revenge handling (most of it works...)
Add advanced features: hero accusations, corner cases (betray the peasant—help counter the revenge with the criminal, such as ambushing the revenger), kingdoms and clans, and town quests to make peasants vengeful or passive.
Use kingdom policies for official revenge handling.
From game version 1.5, add something that could be compatible with the Blood Feud system.
Add a quest to counter the rumors about a hero being killed by a peasant while the hero was a prisoner.
("The Tim had a noble prisoner, and the prisoner died under some mysterious circumstances...")
Add rumors in taverns about peasant revenge quests.

## Contributors

## Features

(Work in progress)

### Dialogues for notable peasants and AI lords

(Work in progress)

#### - Dialogues for notable peasants when an AI or player has captured the criminal hero

(Work in progress)

#### - Dialogues for notable peasants for mod configuration purposes

(Work in progress)

#### - Other dialogues

(Work in progress)

### AI behavior to handle peasant revenge

(Work in progress)

### Hero relations and traits due to peasant revenge

(Work in progress)

## Configuration

(Work in progress)

## Uninstalling

(Work in progress)

## Known Bugs

(Work in progress)

## Compatibility

(Work in progress)

## Source code

[GitHub](https://github.com/arunasradv/PeasantRevenge.git)

## Configuration parameters

(Work in progress)

# Modding with Linux

With Steam, do the following:

After you install M&B II, set the following in the Compatibility options:

- Check: Force the use of a specific Steam Play compatibility tool.
- Select: Proton 10.0-2 (beta)

Start the game, then exit the game.

- With Protontricks (Flatpak), install the .NET 6.0 Runtime.
- For Visual Studio Code, install the .NET 6.0 Runtime for Linux from the Microsoft page.
- Use the `dev_linux` branch for the Visual Studio Code solution.
- You will need to change all incorrect paths in the files (e.g., the username) and probably fix the Visual Studio Code environment.
- Build the project (`PeasantRevenge.sln`).
- Commit local branch changes.
- Use `update_to_new_versions.py` to modify files with the new assembly and game version IDs (update `AssemblyVersion` and `SupportedGameVersion`).
- Commit everything to the remote repository.
- Use `postbuild.sh` to copy all mod files into M&B Modules.

Play the game.
