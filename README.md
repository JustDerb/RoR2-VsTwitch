# Vs Twitch

Tired of having your Twitch audience watch run after run after run without them being able to meaningful interact with you in-game? Look no further, as this mod allows your audience to become the "randomness", the "aggressor", and... Mithrix.

Will your chat help you along your journey, or try to stop your run early? Chat can influence what drops you get from chests, as well as donating [Twitch Bits](https://www.twitch.tv/bits) to create random events you must fight through to survive.

## First Time Setup

1. Launch the game so that the configuration file is created for the first time. **Exit the game.**
2. Edit the configuration file to suite your needs. See the Configuration tables below for more info.
3. Launch the game (hint: you might want to watch the intro scene one more time...)

# Configurations

Currently, `Channel`, `Username`, and `ImplicitOAuth` need to be filled out at a minimum (if you use nothing but the default values).

## Twitch

**WARNING:** The code you put into `ImplicitOAuth` is valid for about 30 days. After that, you'll need to re-auth and update the field!

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`Channel`|text||The channel to monitor Twitch chat|
|`Username`|text||The username to use when calling Twitch APIs. If you aren't using a secondary account, this should be the same as `Channel`|
|`ImplicitOAuth`|text||The "password" to access Twitch APIs. Please visit [twitchapps.com][1] to get the password to put here. Note that this password is not sent to any servers other than Twitch to authenticate. **DO NOT GIVE THIS TO ANYONE.** To revoke this password, go to [Twitch Connections Settings][2] and Disconnect the app named "Twitch Chat OAuth Token Generator".|
|`DebugLogs`|true/false|false|Enable debug logging for Twitch - will spam to the console!|
|`ClientID`|text|q6batx0epp608isickayubi39itsckt|The client ID of the app that you used to populate the `ImplicitOAuth` field. If you used [twitchapps.com][1] this would be the default value. If you used another Twitch app, this needs to be changed accordingly.|
|`EnableItemVoting`|true/false|true|Enables the main feature of this mod. Disable it if you only want to enable bit interactions.|
|`VoteDurationdSec`|number (secs)|20|How long to allow Twitch to vote on items. Increase this value if viewers think the voting is going too "fast" - they might have their video delay too great.|
|`BitsThreshold`|number|1500|The number of bits needed to cause an in-game event.|
|`CurrentBits`|number|0|**Do not edit this field.** Used as storage whenever someone donates bits so that restarting the game doesn't clear the donation count.|

[1]: https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=q6batx0epp608isickayubi39itsckt&redirect_uri=https://twitchapps.com/tmi/&scope=channel_subscriptions+user_subscriptions+channel_check_subscription+bits:read+chat:read+chat:edit+channel:read:redemptions+channel:read:hype_train
[2]: https://www.twitch.tv/settings/connections

## ChannelPoints

**WARNING:** Not typing the Channel Points title in exactly will cause the specific feature not to work! You should see a warning in the console if this happens.

See the [Twitch Channel Points Guide][3] (section "Custom Rewards") for how to create custom rewards. The Reward Name/Channel Points Title needs to be pasted into the configuration exactly as entered (case sensitive).

[3]: https://help.twitch.tv/s/article/channel-points-guide?language=en_US

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`Enable`|true/false|true|Enable all Channel Point features|
|`AllyBeetle`|text||**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Beetle. Leave empty to disable.|
|`AllyLemurian`|text||**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Lemurian. Leave empty to disable.|
|`AllyElderLemurian`|text||**(Case Sensitive!)** Channel Points Title to spawn Ally Elite Elder Lemurian. Leave empty to disable.|
|`RustedKey`|text||**(Case Sensitive!)** Channel Points Title to give everyone a Rusted Key. Leave empty to disable.|
|`BitStorm`|text||**(Case Sensitive!)** Channel Points Title for the bit storm bit event.|
|`Bounty`|text||**(Case Sensitive!)** Channel Points Title for the doppleganger bit event.|
|`ShrineOfOrder`|text||**(Case Sensitive!)** Channel Points Title for the Shrine of Order bit event.|
|`ShrineOfTheMountain`|text||**(Case Sensitive!)** Channel Points Title for the Shrine of the Mountain bit event.|
|`Titan`|text||**(Case Sensitive!)** Channel Points Title for the Aurelionite bit event.|
|`LunarWisp`|text||**(Case Sensitive!)** Channel Points Title for the Lunar Chimera (Wisp) bit event.|
|`Mithrix`|text||**(Case Sensitive!)** Channel Points Title for the Mithrix bit event.|
|`ElderLemurian`|text||**(Case Sensitive!)** Channel Points Title for the Elder Lemurian bit event.|

## Event

To disable an event, simply set the weight to 0. Giving a higher weight increases the probability that the event will occur.

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`BitStormWeight`|number|1|Weight for the bit storm bit event.|
|`BountyWeight`|number|1|Weight for the doppleganger bit event.|
|`ShrineOfOrderWeight`|number|1|Weight for the Shrine of Order bit event.|
|`ShrineOfTheMountainWeight`|number|1|Weight for the Shrine of Order bit event.|
|`TitanWeight`|number|1|Weight for the Aurelionite bit event.|
|`LunarWispWeight`|number|1|Weight for the Lunar Chimera (Wisp) bit event.|
|`MithrixWeight`|number|1|Weight for the Mithrix bit event.|
|`ElderLemurianWeight`|number|1|Weight for the Elder Lemurian bit event.|

## UI

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`SimpleUI`|true/false|false|If enabled, simplifies the item vote UI by putting a single popup in the top-middle of the game screen. If you are playing with multiple people, or generally have a lot of drones, enabling this option can help with clutter on the left side of the game window.|

## Behaviour

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`EnableChoosingLunarItems`|true/false|true|If enabled, Lunar Pod item/equipment drops will be decided by Twitch Chat.|
|`ForceUniqueRolls`|true/false|false|If enabled, all rolls will be guaranteed to be unique. No more rolls with three rusted keys!|

# Events

When your channel hits the defined bit goal (via the `BitsThreshold` config setting), a random in-game event will occur. If enough bits are donated to trigger the goal more than once, the event's will continue to trigger until the bits are reduced under the given goal. For example, if the bit goal is 500 and someone donates 1040 bits, there will be two bit events and the current bit count will reset to 40 bits (1040 - 500 - 500 = 40).

## Bit Storm

Dodge the meteors while trying to move ahead in the stage!

## Bounty

Your Doppleganger has come to try to stop you!

## Shrine of Order

What's this? You see a Shrine that looks rare, and seems to emit a purple hue. You can't help yourself, but you offer a Lunar coin to it...

## Shrine of the Mountain

You have angered the Twitch Chat gods. Prepare for your final battle at the teleporter.

## Aurelionite

Aurelionite comes from the void to try to stop you! (Until this monster is killed, the teleporter will not fully charge)

## Lunar Chimera (Wisp)

Lunar Chimera's come from the void to try to stop you! (Until these monsters are killed, the teleporter will not fully charge)

## Mithrix (Twitch Chat)

Twitch Chat decides to enter your stage to stop you a little earlier in the run... (Until this monster is killed, the teleporter will not fully charge)

## Elder Lemurian

Elder Lumerian's come from the void to try to stop you! (Until these monsters are killed, the teleporter will not fully charge)

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

These will eventually be hooked into Channel Points; but for know you can manually run these for your chat.

* `!allychip <name>` - Spawn ally Beetle with the given name.
* `!allysuperchip <name>` - Spawn random elite ally Beetle with the given name.
* `!allydino <name>` - Spawn ally Lemurian with the given name.
* `!allysuperdino <name>` - Spawn random elite ally Lemurian with the given name.
* `!allybigdino <name>` - Spawn ally Elder Lemurian with the given name.
* `!allysuperbigdino <name>` - Spawn random elite ally Elder Lemurian with the given name.

## Other

* `!rustedkey <name>` - Give all players a rusted key.

# Console Commands

These console commands are generally for testing purposes. You should never need to use them during a run.

* `vs_connect_twitch <channel> <access_token> [username]` - Connect to Twitch. Note that this automatically happens when starting a new run.
* `vs_add_bits <bits>` - Force add bits to the game. Going over the bit goal will trigger an in-game event.
* `vs_set_bit_goal <bits>` - Sets the bit goal and saves it to the config file.

# Changelog

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
