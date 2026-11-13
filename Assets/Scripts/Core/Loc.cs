using System;
using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// String tables for English (default) and Vietnamese.
    /// shortcut: in-code tables instead of the Unity Localization package; move to it when a 3rd language or translators arrive.
    /// </summary>
    public static class Loc
    {
        public static event Action Changed;

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
        {
            // key                       English                                   Tiếng Việt
            ["title"]          = new[] { "Pup Launch",                             "Hải Cẩu Bay" },
            ["tagline"]        = new[] { "Win back the village fish from the polar bears!", "Giành lại cá cho làng hải cẩu!" },
            ["play"]           = new[] { "Play",                                    "Chơi" },
            ["settings"]       = new[] { "Settings",                                "Cài đặt" },
            ["quit"]           = new[] { "Quit",                                    "Thoát" },
            ["map_title"]      = new[] { "Frost Bay",                               "Vịnh Băng Giá" },
            ["shop"]           = new[] { "Upgrades",                                "Nâng cấp" },
            ["stars"]          = new[] { "Stars",                                   "Sao" },
            ["music"]          = new[] { "Music",                                   "Nhạc" },
            ["sfx"]            = new[] { "Sound effects",                           "Hiệu ứng" },
            ["language"]       = new[] { "Language",                                "Ngôn ngữ" },
            ["lang_name"]      = new[] { "English",                                 "Tiếng Việt" },
            ["reset"]          = new[] { "Erase save data",                         "Xóa dữ liệu lưu" },
            ["reset_confirm"]  = new[] { "Erase all stars and upgrades?",           "Xóa hết sao và nâng cấp?" },
            ["yes"]            = new[] { "Yes",                                     "Có" },
            ["no"]             = new[] { "No",                                      "Không" },
            ["back"]           = new[] { "Back",                                    "Quay lại" },
            ["paused"]         = new[] { "PAUSE",                                   "TẠM DỪNG" },
            ["resume"]         = new[] { "Resume",                                  "Tiếp tục" },
            ["retry"]          = new[] { "Retry",                                   "Chơi lại" },
            ["map"]            = new[] { "Map",                                     "Bản đồ" },
            ["next"]           = new[] { "Next",                                    "Màn tiếp" },
            ["sound"]          = new[] { "Sound",                                   "Âm thanh" },
            ["win"]            = new[] { "Fortress smashed!",                       "Phá tan pháo đài!" },
            ["lose"]           = new[] { "The bears held on…",                      "Bầy gấu vẫn trụ…" },
            ["stars_gained"]   = new[] { "+{0} stars",                              "+{0} sao" },
            ["score"]          = new[] { "Score {0}",                               "Điểm {0}" },
            ["level_n"]        = new[] { "Level {0}",                               "Màn {0}" },
            ["through"]        = new[] { "{0} pups made it out!",                   "{0} hải cẩu thoát ra ngoài!" },
            ["through_one"]    = new[] { "1 pup made it out!",                      "1 hải cẩu thoát ra ngoài!" },
            ["none_through"]   = new[] { "No pup made it out!",                     "Không con nào thoát ra được!" },
            ["attack"]         = new[] { "Attack!",                                 "Tấn công!" },
            ["mission_n"]      = new[] { "Mission {0}",                             "Nhiệm vụ {0}" },
            ["hit_bears"]      = new[] { "Hit {0} bears to continue to the next level", "Hạ {0} gấu để sang màn tiếp theo" },
            ["congrats"]       = new[] { "Congratulations!",                       "Chúc mừng!" },
            ["continue"]       = new[] { "Continue",                                "Tiếp tục" },
            ["help_cry"]       = new[] { "HELP!",                                   "CỨU!" },
            ["total_label"]    = new[] { "Total",                                   "Tổng" },
            ["on"]             = new[] { "ON",                                      "BẬT" },
            ["off"]            = new[] { "OFF",                                     "TẮT" },
            ["total_bears"]    = new[] { "Bears on this level: {0}",                "Số gấu trong màn: {0}" },
            ["bears_hit"]      = new[] { "Bears knocked out: {0}/{1}",              "Gấu đã hạ: {0}/{1}" },
            ["mission_done"]   = new[] { "Mission complete!",                       "Hoàn thành nhiệm vụ!" },
            ["perfect_mission"]= new[] { "Perfect mission!",                        "Nhiệm vụ hoàn hảo!" },
            ["mission_failed"] = new[] { "Mission failed!",                         "Thất bại!" },
            ["bears_all_time"] = new[] { "Total bears: {0}",                        "Tổng gấu: {0}" },
            ["stats"]          = new[] { "Stats",                                   "Thống kê" },
            ["skip"]           = new[] { "Skip",                                    "Bỏ qua" },
            ["story"]          = new[] { "Story",                                   "Truyện" },
            ["story_1"]        = new[] { "The seal village was happy, with fish stored for the whole winter.", "Làng hải cẩu sống vui vẻ, kho cá đầy đủ cho cả mùa đông." },
            ["story_2"]        = new[] { "Then the polar bears came down from their ice fortress…", "Rồi bầy gấu bắc cực kéo xuống từ pháo đài băng…" },
            ["story_3"]        = new[] { "They rolled a giant snowball over the pups!", "Chúng lăn một quả cầu tuyết khổng lồ cuốn lấy bầy hải cẩu!" },
            ["story_4"]        = new[] { "The bears locked the pups in cages and took the winter fish.", "Gấu nhốt hải cẩu vào lồng và cướp hết kho cá." },
            ["story_5"]        = new[] { "Only Pip got away. Time to launch a rescue!", "Chỉ có Pip thoát được. Đến lúc phóng đi giải cứu!" },
            ["stats_total"]    = new[] { "Bears knocked out",                       "Tổng gấu đã hạ" },
            ["stats_x2"]       = new[] { "2 bears in one dive",                     "Hạ 2 gấu một lần" },
            ["stats_x3"]       = new[] { "3 bears in one dive",                     "Hạ 3 gấu một lần" },
            ["stats_x4"]       = new[] { "4+ bears in one dive",                    "Hạ 4+ gấu một lần" },
            ["stats_perfect"]  = new[] { "Perfect missions",                        "Màn hoàn hảo" },
            ["ok_hit"]         = new[] { "OK!",                                     "OK!" },
            ["bungee_tip"]     = new[] { "Bungee! Fire when the pup stretches lowest", "Dây nhún! Bắn lúc hải cẩu xuống thấp nhất" },
            ["great_hit"]      = new[] { "GREAT!",                                  "TUYỆT!" },
            ["oops_hit"]       = new[] { "OOPS!",                                   "ỐI!" },
            ["grr_miss"]       = new[] { "Grr…",                                    "Grr…" },
            ["buy"]            = new[] { "Buy",                                     "Mua" },
            ["maxed"]          = new[] { "Max",                                     "Tối đa" },
            ["lv"]             = new[] { "Lv {0}/{1}",                              "Cấp {0}/{1}" },
            ["up_Spiky"]       = new[] { "Ice Spikes",                              "Gai băng" },
            ["up_PuffUp"]      = new[] { "Bouncy Ball",                             "Cuộn tròn" },
            ["up_School"]      = new[] { "Pod",                                     "Bầy hải cẩu" },
            ["up_Ninja"]       = new[] { "Ninja Pup",                               "Hải cẩu ninja" },
            ["up_Blaze"]       = new[] { "Blazing Pup",                             "Hải cẩu lửa" },
            ["upd_Spiky"]      = new[] { "Smash through {0} ice blocks",            "Xuyên {0} khối băng" },
            ["upd_PuffUp"]     = new[] { "+{0} bounces after impact",               "+{0} lần nảy sau va chạm" },
            ["upd_School"]     = new[] { "+{0} pups per level",                     "+{0} hải cẩu mỗi màn" },
            ["upd_Ninja"]      = new[] { "Tap again mid-dive to stop and drop straight down, {0}× per pup", "Chạm lần nữa khi đang lao để khựng rồi rơi thẳng, {0} lần mỗi con" },
            ["upd_Blaze"]      = new[] { "Tap again mid-dive to catch fire and melt through {0} ice cubes", "Chạm lần nữa khi đang lao để bốc lửa, xuyên {0} khối băng" },
            ["how_to"]         = new[] { "How to play",                             "Cách chơi" },
            ["about"]          = new[] { "About",                                   "Giới thiệu" },
            ["hint_map"]       = new[] { "Tap a level to play",                     "Chạm vào một màn để chơi" },
            ["hint_aim_bungee"] = new[] { "Tap to fire! Hit the pup when it stretches lowest", "Chạm để bắn! Bắn lúc hải cẩu nhún xuống thấp nhất" },
            ["hint_aim"]       = new[] { "Tap to whack! Hit it as it swings over the catapult", "Chạm để đập! Đập lúc nó đu qua máy bắn" },
            ["hint_dive"]      = new[] { "Tap just before a bear: the pup noses down and drops!", "Chạm ngay trước con gấu: hải cẩu cắm đầu lao xuống!" },
            ["hint_shop"]      = new[] { "Tap an upgrade to see what it does",      "Chạm vào nâng cấp để xem tác dụng" },
            ["perfect"]        = new[] { "Perfect!",                                "Hoàn hảo!" },
            ["howto_1"]        = new[] { "Pups swing on the rope. Tap to swing the whacker: a good hit sends them up through the gap and on to the fortress.",
                                         "Hải cẩu đu trên dây. Chạm để máy đập vung búa, đánh chúng bay lọt khe góc trên bên phải, thẳng tới pháo đài." },
            ["howto_2"]        = new[] { "Every pup that gets out joins the attack on the bear fortress.",
                                         "Con nào bay lọt ra ngoài sẽ cùng tấn công pháo đài gấu." },
            ["howto_3"]        = new[] { "Pups fly across the bears' arena. Tap above a bear to nose-dive; bounce from bear to bear for a combo!",
                                         "Hải cẩu bay ngang đấu trường của gấu. Chạm khi ở trên con gấu để cắm đầu lao xuống; nảy từ gấu này sang gấu khác để ăn combo!" },
            ["howto_4"]        = new[] { "Knock out enough bears to pass, all of them for a Perfect badge. Stars buy upgrades.",
                                         "Hạ đủ gấu để qua màn, hạ hết để lấy huy hiệu Hoàn hảo. Sao dùng mua nâng cấp." },
            ["about_text"]     = new[] { "Polar bears stole the seal village's winter fish. Grandpa Walrus built the whacker so the pups can win it back.",
                                         "Bầy gấu bắc cực cướp kho cá mùa đông của làng hải cẩu. Ông Hải Mã chế ra máy đập để bầy hải cẩu con giành lại." },
            ["credits"]        = new[] { "Art & sounds: made for this game · Font: Baloo 2 (SIL OFL)",
                                         "Hình & âm thanh: tự làm cho game · Phông: Baloo 2 (SIL OFL)" },
        };

        static int lang = -1;

        public static string Language => Index == 1 ? "vi" : "en";

        static int Index
        {
            get
            {
                if (lang < 0)
                {
                    string saved = SaveSystem.Data.language;
                    lang = saved == "vi" || (string.IsNullOrEmpty(saved) && Application.systemLanguage == SystemLanguage.Vietnamese) ? 1 : 0;
                }
                return lang;
            }
        }

        public static void SetLanguage(string code)
        {
            lang = code == "vi" ? 1 : 0;
            SaveSystem.Data.language = Language;
            SaveSystem.Save();
            Changed?.Invoke();
        }

        public static string T(string key)
        {
            if (table.TryGetValue(key, out var row)) return row[Index];
            Debug.LogWarning($"[PufferLaunch] Missing text key '{key}'");
            return key;
        }

        public static string T(string key, params object[] args) => string.Format(T(key), args);
    }
}
