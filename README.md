# Peasant Revenge
 Peasant Revenge is a Mount&Blade II mod what aims to implement peasant revenge after village has been raided. 
 Player and AI heroes, who captured the criminal, can now allow notable peasant to behead the criminal. 

## Contributors
   [DeLiXxN](https://www.nexusmods.com/mountandblade2bannerlord/users/109248658) English translations (EN)
    
## Features
### Dialogues for notable peasants and AI lords: 
#### - Dialogues for notable peasant when AI or player has captured the criminal hero.
 1. If player captured the criminal, the dialogue options are: 
    - criminal will die (the criminal will be executed);
    - criminal will pay the reparation;
    - ignore peasant demands;
    - ask the criminal about the village raid. The criminal will blame other prisoner for the crime. More options will be available:
      - agree with the criminal (the blamed hero will be executed);
      - disagree with the criminal (the criminal will be executed);
      - execute both (the blamed hero and the criminal);
      - do not allow the execution due to disagreements (criminal and blamed hero are not executed)
 2. When player is captured as a criminal, the dialogue options are: 
     - asking the hero, what if he will not pay the reparation (depending on AI hero traits and relations, hero will tell if player will be executed or not);
     - (**not implemented**) player try to persuade the hero;
     - ask other friendly AI for a favor to pay the reparation (the other hero will pay the reparation);
     - agree to pay the reparation;
     - do not agree to pay the reparation (depending on AI hero traits and relations, player will be executed or spared);
     - blame other prisoner for the crime. More options will be available:
       - confirm the blame (blamed hero will be executed);
     - Depending on AI hero traits and relations, AI will decide, if blamed hero or/and criminal will be executed, or will not allow the execution due to disagreements);
   - change the mind (dialogue will return to previous options);
 3. When hero (from player clan or kingdom) caught the criminal, who has been raiding player's clan or kingdom village. Notable peasant dialogue options are:
     - agree to pay the reparation to notable peasant (the criminal is not executed);
- disagree to pay the reparation and demand to spare the criminal (the criminal is not executed);
- disagree to pay the reparation (the criminal is executed);
- allow to execute both (blamed hero and the criminal);
     - kill the notable peasant;
- (**not implemented**) hero let the other lord (AI will search other hero, who could save the criminal) to decide criminal's fate.
- Hero, who had the criminal hero as a prisoner, may demand "lost ransom". Dialogue options are:
   - disagree to pay "lost ransom";
   - agree to pay "lost ransom";
   - kill the soldier, who asked for "lost ransom";
 4. When player encounter notable party before notable could meet the hero, who has the criminal as a prisoner. Notable peasant dialogue options are:
     - kill notable peasant (revenge is aborted);
     - disband notable peasant mobile party (revenge is aborted, notable peasant will go back to his village);
     - leave notable peasant (notable peasant will continue his revenge);
 5. Dialogue for notable peasant to encourage them to initiate (or not to) the peasant revenge. Notable peasant dialogue options are:
     - execute the notable peasant (game will replace the notable peasant with a new one);
     - teach the notable peasant (use player's traits to change notable peasant's traits);
     - bribe the notable peasant (use money to change notable peasant's traits);
   - (**not implemented**) bribing effect should be temporary and depend on traits and relations.
 #### - Dialogues for notable peasant for mod configuration purpose:
 1. Enable or disable notable peasant parties,
 2. Enable or disable "help neutral village" menu option.
#### - Other dialogues:
 1. (**not implemented**) Discussion with party hero, who refused to kill criminal.
### AI behavior to handle peasant revenge
- The peasant revenge can happen after hero committed to raid the village and the hero became a prisoner, and any notable village peasant has correct hero traits.
- The notable peasant  will initiate dialogue with a mobile party what has the criminal as a prisoner. And will demand criminal's head off or pay the reparation (reparation is scaled to hearts count of the village).
- AI may not allow to kill the criminal due to relations or traits.
- AI will try to find, who could pay the reparation (the criminal, criminal's clan members, criminal's relatives, friends...).
- Heroes can kill notable peasants, when heroes have certain traits and does not want to pay reparation to notable peasant (Player cannot ask for "lost ransom" up to game version 1.1.5).
- Mobile party may demand "lost ransom" after the criminal has been killed.
- Notables, who have active issue quests, do not participate in peasant revenge.
- Mobile parties without leader hero, do not participate in peasant revenge.
- (**not implemented**) AI may try to encourage notable peasant to initiate the peasant revenge, if visited village does not have potential notable peasant and hero traits are appropriate.
### Heroes relations and traits due to peasant revenge
- Traits
  - This mod add additional hero traits exp (for player only, because non player heroes do not have trait exp. by game design) default changes during peasant revenge:
    - -2 Mercy, -5 Honor, -2 Calculating, -1 Valor, when player leave the criminal hero remains.
    - +2 Mercy, +5 Honor, +2 Calculating, +1 Valor, when player accept to ransom the criminal hero remains.
- -2 Mercy, -5 Honor, -2 Calculating, +1 Valor, when player decline to return the criminal hero remains.
- -5 Mercy, +5 Valor, when player tried to persuade the notable peasant to do revenges.
- +5 Mercy, -5 Valor, when player tried to persuade the notable peasant not to do revenges.
- -2 Mercy, -3 Honor, when player executed the notable peasant after or before the notable peasant did his quest.
When paying the reparation heroes will gain charisma and generosity exp (default game process).  
  - Hero (player only) can change notable peasant traits via dialogue, when encouraging them to initiate (or not to) the peasant revenge.
    Notable peasant will get:
    - +1 level Mercy, -1 level Valor, when notable peasant is persuaded not to revenge.
- -1 level Mercy, +1 level Valor, when notable peasant is persuaded to revenge.
  (exp. values can be changed in the mod configuration file)
- Relations
  - Mod try to care of relations between the criminal, notable peasant, mobile party hero, other involved heroes (who paid "lost ransom", settlement owner).
  - Player does not get relations penalties if notable peasant behead the criminal hero (because we assume that execution was because of peasant fault for now). (**not implemented**) Player relations penalties can be set in the configuration file (see relationChangeForPlayerWhenNotableExecutedTheCriminal).
## Configuration:
- You can tweak some configuration values to your liking.
Configuration values are in the file PeasantRevengeModCfg xml, which is created after you create the game, and reloaded after you load the game. And file is at PeasantRevenge dll file location.
- Mod will not write to this file if "CfgVersion" is higher than or equal to PeasantRevenge dll file version (your changes to this file will retain).
- Mod does not save any progress (however game will save notable peasant mobile parties, but without revenge data. The mod will try to disband them, after game was loaded), so game will forget about past raid criminal heroes after game reload (it is done intentionally)
## Uninstalling
If you enabled notable peasant parties, disable them and wait (peasantRevengeTimeoutInDays days) until parties are removed and save game, before you uninstall the mod.
## Known Bugs:
If you enable notable parties, sometimes village will get 1 more notable for a short time, after revenge mobile party is disbanded. This is because other TW module adds/removes them (more a feature than bug.).
## Compatibility:
- Should be Save file compatible (Can be loaded/unloaded any time. If notable parties are enabled - let AI to kill them off)).
- Mod has item "Remains of a corpse". It is converted to "Trash item" by game design after mod removal.
## Source code
[GitHub](https://github.com/arunasradv/PeasantRevenge.git)

## Configuration parameters
  - CfgVersion - mod will not update the configuration, if this version is higher or equal than PeasantRevenge dll file version (first digits).
  - enableRevengerMobileParty - disable or enable revenger parties (true - revenger party will spawn).
  - enableHelpNeutralVillageAndDeclareWarToAttackerMenu - disable or enable menu option to help neutral villages, what are raided by neutral heroes.
  - ReparationsScaleToSettlementHearts - criminal heroes must pay reparation equal to ReparationsScaleToSettlementHearts multiplied by village hearts count.
  - relationChangeAfterReparationsReceived - relation change between notable peasant and party leader, who paid the reparation.
  - relationChangeWhenCannotPayReparations - relation change between notable peasant and party leader, who did not paid the reparation, because does not have money to pay.
  - relationChangeWhenCriminalRefusedToPayReparations - relation change between notable peasant and criminal hero, who did not paid the reparation, because refused to pay.
  - relationChangeWhenLordRefusedToPayReparations - relation change between notable peasant and party leader, who did not paid the reparation, because refused to pay.
  - relationChangeWhenLordExecutedTheCriminal - relations change between party leader and criminal hero clan after the criminal hero was executed by notable peasant. 
  - relationChangeWhenPlayerSavedTheCriminal - relations change between player and criminal after player refused to allow execution of the criminal heroes.
  - (**not implemented**) relationChangeForPlayerWhenNotableExecutedTheCriminal - relations changebetween player and criminal hero clan after the criminal hero was executed by notable peasant.   
  - relationLordAndCriminalChangeWhenLordSavedTheCriminal - relations change between hero (player too) and criminal after hero refused to allow execution of the criminal heroes.
  - relationChangeWithCriminalClanWhenPlayerExecutedTheCriminal -  relations change between player and criminal hero clan after player agreed to allow the execution of the criminal heroes.
  - relationChangeWhenLordKilledMessenger - relations change between hero and party leader after hero executed troop, who demanded "lost ransom".
  - relationChangeWhenLordRefusedToSupportPeasantRevenge - relations change between hero (settlement owner or party leader) and notable peasant, and inverted change between  hero (settlement owner or party leader) and criminal hero, when hero (settlement owner or party leader) refused to allow execution of the criminal heroes.
  - relationChangeWhenLordTeachPeasant - relation change between notable peasant and hero after hero tries to encourage the notable peasant to be able to initiate the peasant revenge.
  - relationChangeLordAndCriminalWhenLordExecutedTheAccusedCriminal - relations change between hero and criminal hero, when criminal hero was not executed, but only to accused hero (to blamed one).
  - goldPercentOfPeasantTotallGoldToTeachPeasantToBeLoyal - percent of notable peasant total gold, what is needed to encourage the notable peasant to be able to initiate the peasant revenge.
  - alwaysExecuteTheCriminal - if true AI will always try to execute the criminal heroes (if alwaysLetLiveTheCriminal is true, this option is ignored).
  - alwaysLetLiveTheCriminal - if true AI will not let to kill the criminal heroes (even reparation will be not paid by criminal - disabled AI).
  - alowKingdomClanToSaveTheCriminal - if false, other clans will not participate in peasant revenge (only settlement owner clan will participate)
  - showPeasantRevengeLogMessages - show messages related to peasant revenge.
  - showPeasantRevengeLogMessagesForKingdom - show messages from  related to peasant revenge in the own kingdom.
  - peasantRevengerIsRandom - if true peasant revenger is not selected by his traits 
  - playerCanPayAnyKingdomClanReparations - (unused)
  - peasantRevengerExcludeTrait - if notable peasant has these traits, he will not participate in peasant revenge (default: "Mercy > 0|Valor < 0")
  - lordNotExecuteMessengerTrait - hero trait who will not execute the notable peasant, but will refuse to pay
  - log_file_name - full log file path (use it only for testing and want to see how AI works)
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
  - otherKingdomClanCanCareOfPeasantRevenge - if false, other kingdom clan will not participate in the diferent kingdom peasant revenge.
  - alwwaysReportPeasantRevengeToClanLeader - if true, main hero settlement peasant will always go to main hero during peasant revenge. If false, main hero settlement notable might solve revenge without player participation, if criminal is not in main hero party.
  - peasantRevengeTimeoutInDays - revenger party will have limited time duration for the revenge.
  - peasantRevengeSartTimeInDays - revenger party will spawn after some time.
  - peasantRevengeMaxPartySize - revenger max party size (including notable peasant).
  - (**not implemented**) peasantRevengeAiIgnorePartyForDays -  other hostile ai parties will not attack revenger party for some time.
  - allowLordToKillMessenger - heroes cannot kill notable peasant.
  - allowPeasantToKillLord - party leader (or player) will kill the criminal instead of notable peasant
  - logColorForClan - peasant revenge log colour for player's clan.
  - logColorForKingdom - peasant revenge log colour for other kingdom clans.
  - logColorForOtherFactions - peasant revenge log colour for other factions.
  - peasantRevengerIntimidationPowerScale - notable peasant party power is multiplied by this constant and compared to party player party's power.
  - enableOtherNobleTraitsChangeAfterNobleExecution - If this value is false, traits of other notables will not change due another notable peasant execution in the same village.
  - lordTraitChangeWhenLordExecuteRevengerAfterOrBeforeQuest -
  - lordCanTryAsManyTimesToPersuadeTheNotable - when player try to persuade peasant to revenge or not , he can try it only limited times. (reset after game reload) 
  - ai - AI parameters, what can have several values (the lists); 
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
	  - lordTraitChangeWhenRansomRemainsDeclined - (for player only)
      - lordTraitChangeWhenRansomRemainsAccepted - (for player only)
      - lordTraitChangeWhenRemainsOfLordAreAbandoned - (for player only) 
      - lordWillDeclineRansomTheVictimRemains -
      - lordWillAbandonTheVictimRemains -
