# Peasant Revenge
 PeasantRevenge is a Mount&Blade II mod what aims to implement peasant revenge after village has been raided. 
 Player and AI heroes, who captured the criminal, can now allow notable peasant to behead the criminal.   
## Features

### Dialogues for notable peasants and AI lords: 
#### - Dialogues for notable peasant when AI or player has captured the criminal hero.
 1. If player captured the criminal,the dialog options are: 
    - criminal will die (the criminal will be executed);
    - criminal will pay the reparation;
    - ignore peasant demands;
    - ask the criminal about the village raid. The criminal will blame other prisoner for the crime. More options will be available:
      - agree with the criminal (the blaimed hero will be executed);
      - disagree with the criminal (the criminal will be executed);
      - execute both (the blaimed hero and the criminal);
      - (**not implemented**) do not allow the execution due to disagreements (criminal and blamed hero are not executed)
 2. When player is captured as a criminal,the dialog options are: 
     - asking the hero, what if he will not pay the reparation (depending on AI hero traits and relations, hero will tell if player will be executed or not);
     - (**not implemented**) player try to persuede the hero;
     - ask other friendly AI for a favour to pay the reparation (the other hero will pay the reparation);
     - agree to pay the reparation;
     - do not agree to pay the reparation (depending on AI hero traits and relations, player will be executed or spared);
     - blame other prisoner for the crime. More options will be available:
       - confirm the blame (blamed hero will be executed);
	     - (**not implemented**) Depending on AI hero traits and relations, AI will decide, if blamed hero or/and criminal will be executed, or will not allow the execution due to disagreements);
	   - change the mind (dialog will return to previous options);
 3. When hero (from player clan or kingdom) caught the criminal, who has been raiding player's clan or kingdom village. Notable peasant dialog options are:
     - agree to pay the reparation to notable peasant (the criminal is not executed);
	 - disagree to pay the reparation and demand to spare the criminal (the criminal is not executed);
	 - disagree to pay the reparation (the criminal is executed);
	 - allow to execute both (balmed hero and the criminal);
     - kill the notable peasant;
	 - (**not implemented**) hero let the other lord (AI will search other hero, who could save the criminal) to decide criminal's fate.
- Hero, who had the criminal hero as a prisoner, may demand "lost ransom". Dialog options are:
   - disagree to pay "lost ransom";
   - agree to pay "lost ransom";
   - kill the soldier, who asked for "lost ransom";
 #### - Dialogues for notable peasant for mod configuration purpose:
  1. Enable or disable notable peasant parties,
  2. Enable or disable "help neutral village" menu option.
  3. Dialogue for notable peasant to encourage them to initiate the peasant revenge. Notable peasant dialog options are
     - execute the notable peasant (game will replace the notable peasant with a new one);
     - teach the notable peasant (use player's traits to change notable peasant's traits);
     - bribe the notable peasant (use money to change notable peasant's traits);
	   - (**not implemented**) bribing effect should be temporary and depend on traits and relations.

### AI behavior to handle peasant revenge
- The peasant revenge can happen after hero committed to raid the village and the hero became a prisoner, and any notable village peasant has correct hero traits.
- The notable peasant (revenger) will initiate dialog with a mobile party what has the criminal as a prisoner. And will demand criminal's head off or pay the reparation (reparation is scaled to hearts count of the village).
- AI may not allow to kill the criminal due to relations or traits.
- AI will try to find, who could pay the reparation (the criminal, criminal's clan members, criminal's relatives, friends...).
- Heroes can kill notable peasants, when heroes have certain traits and does not want to pay reparation to notable peasant (Player cannot ask for "lost ransom" up to game version 1.1.5).
- Mobile party may demand "lost ransom" after the criminal has been killed.
- Notables, who have active issue quests, do not participate in peasant revenge.
- Mobile parties without leader hero, do not participate in peasant revenge.
### Heroes relations and traits due to peasant revenge
- Traits
  - This mod does not add any additional hero traits exp changes during peasant revenge (up to game version 1.1.5).When paying the reparation heroes will gain charisma and generosity exp (default game process).
  - Hero (player only) can change notable peasant traits via dialog, when encouraging them to initiate the peasant revenge.
- Relations
  - Mod try to care of relations between the criminal, notable peasant, mobile party hero, other involved heroes (who paid "lost ransom", settlement owner).
  - Player does not get relations penalties if notable peasant behead the criminal hero.
## Configuration:
- You can tweek some configuration values to your liking.
Configuration values are in the file PeasantRevengeModCfg xml, which is created after you create the game, and reloaded after you load the game. And file is at PeasantRevenge dll file location.
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
[GitHub](https://github.com/arunasradv/PeasantRevenge.git)

## Configuration parameters
  - CfgVersion - mod will not update the configuration, if this version is higher than PeasantRevenge dll file version (first digits).
  - enableRevengerMobileParty - disable or enable revenger parties (true - revenger party will spawn).
  - enableHelpNeutralVillageAndDeclareWarToAttackerMenu -
  - ReparationsScaleToSettlementHearts -
  - relationChangeAfterReparationsReceived -
  - relationChangeWhenCannotPayReparations -
  - relationChangeWhenCriminalRefusedToPayReparations -
  - relationChangeWhenLordRefusedToPayReparations -
  - relationChangeWhenLordExecutedTheCriminal -
  - relationChangeWhenPlayerSavedTheCriminal -
  - relationLordAndCriminalChangeWhenLordSavedTheCriminal -
  - relationChangeWithCriminalClanWhenPlayerExecutedTheCriminal -
  - relationChangeWhenLordKilledMessenger -
  - relationChangeWhenLordRefusedToSupportPeasantRevenge -
  - relationChangeWhenLordTeachPeasant -
  - relationChangeLordAndCriminalWhenLordExecutedTheAccusedCriminal -
  - goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal -
  - alwaysExecuteTheCriminal -
  - alwaysLetLiveTheCriminal -
  - alowKingdomClanToSaveTheCriminal -
  - showPeasantRevengeLogMessages -
  - showPeasantRevengeLogMessagesForKingdom -
  - peasantRevengerIsRandom -
  - playerCanPayAnyKingdomClanReparations -
  - peasantRevengerExcludeTrait -
  - lordNotExecuteMessengerTrait -
  - log_file_name -
  - file_name - file full path to load configuration from (you can use it to redirect game configuration to different file)
  - criminalHeroFromKingdomSuporterMinimumRelation -
  - criminalHeroFromKingdomSuporterMinimumAge -
  - criminalHeroFromClanSuporterMinimumRelation -
  - criminalHeroFromClanSuporterMinimumAge -
  - relationChangeAfterLordPartyGotPaid -
  - relationChangeAfterLordPartyGotNoReward -
  - lordWillAskRansomMoneyIfHasTraits -
  - lordWillOfferRansomMoneyIfHasTraits -
  - lordWillOfferRansomMoneyWithProbabilityIfTraitFails -
  - lordWillDemandRansomMoneyIfHasLessGoldThan -
  - otherKingdomClanCanCareOfPeasantRevenge -
  - alwwaysReportPeasantRevengeToClanLeader -
  - peasantRevengeTimeoutInDays -
  - peasantRevengeSartTimeInDays -
  - peasantRevengeMaxPartySize - revenger max party size (including notable peasant)
  - allowLordToKillMessenger - 
  - allowPeasantToKillLord -
  - logColorForClan - 
  - logColorForKingdom -
  - logColorForOtherFactions -
  - ai - AI parameters, what can have several values; 
	  - partyLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet -
	  - settlementLordLetNotableToKillTheCriminalEvenIfOtherConditionsDoNotLet - 
	  - lordWillAffordPartOfHisSavingsToPayForFavor -
	  - lordWillAffordToHelpTheCriminalAlly - 
	  - lordWillAffordToHelpTheCriminalEnemy -
	  - lordWillAffordToHelpPayLostRansom -
	  - lordIfFriendsWillHelpTheCriminal -
	  - lordIfRelativesWillHelpTheCriminal -
	  - criminalWillBlameOtherLordForTheCrime -
	  - lordWillKillBothAccusedHeroAndCriminalLord -
