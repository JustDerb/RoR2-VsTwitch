# Vs Twitch

Tired of having your Twitch audience watch run after run after run without them being able to meaningful interact with you in-game? Look no further, as this mod allows your audience to become the "randomness", the "aggressor", and... Mithrix.

Will your chat help you along your journey, or try to stop your run early? Chat can influence what drops you get from chests, as well as donating [Twitch Bits](https://www.twitch.tv/bits) to create random events you must fight through to survive.

## First Time Setup

1. Launch the game so that the configuration file is created for the first time. **Exit the game.**
2. Edit the configuration file to suite your needs. See the Configuration tables below for more info.
3. Launch the game (hint: you might want to watch the intro scene one more time...)

# Configurations

Currently, **all config values** need to be filled out. Especially `Channel`, `Username`, and `ImplicitOAuth`.

## Twitch

|Config|Type|Default|Notes|
|------|----|-------|-----|
|`Channel`|text||The channel to monitor Twitch chat|
|`Username`|text||The username to use when calling Twitch APIs. If you aren't using a secondary account, this should be the same as `Channel`|
|`ImplicitOAuth`|text||The "password" to access Twitch APIs. Please visit [twitchapps.com][1] to get the password to put here. Note that this password is not sent to any servers other than Twitch to authenticate. **DO NOT GIVE THIS TO ANYONE.** To revoke this password, go to [Twitch Connections Settings][2] and Disconnect the app named "Twitch Chat OAuth Token Generator".|
|`EnableItemVoting`|true/false|true|Enables the main feature of this mod. Disable it if you only want to enable bit interactions.|
|`VoteDurationdSec`|number (secs)|20|How long to allow Twitch to vote on items. Increase this value if viewers think the voting is going too "fast" - they might have their video delay too great.|
|`BitsThreshold`|number|1500|The number of bits needed to cause an in-game event.|
|`CurrentBits`|number|0|**Do not edit this field.** Used as storage whenever someone donates bits so that restarting the game doesn't clear the donation count.|

[1]: https://id.twitch.tv/oauth2/authorize?response_type=token&client_id=q6batx0epp608isickayubi39itsckt&redirect_uri=https://twitchapps.com/tmi/&scope=channel_subscriptions+user_subscriptions+channel_check_subscription+bits:read+chat:read+chat:edit+channel:read:redemptions+channel:read:hype_train
[2]: https://www.twitch.tv/settings/connections

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

# Console Commands

These console commands are generally for testing purposes. You should never need to use them during a run.

* `vs_connect_twitch <channel> <access_token> [username]` - Connect to Twitch. Note that this automatically happens when starting a new run.
* `vs_add_bits <bits>` - Force add bits to the game. Going over the bit goal will trigger an in-game event.
* `vs_set_bit_goal <bits>` - Sets the bit goal and saves it to the config file.