# Vs Twitch

**This is a server-side mod! Only the host needs this mod installed!**

Tired of having your Twitch audience watch run after run after run without them being able to meaningful interact with you in-game? Look no further, as this mod allows your audience to become the "randomness", the "aggressor", or the "savior".

Will your chat help you along your journey, or try to stop your run early? Chat can influence what drops you get from chests, as well as donating [Twitch Bits](https://www.twitch.tv/bits) or using Channel Points (for Twitch Affiliates) to create random events you must fight through to survive.

## First Time Setup

1. Launch the game and once you load into a single player or multiplayer lobby (as host), the mod will walk you through authorizing to Twitch. Part of this set up requires launching your web browser to authorize the game with Twitch!

# Configurations

## In game editing (via RiskOfOptions)

This mod has a dependency on [RiskOfOptions](https://thunderstore.io/package/Rune580/Risk_Of_Options/) and so many values can be dynamically
updated while you are in the game via the Settings UI. Note that some options need a restart for changes to be fully applied; the options menu will mark this
accordingly when needed. Below you'll see a column for what configurations can be modified in game via the ✔️ marking; otherwise it'll have a ❌.

## Twitch

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`DebugLogs`|true/false|false|✔️|Enable debug logging for Twitch - will spam to the console!|
|`EnableItemVoting`|true/false|true|✔️|Enables the main feature of this mod. Disable it if you only want to enable bit interactions.|
|`VoteDurationdSec`|number (secs)|10|✔️|How long to allow Twitch to vote on items. Increase this value if viewers think the voting is going too "fast" - they might have their video delay too great.|
|`VoteStrategy`|string|MaxVote|✔️|How to tabulate votes. See "Voting Strategies" below for the various values this setting may have.|
|`BitsThreshold`|number|1500|✔️|The number of bits needed to cause an in-game event.|
|`CurrentBits`|number|0|❌|**Do not edit this field.** Used as storage whenever someone donates bits so that restarting the game doesn't clear the donation count.|
|`PublishToChat`|true/false|true|✔️|Publish events (like voting) to Twitch chat.|

### Revoking permissions to Twitch

To revoke, go to https://www.twitch.tv/settings/connections and Disconnect the app named "RoR2-VsApp". Next time you start the game with this mod enabled, it will run you through first-time setup again.

### Vote Strategies

These are the various voting strategies you can use for the `VoteStrategy` setting.

#### MaxVote

Item with the most votes wins. Ties are broken by choosing the first item that got the most votes.

#### MaxVoteRandomTie

Item with the most votes wins. Ties are broken by choosing randomly from the highest votes items.

#### Percentile

Item is chosen by a weighted random selection. If item 1 has 3 votes, item 2 has 4 votes, and item 3 has 1 vote (making a total of 8 votes), then the probabilites for choosing the item are as follows:

* Item 1: 3 / 8 = 37.50% chance
* Item 2: 4 / 8 = 50.00% chance
* Item 3: 1 / 8 = 12.50% chance

## Tiltify

This mod supports basic integration for [Tiltify](https://tiltify.com/) campaigns. Donations cause a random "Bit Event", following the weightings in the "Event" section of the configuration.

**How do I find my Campaign ID?** Once your campaign is created, navigate to your Campaign Dashboard --> Detail tab and find your campaign ID. It should be a six to seven digit number.

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`CampaignId`|string||✔️|The Campaign ID to track donations; clear it to disable Tiltify integration|

## ChannelPoints

**WARNING:** Not typing the Channel Points title in exactly will cause the specific feature not to work! You should see a warning in the console if this happens.

See the [Twitch Channel Points Guide](https://help.twitch.tv/s/article/channel-points-guide?language=en_US) (section "Custom Rewards") for how to create custom rewards. The Reward Name/Channel Points Title needs to be pasted into the configuration exactly as entered (case sensitive).

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`Enable`|true/false|true|✔️|Enable all Channel Point features|
|`AllyBeetle`|text||✔️|**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Beetle. Leave empty to disable.|
|`AllyLemurian`|text||✔️|**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Lemurian. Leave empty to disable.|
|`AllyElderLemurian`|text||✔️|**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Elder Lemurian. Leave empty to disable.|
|`RustedKey`|text||✔️|**(Case Sensitive!)** Channel Points Title to give everyone a Rusted Key. Leave empty to disable.|
|`BitStorm`|text||✔️|**(Case Sensitive!)** Channel Points Title for the bit storm bit event.|
|`Bounty`|text||✔️|**(Case Sensitive!)** Channel Points Title for the doppleganger bit event.|
|`ShrineOfOrder`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Shrine of Order bit event.|
|`ShrineOfTheMountain`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Shrine of the Mountain bit event.|
|`Titan`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Aurelionite bit event.|
|`LunarWisp`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Lunar Chimera (Wisp) bit event.|
|`Mithrix`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Mithrix bit event.|
|`ElderLemurian`|text||✔️|**(Case Sensitive!)** Channel Points Title for the Elder Lemurian bit event.|

## Event

When your channel hits the defined bit goal (via the `BitsThreshold` config setting), a random in-game event will occur. If enough bits are donated to trigger the goal more than once, the event's will continue to trigger until the bits are reduced under the given goal. For example, if the bit goal is 500 and someone donates 1040 bits, there will be two bit events and the current bit count will reset to 40 bits (1040 - 500 - 500 = 40).

To disable an event, simply set the weight to 0. Giving a higher weight increases the probability that the event will occur.

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`BitStormWeight`|number|1|✔️|Weight for the bit storm bit event. Dodge the meteors while trying to move ahead in the stage!|
|`BountyWeight`|number|1|✔️|Weight for the doppleganger bit event. Your Doppleganger has come to try to stop you!|
|`ShrineOfOrderWeight`|number|1|✔️|Weight for the Shrine of Order bit event. What's this? You see a Shrine that looks rare, and seems to emit a purple hue. You can't help yourself, but you offer a Lunar coin to it...|
|`ShrineOfTheMountainWeight`|number|1|✔️|Weight for the Shrine of the Mountain bit event. You have angered the Twitch Chat gods. Prepare for your final battle at the teleporter.|
|`TitanWeight`|number|1|✔️|Weight for the Aurelionite bit event. Aurelionite comes from the void to try to stop you!|
|`LunarWispWeight`|number|1|✔️|Weight for the Lunar Chimera (Wisp) bit event. Lunar Chimera's come from the void to try to stop you!|
|`MithrixWeight`|number|1|✔️|Weight for the Mithrix bit event. Twitch Chat decides to enter your stage to stop you a little earlier in the run...|
|`ElderLemurianWeight`|number|1|✔️|Weight for the Elder Lemurian bit event. Elder Lumerian's come from the void to try to stop you!|

## UI

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`SimpleUI`|true/false|false|✔️|If enabled, simplifies the item vote UI by putting a single popup in the top-middle of the game screen. If you are playing with multiple people, or generally have a lot of drones, enabling this option can help with clutter on the left side of the game window.|

## Behaviour

|Config|Type|Default|RiskOfOptions|Notes|
|------|----|-------|-------------|-----|
|`EnableChoosingLunarItems`|true/false|true|✔️|If enabled, Lunar Pod item/equipment drops will be decided by Twitch Chat.|
|`ForceUniqueRolls`|true/false|false|✔️|If enabled, all rolls will be guaranteed to be unique. No more rolls with three rusted keys!|

# Chat Commands

Chat commands can only be executed by Moderators or the Broadcaster of the channel. All other users will be ignored silently.

## Bit Events

* `!roll` - Forces a random bit event to occur.
* `!meteor` - Force Meteor shower bit event.
* `!bounty` - Force Doppleganger bit event.
* `!order` - Force a Shrine of Order bit event.
* `!mountain` - Force a Shrine of the Mountain bit event.
* `!titan` - Force Aurelionite bit event.
* `!lunar` - Force Lunar Chimera (Wisp) bit event.
* `!mithrix` - Force Mithrix bit event.
* `!lemurian` - Force Elder Lemurian bit event.
* `!grandparent` - Force Grandparent bit event (not currently supported).

## Ally Events

* `!allychip <name>` - Spawn ally Beetle with the given name.
* `!allysuperchip <name>` - Spawn random elite ally Beetle with the given name.
* `!allydino <name>` - Spawn ally Lemurian with the given name.
* `!allysuperdino <name>` - Spawn random elite ally Lemurian with the given name.
* `!allybigdino <name>` - Spawn ally Elder Lemurian with the given name.
* `!allysuperbigdino <name>` - Spawn random elite ally Elder Lemurian with the given name.

## Other

* `!rustedkey <name>` - Give all players a rusted key (with the given name being who gave it).

# Console Commands

_These console commands are generally for testing purposes. You should never need to use them during a run._

* `vs_add_bits <bits>` - Force add bits to the game. Going over the bit goal will trigger an in-game event.
* `vs_set_bit_goal <bits>` - Sets the bit goal and saves it to the config file.
