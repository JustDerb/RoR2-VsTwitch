using RoR2;
using System.Collections.Generic;

namespace VsTwitch
{
    class LanguageOverride
    {
        private readonly Dictionary<string, string> StringsByToken;

        public const string STREAMER_TOKEN = "{streamer}";

        private string _StreamerName;
        public string StreamerName {
            get { 
                return _StreamerName; 
            }
            set {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _StreamerName = Util.EscapeRichTextForTextMeshPro(value.Trim());
                }
                else
                {
                    _StreamerName = "Streamer";
                }
            }
        }

        public LanguageOverride()
        {
            StreamerName = "Streamer";
            StringsByToken = new Dictionary<string, string>
            {
				// Dialogue.txt
				{ "BROTHER_DIALOGUE_FORMAT", "<color=#c6d5ff><size=120%>Twitch Chat: {0}</color></size>" },
                { "BROTHER_SPAWN_PHASE1_1", $"Get that F key warmed up, {STREAMER_TOKEN}" },
                { "BROTHER_SPAWN_PHASE1_2", $"Time to get one hit KO'd, {STREAMER_TOKEN}" },
                { "BROTHER_SPAWN_PHASE1_3", $"No amount of character guide Youtube videos can save you now, {STREAMER_TOKEN}" },
                { "BROTHER_SPAWN_PHASE1_4", $"Your DPS is absolute trash, {STREAMER_TOKEN}" },
                { "BROTHER_DAMAGEDEALT_1", "git gud" },
                { "BROTHER_DAMAGEDEALT_2", "Too much bungus, not enough movement speed" },
                { "BROTHER_DAMAGEDEALT_3", "trash. no cap." },
                { "BROTHER_DAMAGEDEALT_4", "Needs more rusted keys" },
                { "BROTHER_DAMAGEDEALT_5", "THIS IS MY SWAMP" },
                { "BROTHER_DAMAGEDEALT_6", "Stop feeding" },
                { "BROTHER_DAMAGEDEALT_7", "MOM, GET THE CAMERA" },
                { "BROTHER_DAMAGEDEALT_8", "Chat, finds a way" },
                { "BROTHER_DAMAGEDEALT_9", "baited" },
                { "BROTHER_DAMAGEDEALT_10", "I bet you main Huntress" },
                { "BROTHER_KILL_1", "ur mad" },
                { "BROTHER_KILL_2", "gg ez" },
                { "BROTHER_KILL_3", "PogU" },
                { "BROTHER_KILL_4", $"{STREAMER_TOKEN} is typing..." },
                { "BROTHER_KILL_5", $"Sent {STREAMER_TOKEN} straight to the shadow realm" },
                { "BROTHERHURT_DAMAGEDEALT_1", "Get down from that ramp and fight me like a man" },
                { "BROTHERHURT_DAMAGEDEALT_2", $"/votekick {STREAMER_TOKEN}" },
                { "BROTHERHURT_DAMAGEDEALT_3", "UNO REVERSE CARD" },
                { "BROTHERHURT_DAMAGEDEALT_4", "don't choke" },
                { "BROTHERHURT_DAMAGEDEALT_5", "consider those cheeks CLAPPED" },
                { "BROTHERHURT_DAMAGEDEALT_6", "boop" },
                { "BROTHERHURT_DAMAGEDEALT_7", $"It was at this moment, {STREAMER_TOKEN} knew, they f***ed up" },
                { "BROTHERHURT_DAMAGEDEALT_8", "YEET" },
                { "BROTHERHURT_DAMAGEDEALT_9", "OOF" },
                { "BROTHERHURT_DAMAGEDEALT_10", "You're gonna love my bits." },
                { "BROTHERHURT_KILL_1", "get rekt m8" },
                { "BROTHERHURT_KILL_2", "BE GONE, THOT" },
                { "BROTHERHURT_KILL_3", "THIS IS MY CHANNEL NOW" },
                { "BROTHERHURT_KILL_4", "WASTED" },
                { "BROTHERHURT_KILL_5", "gg ez" },
                { "BROTHERHURT_DEATH_1", $"You had to have gotten good rolls, {STREAMER_TOKEN}" },
                { "BROTHERHURT_DEATH_2", $"{STREAMER_TOKEN} STONKS = PURCHAS" },
                { "BROTHERHURT_DEATH_3", $"I wish it didn't have to come to this {STREAMER_TOKEN}...[prepares bits]" },
                { "BROTHERHURT_DEATH_4", "Twitch.exe has encountered a fatal error" },
                { "BROTHERHURT_DEATH_5", "WoolieGaming is typing..." },
                { "BROTHERHURT_DEATH_6", "I am become salt" },
                // Cutscene.txt
                { "CUTSCENE_INTRO_FLAVOR_1", "UES 'Poggers' " },
                { "CUTSCENE_INTRO_FLAVOR_2", "Last known coordinates of the UES 'TriHard'" },
                { "CUTSCENE_INTRO_SUBTITLE_1", $"We're here, {STREAMER_TOKEN}. The Twitch alert came from this exact coordinate." },
                { "CUTSCENE_INTRO_SUBTITLE_2", "This channel... it isn't on any subscriptions we know. This area isn't even favorited... " },
                { "CUTSCENE_INTRO_SUBTITLE_3", "Any visuals on the hype train?" },
                { "CUTSCENE_INTRO_SUBTITLE_4", "No sir... just Prime subs." },
                { "CUTSCENE_INTRO_SUBTITLE_5", "Any signs of streamers?" },
                { "CUTSCENE_INTRO_SUBTITLE_6", "....No sir. Bioscanner is dark." },
                { "CUTSCENE_INTRO_SUBTITLE_7", "Sir... the Twitch alert. It mentioned... it mentioned a mod we have never seen before." },
                { "CUTSCENE_INTRO_SUBTITLE_8", "Emotes, and bits, and..." },
                { "CUTSCENE_INTRO_SUBTITLE_9", "...Sir... do you think we're ready?" },
                { "CUTSCENE_INTRO_SUBTITLE_10", "Sir?" },
                { "GENERIC_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, knowing they shamelessly shook down his Twitch Chat for all their worth." },
                { "COMMANDO_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, satisfied with the fact that they bested Twitch Chat with THE WORST survivor in the current meta." },
                { "HUNTRESS_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, knowing they would get absolutely CLAPPED if Twitch Chat had made him play literally any other character." },
                { "ENGI_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, more BUNGUS than man, and yet, more man than Twitch Chat." },
                { "TREEBOT_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, reduced to a lifeless desk plant after the Captain decided they needed his fuel array back." },
                { "CROCO_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, reveling in the fact that, according to Twitch Chat, they is the GOODEST BOI." },
                { "LOADER_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, knowing that they were the only viable melee character." },
                { "MERC_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, his sword as dull as the idiots still watching this on Twitch" },
                { "CAPTAIN_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, forever haunted by flashbacks of meteor showers and mutiny" },
                { "TOOLBOT_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, with multiple counts of vehicular manslaughter and a thirst for blood." },
                { "MAGE_OUTRO_FLAVOR", $"..and so {STREAMER_TOKEN} left, wishing Twitch chat had thrown in a few more goat hooves" },
                // Difficulty.txt
                { "DIFFICULTY_BAR_0", "ResidentSleeper" },
                { "DIFFICULTY_BAR_1", "CoolStoryBob" },
                { "DIFFICULTY_BAR_2", "Jebaited" },
                { "DIFFICULTY_BAR_3", "Kreygasm" },
                { "DIFFICULTY_BAR_4", "TriHard" },
                { "DIFFICULTY_BAR_5", "MrDestructoid" },
                { "DIFFICULTY_BAR_6", "TheIlluminati" },
                { "DIFFICULTY_BAR_7", "PJSalt" },
                { "DIFFICULTY_BAR_8", "BibleThump" },
                { "DIFFICULTY_BAR_9", "KAPPAKAPPA" },
                // Main.txt
                { "TITLE_MULTIPLAYER", "Fight Chat" },
                { "DIFFICULTY_EASY_NAME", "LUL" },
                { "DIFFICULTY_NORMAL_NAME", "PogChamp" },
                { "DIFFICULTY_HARD_NAME", "NotLikeThis" },
                // CharacterBodies.txt
                { "BROTHER_BODY_NAME", "Twitch Chat" },
                { "BROTHER_BODY_SUBTITLE", $"Oppressor of {STREAMER_TOKEN}" }
            };
        }

        internal bool TryGetLocalizedStringByToken(string token, out string result)
        {
            if (StringsByToken.TryGetValue(token, out result))
            {
                result = result.Replace(STREAMER_TOKEN, StreamerName);
                return true;
            }
            return false;
        }
    }
}
