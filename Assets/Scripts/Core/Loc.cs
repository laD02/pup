using System;
using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>
    /// String tables, one column per language in <see cref="Codes"/> order (English is the default).
    /// shortcut: in-code tables instead of the Unity Localization package; move to it when translators arrive.
    /// </summary>
    public static class Loc
    {
        public static event Action Changed;

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
        {
            // key                       en, vi, then de, fr, es, it, pl (EU listings, docs/ASO.md)
            ["title"]          = new[] { "Pup Launch",                             "Hải Cẩu Bay",
                                         "Pup Launch", "Pup Launch", "Pup Launch", "Pup Launch", "Pup Launch" },
            ["tagline"]        = new[] { "Win back the village fish from the polar bears!", "Giành lại cá cho làng hải cẩu!",
                                         "Hol den Fisch des Dorfes von den Eisbären zurück!", "Reprends le poisson du village aux ours polaires !", "¡Recupera el pescado del pueblo de los osos polares!", "Riprendi il pesce del villaggio dagli orsi polari!", "Odbierz niedźwiedziom polarnym ryby z wioski!" },
            ["play"]           = new[] { "Play",                                    "Chơi",
                                         "Spielen", "Jouer", "Jugar", "Gioca", "Graj" },
            ["settings"]       = new[] { "Settings",                                "Cài đặt",
                                         "Einstellungen", "Réglages", "Ajustes", "Impostazioni", "Ustawienia" },
            ["quit"]           = new[] { "Quit",                                    "Thoát",
                                         "Beenden", "Quitter", "Salir", "Esci", "Wyjdź" },
            ["map_title"]      = new[] { "Frost Bay",                               "Vịnh Băng Giá",
                                         "Frostbucht", "Baie Gelée", "Bahía Helada", "Baia Gelata", "Mroźna Zatoka" },
            ["shop"]           = new[] { "Upgrades",                                "Nâng cấp",
                                         "Upgrades", "Améliorations", "Mejoras", "Potenziamenti", "Ulepszenia" },
            ["stars"]          = new[] { "Stars",                                   "Sao",
                                         "Sterne", "Étoiles", "Estrellas", "Stelle", "Gwiazdki" },
            ["music"]          = new[] { "Music",                                   "Nhạc",
                                         "Musik", "Musique", "Música", "Musica", "Muzyka" },
            ["sfx"]            = new[] { "Sound effects",                           "Hiệu ứng",
                                         "Soundeffekte", "Effets sonores", "Efectos de sonido", "Effetti sonori", "Efekty dźwiękowe" },
            ["language"]       = new[] { "Language",                                "Ngôn ngữ",
                                         "Sprache", "Langue", "Idioma", "Lingua", "Język" },
            ["lang_name"]      = new[] { "English",                                 "Tiếng Việt",
                                         "Deutsch", "Français", "Español", "Italiano", "Polski" },
            ["reset"]          = new[] { "Erase save data",                         "Xóa dữ liệu lưu",
                                         "Spielstand löschen", "Effacer la sauvegarde", "Borrar partida", "Cancella salvataggio", "Usuń zapis gry" },
            ["reset_confirm"]  = new[] { "Erase all stars and upgrades?",           "Xóa hết sao và nâng cấp?",
                                         "Alle Sterne und Upgrades löschen?", "Effacer toutes les étoiles et améliorations ?", "¿Borrar todas las estrellas y mejoras?", "Cancellare tutte le stelle e i potenziamenti?", "Usunąć wszystkie gwiazdki i ulepszenia?" },
            ["yes"]            = new[] { "Yes",                                     "Có",
                                         "Ja", "Oui", "Sí", "Sì", "Tak" },
            ["no"]             = new[] { "No",                                      "Không",
                                         "Nein", "Non", "No", "No", "Nie" },
            ["back"]           = new[] { "Back",                                    "Quay lại",
                                         "Zurück", "Retour", "Atrás", "Indietro", "Wstecz" },
            ["paused"]         = new[] { "PAUSE",                                   "TẠM DỪNG",
                                         "PAUSE", "PAUSE", "PAUSA", "PAUSA", "PAUZA" },
            ["resume"]         = new[] { "Resume",                                  "Tiếp tục",
                                         "Weiter", "Reprendre", "Continuar", "Riprendi", "Wznów" },
            ["retry"]          = new[] { "Retry",                                   "Chơi lại",
                                         "Nochmal", "Rejouer", "Reintentar", "Riprova", "Jeszcze raz" },
            ["map"]            = new[] { "Map",                                     "Bản đồ",
                                         "Karte", "Carte", "Mapa", "Mappa", "Mapa" },
            ["next"]           = new[] { "Next",                                    "Màn tiếp",
                                         "Weiter", "Suivant", "Siguiente", "Avanti", "Dalej" },
            ["sound"]          = new[] { "Sound",                                   "Âm thanh",
                                         "Ton", "Son", "Sonido", "Suono", "Dźwięk" },
            ["win"]            = new[] { "Fortress smashed!",                       "Phá tan pháo đài!",
                                         "Festung zerstört!", "Forteresse détruite !", "¡Fortaleza destrozada!", "Fortezza distrutta!", "Twierdza rozbita!" },
            ["lose"]           = new[] { "The bears held on…",                      "Bầy gấu vẫn trụ…",
                                         "Die Bären haben gehalten…", "Les ours ont tenu bon…", "Los osos resistieron…", "Gli orsi hanno resistito…", "Niedźwiedzie się utrzymały…" },
            ["stars_gained"]   = new[] { "+{0} stars",                              "+{0} sao",
                                         "+{0} Sterne", "+{0} étoiles", "+{0} estrellas", "+{0} stelle", "Gwiazdki: +{0}" },
            ["score"]          = new[] { "Score {0}",                               "Điểm {0}",
                                         "Punkte {0}", "Score {0}", "Puntos {0}", "Punti {0}", "Wynik {0}" },
            ["level_n"]        = new[] { "Level {0}",                               "Màn {0}",
                                         "Level {0}", "Niveau {0}", "Nivel {0}", "Livello {0}", "Poziom {0}" },
            ["through"]        = new[] { "{0} pups made it out!",                   "{0} hải cẩu thoát ra ngoài!",
                                         "{0} Robben sind durch!", "{0} phoques sont passés !", "¡{0} focas han salido!", "{0} foche sono uscite!", "Wyleciało fok: {0}!" },
            ["through_one"]    = new[] { "1 pup made it out!",                      "1 hải cẩu thoát ra ngoài!",
                                         "1 Robbe ist durch!", "1 phoque est passé !", "¡1 foca ha salido!", "1 foca è uscita!", "Wyleciała 1 foka!" },
            ["none_through"]   = new[] { "No pup made it out!",                     "Không con nào thoát ra được!",
                                         "Keine Robbe ist durch!", "Aucun phoque n'est passé !", "¡Ninguna foca ha salido!", "Nessuna foca è uscita!", "Żadna foka nie wyleciała!" },
            ["attack"]         = new[] { "Attack!",                                 "Tấn công!",
                                         "Angriff!", "À l'attaque !", "¡Al ataque!", "All'attacco!", "Do ataku!" },
            ["mission_n"]      = new[] { "Mission {0}",                             "Nhiệm vụ {0}",
                                         "Mission {0}", "Mission {0}", "Misión {0}", "Missione {0}", "Misja {0}" },
            ["hit_bears"]      = new[] { "Hit {0} bears to continue to the next level", "Hạ {0} gấu để sang màn tiếp theo",
                                         "Besiege {0} Bären für das nächste Level", "Assomme {0} ours pour passer au niveau suivant", "Derriba {0} osos para pasar al siguiente nivel", "Abbatti {0} orsi per passare al livello successivo", "Pokonaj niedźwiedzie, by przejść dalej: {0}" },
            ["congrats"]       = new[] { "Congratulations!",                       "Chúc mừng!",
                                         "Glückwunsch!", "Bravo !", "¡Enhorabuena!", "Complimenti!", "Gratulacje!" },
            ["continue"]       = new[] { "Continue",                                "Tiếp tục",
                                         "Weiter", "Continuer", "Continuar", "Continua", "Dalej" },
            ["help_cry"]       = new[] { "HELP!",                                   "CỨU!",
                                         "HILFE!", "À L'AIDE !", "¡SOCORRO!", "AIUTO!", "POMOCY!" },
            ["total_label"]    = new[] { "Total",                                   "Tổng",
                                         "Gesamt", "Total", "Total", "Totale", "Razem" },
            ["on"]             = new[] { "ON",                                      "BẬT",
                                         "AN", "ON", "ACTIVADO", "ATTIVO", "WŁ." },
            ["off"]            = new[] { "OFF",                                     "TẮT",
                                         "AUS", "OFF", "DESACTIVADO", "DISATTIVO", "WYŁ." },
            ["total_bears"]    = new[] { "Bears on this level: {0}",                "Số gấu trong màn: {0}",
                                         "Bären in diesem Level: {0}", "Ours dans ce niveau : {0}", "Osos en este nivel: {0}", "Orsi in questo livello: {0}", "Niedźwiedzie w poziomie: {0}" },
            ["bears_hit"]      = new[] { "Bears knocked out: {0}/{1}",              "Gấu đã hạ: {0}/{1}",
                                         "Bären besiegt: {0}/{1}", "Ours assommés : {0}/{1}", "Osos derribados: {0}/{1}", "Orsi abbattuti: {0}/{1}", "Pokonane niedźwiedzie: {0}/{1}" },
            ["mission_done"]   = new[] { "Mission complete!",                       "Hoàn thành nhiệm vụ!",
                                         "Mission erfüllt!", "Mission accomplie !", "¡Misión cumplida!", "Missione compiuta!", "Misja wykonana!" },
            ["perfect_mission"]= new[] { "Perfect mission!",                        "Nhiệm vụ hoàn hảo!",
                                         "Perfekte Mission!", "Mission parfaite !", "¡Misión perfecta!", "Missione perfetta!", "Perfekcyjna misja!" },
            ["mission_failed"] = new[] { "Mission failed!",                         "Thất bại!",
                                         "Mission gescheitert!", "Mission échouée !", "¡Misión fallida!", "Missione fallita!", "Misja nieudana!" },
            ["bears_all_time"] = new[] { "Total bears: {0}",                        "Tổng gấu: {0}",
                                         "Bären gesamt: {0}", "Total d'ours : {0}", "Osos en total: {0}", "Orsi in totale: {0}", "Niedźwiedzie łącznie: {0}" },
            ["stats"]          = new[] { "Stats",                                   "Thống kê",
                                         "Statistik", "Statistiques", "Estadísticas", "Statistiche", "Statystyki" },
            ["ad_bonus_pup"] = new[] { "Ad: +1 pup", "Quảng cáo: +1 hải cẩu", "Werbung: +1 Robbe", "Pub : +1 phoque", "Anuncio: +1 foca", "Annuncio: +1 foca", "Reklama: +1 foka" },
            ["ad_none"] = new[] { "No ad right now", "Chưa có quảng cáo", "Keine Werbung da", "Pas de pub", "Sin anuncios ahora", "Nessun annuncio ora", "Brak reklamy" },
            ["privacy"]        = new[] { "Privacy",                                 "Quyền riêng tư",
                                         "Datenschutz", "Confidentialité", "Privacidad", "Privacy", "Prywatność" },
            ["skip"]           = new[] { "Skip",                                    "Bỏ qua",
                                         "Überspringen", "Passer", "Saltar", "Salta", "Pomiń" },
            ["story"]          = new[] { "Story",                                   "Truyện",
                                         "Geschichte", "Histoire", "Historia", "Storia", "Historia" },
            ["story_1"]        = new[] { "The seal village was happy, with fish stored for the whole winter.", "Làng hải cẩu sống vui vẻ, kho cá đầy đủ cho cả mùa đông.",
                                         "Das Robbendorf war glücklich, mit Fisch für den ganzen Winter.", "Le village des phoques était heureux, avec du poisson pour tout l'hiver.", "El pueblo de las focas era feliz, con pescado para todo el invierno.", "Il villaggio delle foche era felice, con pesce per tutto l'inverno.", "Wioska fok żyła szczęśliwie, z rybami na całą zimę." },
            ["story_2"]        = new[] { "Then the polar bears came down from their ice fortress…", "Rồi bầy gấu bắc cực kéo xuống từ pháo đài băng…",
                                         "Dann kamen die Eisbären aus ihrer Eisfestung herunter…", "Puis les ours polaires sont descendus de leur forteresse de glace…", "Entonces los osos polares bajaron de su fortaleza de hielo…", "Poi gli orsi polari scesero dalla loro fortezza di ghiaccio…", "Wtedy niedźwiedzie polarne zeszły ze swojej lodowej twierdzy…" },
            ["story_3"]        = new[] { "They rolled a giant snowball over the pups!", "Chúng lăn một quả cầu tuyết khổng lồ cuốn lấy bầy hải cẩu!",
                                         "Sie rollten einen riesigen Schneeball über die Robbenkinder!", "Ils ont roulé une énorme boule de neige sur les bébés phoques !", "¡Hicieron rodar una bola de nieve gigante sobre las crías!", "Fecero rotolare una palla di neve gigante sui cuccioli!", "Stoczyły olbrzymią śnieżkę prosto na małe foki!" },
            ["story_4"]        = new[] { "The bears locked the pups in cages and took the winter fish.", "Gấu nhốt hải cẩu vào lồng và cướp hết kho cá.",
                                         "Die Bären sperrten die Robben in Käfige und nahmen den Winterfisch.", "Les ours ont enfermé les phoques en cage et volé le poisson d'hiver.", "Los osos encerraron a las focas en jaulas y se llevaron el pescado.", "Gli orsi chiusero le foche in gabbia e presero il pesce d'inverno.", "Niedźwiedzie zamknęły foki w klatkach i zabrały zimowe ryby." },
            ["story_5"]        = new[] { "Only Pip got away. Time to launch a rescue!", "Chỉ có Pip thoát được. Đến lúc phóng đi giải cứu!",
                                         "Nur Pip ist entkommen. Zeit für eine Rettung!", "Seul Pip s'est échappé. Place au sauvetage !", "Solo Pip escapó. ¡Hora de lanzar el rescate!", "Solo Pip è scappato. È ora del salvataggio!", "Uciekł tylko Pip. Czas na akcję ratunkową!" },
            ["stats_total"]    = new[] { "Bears knocked out",                       "Tổng gấu đã hạ",
                                         "Besiegte Bären", "Ours assommés", "Osos derribados", "Orsi abbattuti", "Pokonane niedźwiedzie" },
            ["stats_x2"]       = new[] { "2 bears in one dive",                     "Hạ 2 gấu một lần",
                                         "2 Bären in einem Sturzflug", "2 ours en un plongeon", "2 osos en una caída", "2 orsi in un tuffo", "2 niedźwiedzie naraz" },
            ["stats_x3"]       = new[] { "3 bears in one dive",                     "Hạ 3 gấu một lần",
                                         "3 Bären in einem Sturzflug", "3 ours en un plongeon", "3 osos en una caída", "3 orsi in un tuffo", "3 niedźwiedzie naraz" },
            ["stats_x4"]       = new[] { "4+ bears in one dive",                    "Hạ 4+ gấu một lần",
                                         "4+ Bären in einem Sturzflug", "4+ ours en un plongeon", "4+ osos en una caída", "4+ orsi in un tuffo", "4+ niedźwiedzi naraz" },
            ["stats_perfect"]  = new[] { "Perfect missions",                        "Màn hoàn hảo",
                                         "Perfekte Missionen", "Missions parfaites", "Misiones perfectas", "Missioni perfette", "Perfekcyjne misje" },
            ["ok_hit"]         = new[] { "OK!",                                     "OK!",
                                         "OK!", "OK !", "¡OK!", "OK!", "OK!" },
            ["bungee_tip"]     = new[] { "Bungee! Fire when the pup stretches lowest", "Dây nhún! Bắn lúc hải cẩu xuống thấp nhất",
                                         "Bungee! Feuer, wenn die Robbe am tiefsten ist", "Élastique ! Tire quand le phoque est au plus bas", "¡Elástico! Dispara cuando la foca esté más abajo", "Elastico! Spara quando la foca è più in basso", "Gumka! Strzelaj, gdy foka jest najniżej" },
            ["great_hit"]      = new[] { "GREAT!",                                  "TUYỆT!",
                                         "SUPER!", "GÉNIAL !", "¡GENIAL!", "OTTIMO!", "ŚWIETNIE!" },
            ["oops_hit"]       = new[] { "OOPS!",                                   "ỐI!",
                                         "HOPPLA!", "OUPS !", "¡UY!", "OPS!", "UPS!" },
            ["grr_miss"]       = new[] { "Grr…",                                    "Grr…",
                                         "Grr…", "Grr…", "Grr…", "Grr…", "Grr…" },
            ["buy"]            = new[] { "Buy",                                     "Mua",
                                         "Kaufen", "Acheter", "Comprar", "Compra", "Kup" },
            ["maxed"]          = new[] { "Max",                                     "Tối đa",
                                         "Max", "Max", "Máx.", "Max", "Maks." },
            ["lv"]             = new[] { "Lv {0}/{1}",                              "Cấp {0}/{1}",
                                         "Stufe {0}/{1}", "Niv. {0}/{1}", "Nv. {0}/{1}", "Liv. {0}/{1}", "Poz. {0}/{1}" },
            ["up_Spiky"]       = new[] { "Ice Spikes",                              "Gai băng",
                                         "Eisstacheln", "Pics de glace", "Púas de hielo", "Punte di ghiaccio", "Lodowe kolce" },
            ["up_PuffUp"]      = new[] { "Bouncy Ball",                             "Cuộn tròn",
                                         "Hüpfball", "Boule rebondissante", "Bola saltarina", "Palla rimbalzante", "Skacząca piłka" },
            ["up_School"]      = new[] { "Pod",                                     "Bầy hải cẩu",
                                         "Rudel", "Bande", "Manada", "Branco", "Stado" },
            ["up_Ninja"]       = new[] { "Ninja Pup",                               "Hải cẩu ninja",
                                         "Ninja-Robbe", "Phoque ninja", "Foca ninja", "Foca ninja", "Foka ninja" },
            ["up_Blaze"]       = new[] { "Blazing Pup",                             "Hải cẩu lửa",
                                         "Feuer-Robbe", "Phoque de feu", "Foca de fuego", "Foca di fuoco", "Ognista foka" },
            ["upd_Spiky"]      = new[] { "Smash through {0} ice blocks",            "Xuyên {0} khối băng",
                                         "Durchbricht {0} Eisblöcke", "Traverse {0} blocs de glace", "Atraviesa {0} bloques de hielo", "Sfonda {0} blocchi di ghiaccio", "Przebija bloki lodu: {0}" },
            ["upd_PuffUp"]     = new[] { "+{0} bounces after impact",               "+{0} lần nảy sau va chạm",
                                         "+{0} Sprünge nach dem Aufprall", "+{0} rebonds après l'impact", "+{0} rebotes tras el impacto", "+{0} rimbalzi dopo l'impatto", "Odbicia po uderzeniu: +{0}" },
            ["upd_School"]     = new[] { "+{0} pups per level",                     "+{0} hải cẩu mỗi màn",
                                         "+{0} Robben pro Level", "+{0} phoques par niveau", "+{0} focas por nivel", "+{0} foche per livello", "Foki na poziom: +{0}" },
            ["upd_Ninja"]      = new[] { "Tap again mid-dive to stop and drop straight down, {0}× per pup", "Chạm lần nữa khi đang lao để khựng rồi rơi thẳng, {0} lần mỗi con",
                                         "Tippe im Sturzflug erneut: stoppen und senkrecht fallen, {0}× pro Robbe", "Touche encore en plongée pour t'arrêter et tomber tout droit, {0}× par phoque", "Toca otra vez en plena caída para frenar y caer en vertical, {0}× por foca", "Tocca di nuovo in picchiata per fermarti e cadere dritto, {0}× per foca", "Stuknij ponownie w locie, by stanąć i spaść pionowo, {0}× na fokę" },
            ["upd_Blaze"]      = new[] { "Tap again mid-dive to catch fire and melt through {0} ice cubes", "Chạm lần nữa khi đang lao để bốc lửa, xuyên {0} khối băng",
                                         "Tippe im Sturzflug erneut: Feuer fangen und {0} Eiswürfel schmelzen", "Touche encore en plongée pour t'enflammer et faire fondre {0} cubes de glace", "Toca otra vez en plena caída para arder y derretir {0} cubos de hielo", "Tocca di nuovo in picchiata per prendere fuoco e sciogliere {0} cubetti di ghiaccio", "Stuknij ponownie w locie, by zapłonąć i stopić kostki lodu: {0}" },
            ["how_to"]         = new[] { "How to play",                             "Cách chơi",
                                         "So wird gespielt", "Comment jouer", "Cómo jugar", "Come si gioca", "Jak grać" },
            ["about"]          = new[] { "About",                                   "Giới thiệu",
                                         "Info", "À propos", "Acerca de", "Info", "O grze" },
            ["hint_map"]       = new[] { "Tap a level to play",                     "Chạm vào một màn để chơi",
                                         "Tippe auf ein Level", "Touche un niveau pour jouer", "Toca un nivel para jugar", "Tocca un livello per giocare", "Stuknij poziom, aby zagrać" },
            ["hint_aim_bungee"] = new[] { "Tap to fire! Hit the pup when it stretches lowest", "Chạm để bắn! Bắn lúc hải cẩu nhún xuống thấp nhất",
                                         "Tippen zum Feuern! Schieß, wenn die Robbe am tiefsten ist", "Touche pour tirer ! Vise quand le phoque est au plus bas", "¡Toca para disparar! Hazlo cuando la foca esté más abajo", "Tocca per sparare! Colpisci quando la foca è più in basso", "Stuknij, by strzelić! Strzel, gdy foka jest najniżej" },
            ["hint_aim"]       = new[] { "Tap to whack! Hit it as it swings over the catapult", "Chạm để đập! Đập lúc nó đu qua máy bắn",
                                         "Tippen zum Schlagen! Triff, wenn sie über das Katapult schwingt", "Touche pour frapper ! Vise quand il passe au-dessus de la catapulte", "¡Toca para golpear! Hazlo cuando pase sobre la catapulta", "Tocca per colpire! Colpisci quando oscilla sopra la catapulta", "Stuknij, by uderzyć! Uderz, gdy foka przelatuje nad katapultą" },
            ["hint_dive"] = new[] { "Tap early! The pup dives forward at a slant", "Chạm sớm! Hải cẩu lao chéo về phía trước", "Früh tippen! Die Robbe stürzt schräg nach vorn", "Touche tôt ! Le phoque plonge en biais vers l'avant", "¡Toca pronto! La foca se lanza en diagonal hacia delante", "Tocca presto! La foca si tuffa in diagonale in avanti", "Stuknij wcześnie! Foka nurkuje ukosem do przodu" },
            ["hint_shop"]      = new[] { "Tap an upgrade to see what it does",      "Chạm vào nâng cấp để xem tác dụng",
                                         "Tippe auf ein Upgrade, um zu sehen, was es kann", "Touche une amélioration pour voir ce qu'elle fait", "Toca una mejora para ver qué hace", "Tocca un potenziamento per vedere cosa fa", "Stuknij ulepszenie, by zobaczyć, co daje" },
            ["perfect"]        = new[] { "Perfect!",                                "Hoàn hảo!",
                                         "Perfekt!", "Parfait !", "¡Perfecto!", "Perfetto!", "Perfekcyjnie!" },
            ["howto_1"]        = new[] { "Pups swing on the rope. Tap to swing the whacker: a good hit sends them up through the gap and on to the fortress.",
                                         "Hải cẩu đu trên dây. Chạm để máy đập vung búa, đánh chúng bay lọt khe góc trên bên phải, thẳng tới pháo đài.",
                                         "Robben schaukeln am Seil. Tippe, damit der Schläger zuschlägt: ein guter Treffer schickt sie durch die Lücke zur Festung.", "Les phoques se balancent sur la corde. Touche pour frapper : un bon coup les envoie par l'ouverture jusqu'à la forteresse.", "Las focas se balancean en la cuerda. Toca para golpear: un buen golpe las lanza por el hueco hasta la fortaleza.", "Le foche dondolano sulla corda. Tocca per colpire: un buon colpo le manda nel varco fino alla fortezza.", "Foki huśtają się na linie. Stuknij, by uderzyć: dobry cios posyła je przez szczelinę prosto do twierdzy." },
            ["howto_2"]        = new[] { "Every pup that gets out joins the attack on the bear fortress.",
                                         "Con nào bay lọt ra ngoài sẽ cùng tấn công pháo đài gấu.",
                                         "Jede Robbe, die hinauskommt, greift die Bärenfestung an.", "Chaque phoque qui sort rejoint l'attaque de la forteresse des ours.", "Cada foca que sale se une al ataque contra la fortaleza de los osos.", "Ogni foca che esce si unisce all'attacco alla fortezza degli orsi.", "Każda foka, która wyleci, dołącza do ataku na twierdzę niedźwiedzi." },
            ["howto_3"] = new[] { "Pups fly across the bears' arena. Tap before the bear: the pup dives forward at a slant. Bounce from bear to bear for a combo!", "Hải cẩu bay ngang đấu trường của gấu. Chạm trước khi tới con gấu: hải cẩu lao chéo về phía trước. Nảy từ gấu này sang gấu khác để ăn combo!", "Robben fliegen über die Arena der Bären. Tippe vor dem Bären: die Robbe stürzt schräg nach vorn. Spring von Bär zu Bär für eine Kombo!", "Les phoques survolent l'arène des ours. Touche avant l'ours : le phoque plonge en biais vers l'avant. Rebondis d'ours en ours pour un combo !", "Las focas cruzan la arena de los osos. Toca antes del oso: la foca se lanza en diagonal hacia delante. ¡Rebota de oso en oso para hacer combo!", "Le foche sorvolano l'arena degli orsi. Tocca prima dell'orso: la foca si tuffa in diagonale in avanti. Rimbalza da un orso all'altro per una combo!", "Foki przelatują nad areną niedźwiedzi. Stuknij przed niedźwiedziem: foka nurkuje ukosem do przodu. Odbijaj się od kolejnych, by zrobić combo!" },
            ["howto_4"]        = new[] { "Knock out enough bears to pass, all of them for a Perfect badge. Stars buy upgrades.",
                                         "Hạ đủ gấu để qua màn, hạ hết để lấy huy hiệu Hoàn hảo. Sao dùng mua nâng cấp.",
                                         "Besiege genug Bären zum Weiterkommen, alle für das Perfekt-Abzeichen. Mit Sternen kaufst du Upgrades.", "Assomme assez d'ours pour passer, tous pour le badge Parfait. Les étoiles achètent des améliorations.", "Derriba suficientes osos para pasar, todos para la insignia Perfecto. Las estrellas compran mejoras.", "Abbatti abbastanza orsi per passare, tutti per il distintivo Perfetto. Le stelle comprano potenziamenti.", "Pokonaj dość niedźwiedzi, by przejść, wszystkie dla odznaki Perfekcyjnie. Za gwiazdki kupisz ulepszenia." },
            ["about_text"]     = new[] { "Polar bears stole the seal village's winter fish. Grandpa Walrus built the whacker so the pups can win it back.",
                                         "Bầy gấu bắc cực cướp kho cá mùa đông của làng hải cẩu. Ông Hải Mã chế ra máy đập để bầy hải cẩu con giành lại.",
                                         "Eisbären haben den Winterfisch des Robbendorfs gestohlen. Opa Walross baute den Schläger, damit die Robben ihn zurückholen.", "Les ours polaires ont volé le poisson d'hiver du village des phoques. Papi Morse a construit la machine pour que les petits le récupèrent.", "Los osos polares robaron el pescado de invierno del pueblo de las focas. El abuelo Morsa construyó la máquina para que las crías lo recuperen.", "Gli orsi polari hanno rubato il pesce d'inverno del villaggio delle foche. Nonno Tricheco ha costruito la macchina perché i cuccioli lo riprendano.", "Niedźwiedzie polarne ukradły zimowe ryby wioski fok. Dziadek Mors zbudował maszynę, by małe foki je odzyskały." },
            ["credits"]        = new[] { "Art & sounds: made for this game · Font: Baloo 2 (SIL OFL)",
                                         "Hình & âm thanh: tự làm cho game · Phông: Baloo 2 (SIL OFL)",
                                         "Grafik & Sound: für dieses Spiel gemacht · Schrift: Baloo 2 (SIL OFL)", "Graphismes et sons : créés pour ce jeu · Police : Baloo 2 (SIL OFL)", "Gráficos y sonidos: hechos para este juego · Fuente: Baloo 2 (SIL OFL)", "Grafica e suoni: creati per questo gioco · Font: Baloo 2 (SIL OFL)", "Grafika i dźwięki: stworzone dla tej gry · Czcionka: Baloo 2 (SIL OFL)" },
            ["need_stars"] = new[] { "Stars missing: {0}", "Cần thêm {0} sao", "Fehlende Sterne: {0}", "Étoiles manquantes : {0}", "Estrellas que faltan: {0}", "Stelle mancanti: {0}", "Brakuje gwiazdek: {0}" },
            ["collect_fish"] = new[] { "Fly through the fish to collect them!", "Bay xuyên qua đàn cá để gom cá!", "Flieg durch die Fische und sammle sie ein!", "Vole à travers les poissons pour les ramasser !", "¡Vuela entre los peces para recogerlos!", "Vola tra i pesci per raccoglierli!", "Przeleć przez ryby, by je zebrać!" },
            ["fish_got"] = new[] { "Fish collected: {0}/{1}", "Cá đã gom: {0}/{1}", "Gesammelte Fische: {0}/{1}", "Poissons ramassés : {0}/{1}", "Peces recogidos: {0}/{1}", "Pesci raccolti: {0}/{1}", "Zebrane ryby: {0}/{1}" },
            ["ending"] = new[] { "Frost Bay is free! Now collect every star.", "Vịnh Băng Giá đã được giải cứu! Giờ hãy gom đủ sao.", "Die Frostbucht ist frei! Hol dir jetzt alle Sterne.", "La Baie Gelée est libre ! Récolte maintenant toutes les étoiles.", "¡La Bahía Helada es libre! Ahora consigue todas las estrellas.", "La Baia Gelata è libera! Ora raccogli tutte le stelle.", "Mroźna Zatoka jest wolna! Teraz zdobądź wszystkie gwiazdki." },
            ["star_rule_1"] = new[] { "Mission passed", "Qua màn", "Mission geschafft", "Mission réussie", "Misión superada", "Missione superata", "Misja zaliczona" },
            ["star_rule_2"] = new[] { "All bears down, a pup to spare", "Hạ hết gấu khi còn dư hải cẩu", "Alle Bären besiegt, eine Robbe übrig", "Tous les ours assommés, un phoque en réserve", "Todos los osos derribados y una foca de sobra", "Tutti gli orsi abbattuti, una foca di scorta", "Wszystkie niedźwiedzie pokonane, foka w zapasie" },
            ["star_rule_3"] = new[] { "60% of the fortress smashed", "Phá 60% pháo đài", "60 % der Festung zerstört", "60 % de la forteresse détruite", "60 % de la fortaleza destruida", "60% della fortezza distrutto", "60% twierdzy rozbite" },
            ["star_fish_1"] = new[] { "Run finished", "Hoàn thành", "Runde geschafft", "Manche terminée", "Ronda completada", "Giro completato", "Runda ukończona" },
            ["star_fish_2"] = new[] { "Half of the fish", "Gom nửa số cá", "Die Hälfte der Fische", "La moitié des poissons", "La mitad de los peces", "Metà dei pesci", "Połowa ryb" },
            ["star_fish_3"] = new[] { "80% of the fish", "Gom 80% số cá", "80 % der Fische", "80 % des poissons", "El 80 % de los peces", "L'80% dei pesci", "80% ryb" },
            ["retry_attack"] = new[] { "Attack again", "Tấn công lại", "Nochmal angreifen", "Réattaquer", "Atacar de nuevo", "Attacca di nuovo", "Atakuj ponownie" },
        };

        /// <summary>Language codes in table-column order; also the values stored in the save.</summary>
        public static readonly string[] Codes = { "en", "vi", "de", "fr", "es", "it", "pl" };

        static int lang = -1;

        public static string Language => Codes[Index];

        static int Index
        {
            get
            {
                if (lang < 0)
                {
                    string saved = SaveSystem.Data.language;
                    lang = string.IsNullOrEmpty(saved) ? FromSystem(Application.systemLanguage) : Math.Max(0, Array.IndexOf(Codes, saved));
                }
                return lang;
            }
        }

        static int FromSystem(SystemLanguage system) => system switch
        {
            SystemLanguage.Vietnamese => 1,
            SystemLanguage.German => 2,
            SystemLanguage.French => 3,
            SystemLanguage.Spanish => 4,
            SystemLanguage.Italian => 5,
            SystemLanguage.Polish => 6,
            _ => 0,
        };

        /// <summary>The language after the current one, for the Settings button that cycles through all of them.</summary>
        public static string NextLanguage => Codes[(Index + 1) % Codes.Length];

        public static void SetLanguage(string code)
        {
            lang = Math.Max(0, Array.IndexOf(Codes, code));
            SaveSystem.Data.language = Language;
            SaveSystem.Save();
            Changed?.Invoke();
        }

        /// <summary>Every key with a non-empty text per language (EditMode test).</summary>
        public static bool IsComplete(out string problem)
        {
            foreach (var row in table)
            {
                if (row.Value.Length != Codes.Length) { problem = $"{row.Key}: {row.Value.Length} columns"; return false; }
                for (int i = 0; i < Codes.Length; i++)
                    if (string.IsNullOrEmpty(row.Value[i])) { problem = $"{row.Key}: empty {Codes[i]}"; return false; }
            }
            problem = null;
            return true;
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
