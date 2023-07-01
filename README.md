# Peasant Revenge
 PeasantRevenge is a Mount&Blade II mod what aims to implement peasant revenge after village has been raided. 
 Player and AI heroes, who captured the criminal, can now allow notable peasant to behead the criminal.   
 ## Features
 ### Dialogues for notable peasants and AI lords: 
- Dialogues for notable peasant when AI or player has captured the criminal hero.
- Dialogues for notable peasant for mod configuration purpose:
   - Enable or disable notable peasant parties,
   - Enable or disable "help neutral village" menu option.
- Dialogue for notable peasant to encourage them to initiate the peasant revenge.
### AI behavior to handle peasant revenge
- The peasant revenge can happen after hero committed to raid the village and the hero became a prisoner, and any notable village peasant has correct hero traits.
- The notable peasant (revenger) will initiate dialog with a mobile party what has the criminal as a prisoner. And will demand criminal's head off or pay the reparation (reparation is scaled to hearts count of the village).
- AI may not allow to kill the criminal due to relations or traits.
- AI will try to find, who could pay the reparation (the criminal, criminal's clan members, criminal's relatives, friends...).
- Heroes can kill notable peasants, when heroes have certain traits and does not want to pay reparation to notable peasant (Player cannot ask for "lost ransom" up to game version 1.1.5).
- Mobile party may demand "lost ransom" after the criminal has been killed.
- Notables, who have active issue quests, do not participate in peasant revenge.
### Heroes relations and traits due to peasant revenge
- Traits
  - This mod does not add any additional hero traits exp changes during peasant revenge (up to game version 1.1.5).When paying the reparation heroes will gain charisma and generosity exp (default game process).
  - Hero (player only) can change notable peasant traits via dialog, when encouraging them to initiate the peasant revenge.
- Relations
  - Mod try to care of relations between the criminal, notable peasant, mobile party hero, other involved heroes (who paid "lost ransom", settlement owner).
  - Player does not get relations penalties if peasant kill the criminal heroes.
## Configuration:
- You can tweek some configuration values to your liking.
Configuration values are in the file PeasantRevengeModCfg xml, which is created after you create the game, and reloaded after you load the game.
- Mod will not write to this file if "CfgVersion" is higher than or equal to PeasantRevenge dll file version (your changes to this file will retain).
- Mod does not save any progress (however game will save notable peasant mobile parties, but without revenge data. The mod will try to disband them, after game was loaded), so game will forget about past raid criminal heroes after game reload (it is done intentionally)
## Uninstalling
If you enabled notable peasant parties, disable them and wait (peasantRevengeTimeoutInDays days) until parties are removed and save game, before you uninstall the mod.
## Known Bugs:
If you enable notable parties, sometimes village will get 1 more notable for a short time, after revenge mobile party is disbanded. This is because other TW module adds/removes them (more a feature than bug.).
If you enable notable parties, and remove mod later without waiting for revenge parties to disband and If you attack not removed party you will get crash (solution: wait until AI kills them off).
## Compatibility:
- Should be Save file compatible (Can be loaded/unloaded any time. If notable parties are enabled - let AI to kill them off)).
- Mod Diplomacy: Diplomacy mod does not allow kill heroes, if killer hero is without mobile party.
Please load the Diplomacy mod before this mod (The mod will automatically set allowLordToKillMessenger and allowPeasantToKillLord values to "false" in the PeasantRevengeModCfg xml file).
- For version 1.2.0 and higher: mod has item "Remains of a corpse". It is converted to "Trash item" by game design after mod removal.

## Source code
GitHub: https://github.com/arunasradv/PeasantRevenge.git