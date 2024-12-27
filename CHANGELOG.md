# Changelog

### 1.0.17

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/15?closed=1

* Fixed Channel Points not working.
* Fixed monster spawns reducing the cost for the Director to 0, which would cause an explosion of enemies of that type to spawn (in elite form).
* 

### 1.0.16

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/14?closed=1

* Removed teleporter lock for monster events. It was too confusing for people and sometimes it bugged out and ruined runs.

### 1.0.15

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/13?closed=1

* Truly made `Risk_Of_Options` a soft dependency! The mod will now correctly work with the mod not installed.

### 1.0.14

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/12?closed=1

* Added new dependency: [RiskOfOptions](https://thunderstore.io/package/Rune580/Risk_Of_Options/)
* Refactored configurations to hook into RiskOfOptions
* Allowed ChannelPoints to be dynamically updated in game (previously they were coded that you needed a restart)
* Unified the logging system. You should now be able to filter all of Vs Twitch logs easily via the mod launcher.

### 1.0.13

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/11?closed=1

* Overall, minor bug fixes
* Fixed items not being given to everyone in multiplayer when item voting turned on
* Added maintainer chat command (to help me verify myself in a channel)
* Updated the chat to show `DisplayName` instead of `Username`

### 1.0.12

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/10?closed=1

* Added new option to disable language changes: `EnableLanguageEdits`
* Added support for Tiltify campaigns
* Fixed Void cradles always rolling Lunar items when opened. This was due to the Risk of Rain 2 DLC: Survivors of the Void migrating to a new drop table mechanism. Rolling for items now use this new mechanism.

### 1.0.11

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/9?closed=1

* Removed R2API dependency
* Update code to work with the new Risk of Rain 2 DLC: Survivors of the Void
* Added a new Twitch configuration: `PublishToChat`

### 1.0.10

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/8?closed=1

* Minor language updates
* The Shrine of the Mountain events (bits or channel points) no longer scale according to `Mountain Shrines = # of players`. Each trigger will cause only one Mountain Shrine event to happen, regardless of how many players there are.

### 1.0.9

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/7?closed=1

* Percentile voting strategy actually works now.
* When chat is voting on Equipment and Artifact of Command is turned on, all players will immediatly recieve the equipment. It will no longer drop an orb (as if that happend, the orb would turn into a Command Essence).
* MaxVoteRandomTie will correctly pick a random item if there are no votes.
* Updated the ImplicitOAuth description to direct them to the README.md so they can easily click the link. People were trying to type the URL form the mod manager...
* Fixed Rusted Key Channel Points integration.

### 1.0.6

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/6?closed=1

* Updated library for new RoR2 release

### 1.0.5

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/5?closed=1

* When someone spends Channel Points, their username will appear in the chat.
* Event Director update: It now is created on startup and lives for the life of the game. This should help with events during the character select screen (as before they didn't work even when it seemed like they did).
* Some text formatting around numbers.
* Voting queue will be reset when a run ends. No more stacking the queue in the current run, ending the game, and getting them in the next run.
* When events happen, the teleporter will now have a visual update (purple crystals). This should help for more visual queues that something is blocking the teleporter from charging.
* The voting strategy is now configurable. (Config: `VoteStrategy`)

### 1.0.4

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/4?closed=1

* Spawned monsters from Bit/Channel Point events will not be destroyed after Void Field waves or Mithrix waves.
* Spawned monsters no longer die immediatly when hitting out of bounds/zone triggers. No more cheesing Mithrix.
* Spawned monsters do not take fall damage (mainly to counter teleporting them back into the stage).
* Twisted Scavenger should correctly drop Lunar Coins at the end instead of causing a roll
* Shrine of the Mountain, on the last stage, is worthless; now it gives all alive enemies Dio's Best Friend and random items equivalent to the number of players in the run. If the monster already has a Dio's (consumed or not) they do not recieve this buff.

### 1.0.3

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/3?closed=1

* All Bit Events can now be triggered via Channel Points (if configured).
* Updated title menu text.
* Update language to use they/them pronouns.
* Added more debug logging around TwitchLib (this library...can be finicky for some people). (Config: `DebugLogs`)
* Move the location of the vote notification to no longer be in the way of any allies. It's not positioning using the best calculation... but it's better than it was.'
* Protect against mods that cause rolls to happen mutliple times, causing the same rolls one after the other. If this happens a warning will be printed to the console.
* Allies get reduced items to hopefully be less "tanky"

### 1.0.2

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/2?closed=1

* Added Channel Points integration (See ChannelPoints Configuration)
* Updated how bit event bosses Health and HP scale over time. This should make them more difficult as the run progresses.

### 1.0.1

See more info: https://github.com/JustDerb/RoR2-VsTwitch/milestone/1?closed=1

* VsTwitch should now only activate when hosting a game. This mod will not enable and connect to Twitch if you are joining a game as a client.
* Lunar Pods have been added to the choices Twitch Chat can choose from (Config: `EnableChoosingLunarItems`)
* Now rolls can be forced to always be unique (Config: `ForceUniqueRolls`)
* Added new Bit Event: Shrine of Order (Config: `ShrineOfOrderWeight`)
* Added new Bit Event: Shrine of the Mountain (Config: `ShrineOfTheMountainWeight`)

### 1.0.0

* Initial release
