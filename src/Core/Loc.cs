using System;
using System.Collections.Generic;

namespace RCGAccess
{
    /// <summary>Mod texts in German and English. Game languages other than German use English mod texts.</summary>
    public static class Loc
    {
        private static readonly Dictionary<string, string[]> _t = new Dictionary<string, string[]>();

        public static bool IsGerman
        {
            get
            {
                if (Settings.ModLang == 1) return true;
                if (Settings.ModLang == 2) return false;
                if (GameInfo.LanguageKnown) return GameInfo.Language == LocalizationLanguages.German;
                return true;
            }
        }

        private static void A(string key, string de, string en) { _t[key] = new string[] { de, en }; }

        public static string TryT(string key)
        {
            string[] v;
            if (!_t.TryGetValue(key, out v)) return null;
            return IsGerman ? v[0] : v[1];
        }

        public static string T(string key, params object[] args)
        {
            string s = TryT(key);
            if (s == null) return key;
            if (args != null && args.Length > 0)
            {
                try { return string.Format(s, args); } catch { return s; }
            }
            return s;
        }

        static Loc()
        {
            A("start", "RCGAccess {0} geladen.", "RCGAccess {0} loaded.");
            A("on", "an", "on");
            A("off", "aus", "off");
            A("pos", "{0} von {1}", "{0} of {1}");
            A("lang_auto", "automatisch wie das Spiel", "automatic, like the game");
            A("voice_auto", "automatisch", "automatic");
            A("unit_m", "Meter", "meters");
            A("menu_first", "Willkommen bei RCGAccess, dem Zugänglichkeits-Mod für River City Girls. Bitte stelle hier die Funktionen ein. Pfeiltasten oder Steuerkreuz hoch und runter wählen, links und rechts ändern, Eingabe oder A schaltet, F1 oder Y erklärt den Eintrag. Speichern und schließen startet das Spiel.",
                "Welcome to RCGAccess, the accessibility mod for River City Girls. Please set up the features here. Arrow keys or d-pad up and down select, left and right change, Enter or A toggles, F1 or Y explains the entry. Save and close starts the game.");
            A("menu_open", "Mod-Menü. Das Spiel ist angehalten.", "Mod menu. The game is paused.");
            A("menu_saved", "Gespeichert. Weiter im Spiel.", "Saved. Back to the game.");
            A("menu_discarded", "Änderungen verworfen. Weiter im Spiel.", "Changes discarded. Back to the game.");
            A("url_opened", "Öffne {0} im Browser. Mit Alt und Tab kommst du zurück ins Spiel.", "Opening {0} in the browser. Use Alt and Tab to return to the game.");
            A("url_failed", "Der Browser ließ sich nicht öffnen. Die Adresse steht in der Anleitung: {0}", "The browser could not be opened. The address is in the manual: {0}");

            // menu entries
            A("m_modlang", "Sprache des Mods", "Mod language");
            A("m_modlang_help", "Automatisch spricht der Mod die Sprache des Spiels mit einer passenden Stimme. Deutsch oder English legt die Sprache fest.", "Automatic follows the game language with a matching voice. Deutsch or English fixes the language.");
            A("m_voice_de", "Deutsche Stimme", "German voice");
            A("m_voice_de_help", "SAPI-Stimme für Deutsch. Automatisch nimmt die erste deutsche Stimme.", "SAPI voice for German. Automatic picks the first German voice.");
            A("m_voice_en", "Englische Stimme", "English voice");
            A("m_voice_en_help", "SAPI-Stimme für Englisch. Automatisch nimmt die erste englische Stimme.", "SAPI voice for English. Automatic picks the first English voice.");
            A("m_rate", "Sprechtempo", "Speech rate");
            A("m_rate_help", "Tempo der Stimme von minus 5 bis 10. Im Spiel auch mit F7 und Umschalt plus F7.", "Speech rate from minus 5 to 10. In game also with F7 and Shift plus F7.");
            A("m_speechvol", "Lautstärke der Sprache", "Speech volume");
            A("m_speechvol_help", "Lautstärke der Mod-Stimme.", "Volume of the mod voice.");
            A("m_menus", "Menüs vorlesen", "Read menus");
            A("m_menus_help", "Liest den gewählten Menüeintrag mit Wert und Position, Bildschirmtitel und Wertänderungen.", "Reads the selected menu entry with value and position, screen titles and value changes.");
            A("m_dialogs", "Dialoge und Untertitel vorlesen", "Read dialogs and subtitles");
            A("m_dialogs_help", "Liest Gespräche mit Porträts und die Sprechblasen der Hauptfiguren und Bosse.", "Reads portrait conversations and the speech bubbles of main characters and bosses.");
            A("m_chatter", "Sprüche von Gegnern und Passanten vorlesen", "Read enemy and bystander chatter");
            A("m_chatter_help", "Liest die kurzen Sprüche von Schlägern und Passanten. Im Kampf oft sehr viele, darum standardmäßig aus.", "Reads short lines of thugs and bystanders. Often many in fights, so off by default.");
            A("m_hints", "Tutorials, Quests und Einblendungen", "Tutorials, quests and pop-ups");
            A("m_hints_help", "Liest Tutorial-Fenster, Quest-Meldungen, Gegenstände, Stufenaufstieg, Nachrichten und Abfragen.", "Reads tutorial windows, quest messages, items, level ups, messages and prompts.");
            A("m_interact", "Interaktionshinweise", "Interaction hints");
            A("m_interact_help", "Sagt an, wenn du vor einer Tür, einem Laden, einer Person, einer Waffe oder einem Gegenstand stehst, und welche Taste nötig ist.", "Announces when you stand at a door, shop, person, weapon or object, and which button to use.");
            A("m_radar", "Gegner-Radar mit Klopfen", "Enemy radar knocking");
            A("m_radar_help", "Der nächste Gegner klopft. Seite im Stereo, höher heißt weiter oben im Bild, schneller heißt näher, leiser heißt weniger Energie.", "The nearest enemy knocks. Stereo side, higher means further up, faster means closer, quieter means less energy.");
            A("m_knockvol", "Lautstärke Gegner-Klopfen", "Enemy knock volume");
            A("m_knockvol_help", "0 bis 200 Prozent, damit das Klopfen im Kampflärm hörbar bleibt. Jede Änderung spielt das Klopfen vor.", "0 to 200 percent so the knock stays audible in combat. Each change plays a preview.");
            A("m_radarrange", "Reichweite des Gegner-Radars", "Enemy radar range");
            A("m_radarrange_help", "Bis zu dieser Entfernung klopft der nächste Gegner.", "The nearest enemy knocks up to this distance.");
            A("m_enemyannounce", "Gegner und Kampfbereiche ansagen", "Announce enemies and fight areas");
            A("m_enemyannounce_help", "Sagt neue Gegner mit Seite und Entfernung, Bosse, gesperrte Kampfbereiche und deren Ende an.", "Announces new enemies with side and distance, bosses, locked fight areas and their end.");
            A("m_attackwarn", "Angriffswarnung", "Attack warning");
            A("m_attackwarn_help", "Zwei kurze helle Töne von der Seite des Gegners, wenn ein naher Gegner angreift. Zeit zum Blocken oder Ausweichen.", "Two short high tones from the enemy's side when a nearby enemy attacks. Time to block or dodge.");
            A("m_health", "Gesundheitstöne", "Health tones");
            A("m_health_help", "Ton bei Schaden und Heilung, tiefer Warnpuls unter 30 Prozent Gesundheit.", "Tone on damage and healing, low warning pulse below 30 percent health.");
            A("m_boss", "Boss-Energie ansagen", "Announce boss energy");
            A("m_boss_help", "Sagt die Energie eines Bosses bei 75, 50, 25 und 10 Prozent an.", "Announces boss energy at 75, 50, 25 and 10 percent.");
            A("m_room", "Raum beim Betreten ansagen", "Announce room on entry");
            A("m_room_help", "Sagt den Namen des Raums und die Zahl der Ausgänge an, und welcher Ausgang Richtung Quest führt, wenn der Mod den Weg schon kennt.", "Announces the room name, the number of exits and which exit leads toward the quest if the mod already knows the way.");
            A("m_deadend", "Hinweis, wenn es nicht weitergeht", "Dead end hint");
            A("m_deadend_help", "Rauschen von der Seite, wenn du gegen eine Grenze läufst. Danach sagt der Mod, wo der Weg weitergeht, zum Beispiel nach oben.", "Noise from the side when you walk into a boundary. Then the mod says where the way continues, for example upwards.");
            A("m_hazards", "Gruben-Warnung", "Pit warning");
            A("m_hazards_help", "Fallender Ton und Ansage, wenn du nahe an einer Grube oder Absturzkante bist.", "Falling tone and announcement near a pit or drop.");
            A("m_moneytones", "Ton beim Geldaufsammeln", "Money pickup tone");
            A("m_moneytones_help", "Zwei helle Töne, wenn du Geld aufsammelst.", "Two bright tones when you pick up money.");
            A("m_moneyspeak", "Geldbetrag ansagen", "Announce money amount");
            A("m_moneyspeak_help", "Sagt kurz nach dem Aufsammeln den Betrag an.", "Announces the amount shortly after picking it up.");
            A("m_tonevol", "Lautstärke der Orientierungstöne", "Orientation tone volume");
            A("m_tonevol_help", "Lautstärke aller Töne außer dem Gegner-Klopfen. Jede Änderung spielt den Peilton vor.", "Volume of all tones except the enemy knock. Each change plays the beacon.");
            A("m_padkeys", "Mod-Funktionen am Controller", "Mod functions on the controller");
            A("m_padkeys_help", "Rechten Stick kurz drücken: Hinführen zum Ziel. Rechten Stick halten: Peilton an oder aus. Rechter Stick hoch und runter: Ziel wechseln. Rechter Stick links: Raum und Ausgänge. Rechter Stick rechts: Status. Den linken Stick drücken benutzt das Spiel für die Verhöhnung.", "Press right stick briefly: guide me to the target. Hold right stick: beacon on or off. Right stick up and down: change target. Right stick left: room and exits. Right stick right: status. The game uses the left stick press for taunting.");
            A("guard_keys", "Pattinson, Schlüssel nehmen", "Pattinson, take the keys");
            A("toys_room", "Pattinson schläft. Auf dem Boden liegt quietschendes Spielzeug, {0} Stück. Trittst du darauf, wacht er auf und wirft dich raus. Hinführen geht um das Spielzeug herum; bei Spielzeug vor dir piept es hell. Bei Pattinson X halten, um die Schlüssel zu nehmen.", "Pattinson is asleep. There are {0} squeaky toys on the floor. Step on one and he wakes up and throws you out. Guiding walks around the toys; a toy in front of you beeps brightly. Hold X at Pattinson to take the keys.");
            A("toy_warn", "Vorsicht, Spielzeug {0}", "Careful, toy {0}");
            A("m_menusamples", "Töne im Mod-Menü vorspielen", "Play sounds in the mod menu");
            A("m_menusamples_help", "Steht die Auswahl im Mod-Menü auf einer Funktion mit eigenem Ton, spielt der Mod diesen Ton nach der Ansage vor, damit du ihn lernst.", "When the mod menu selection is on a feature with its own sound, the mod plays that sound after the announcement so you can learn it.");
            A("m_tonetest", "Alle Töne anhören", "Listen to all sounds");
            A("m_tonetest_help", "Spielt alle Töne des Mods nacheinander mit Erklärung, wie Umschalt+F6 im Spiel.", "Plays all sounds of the mod in a row with explanations, like Shift+F6 in the game.");
            A("m_linetone", "Heller Klick, wenn ein Gegner auf deiner Linie ist", "Bright click when an enemy is on your line");
            A("m_linetone_help", "Das Klopfen des nächsten Gegners bekommt einen hellen Klick, wenn er auf gleicher Höhe steht wie du, also weder weiter oben noch weiter unten. Dann treffen deine Angriffe. Steht er höher, klopft es deutlich höher, steht er tiefer, deutlich tiefer.", "The knock of the nearest enemy gets a bright click when it stands on your line, neither further up nor further down. Then your attacks hit. If it stands higher, the knock is clearly higher; lower, clearly lower.");
            A("m_walkrun", "Beim Hinführen rennen", "Run while guiding");
            A("m_walkrun_help", "Auf langen geraden Strecken rennt die Figur beim Hinführen, das spart Zeit. Kurz vor dem Ziel geht sie wieder.", "On long straight stretches the character runs while guiding, which saves time. Close to the goal it walks again.");
            A("m_autodoor", "Türen nach dem Hinführen öffnen", "Open doors after guiding");
            A("m_autodoor_help", "Führt dich das Hinführen zu einer offenen Tür, hält der Mod X für dich und du gehst direkt in den nächsten Raum. Bewegst du den linken Stick, bricht das ab.", "When guiding brings you to an open door, the mod holds X for you and you walk straight into the next room. Moving the left stick cancels it.");
            A("door_auto", "{0}, gehe hindurch.", "{0}, going through.");
            A("shop_money", "Dein Geld: {0} Dollar.", "Your money: {0} dollars.");
            A("npc_no", "Die Person reagiert gerade nicht. Versuch es gleich noch einmal.", "The person does not react right now. Try again in a moment.");
            A("npc_fight", "Während eines Kampfes redet hier niemand mit dir. Erst alle Gegner besiegen.", "Nobody talks to you during a fight. Defeat all enemies first.");
            A("save_hint", "Achtung: Das Spiel speichert nur beim Betreten eines Raums. Was du seitdem gemacht hast, etwa Einkäufe, geht beim Beenden verloren. Wechsle vorher einmal den Raum.", "Note: the game only saves when you enter a room. What you did since, such as purchases, is lost when quitting. Change rooms once before.");
            A("door_came", "Du stehst noch in der Tür, durch die du gekommen bist. Geh erst ein Stück weg, dann bringt dich X halten zurück.", "You are still standing in the door you came through. Step away first, then holding X takes you back.");
            A("shop_keys", "Rechter Stick rechts sagt dein Geld.", "Right stick right says your money.");
            A("shop_use_now", "A isst es sofort, B legt es ins Inventar.", "A eats it now, B puts it in your inventory.");
            A("shop_left", "Noch {0} Dollar.", "{0} dollars left.");
            A("shop_expensive", "zu teuer", "too expensive");
            A("speed_1", "langsam", "slow");
            A("speed_2", "mittel", "medium");
            A("speed_3", "schnell", "fast");
            A("m_autowalkstop", "Hinführen bei Gegnern anhalten", "Stop guiding near enemies");
            A("m_autowalkstop_help", "Das automatische Hinführen hält an, sobald ein Gegner näher als 4 Meter kommt.", "Guided walking stops as soon as an enemy is closer than 4 meters.");
            A("m_duckspeech", "Spiel leiser bei Sprache", "Duck game during speech");
            A("m_duckspeech_help", "Das Spiel wird leiser, solange die Mod-Stimme spricht.", "The game gets quieter while the mod voice speaks.");
            A("m_ducktones", "Spiel leiser bei Tönen", "Duck game during tones");
            A("m_ducktones_help", "Das Spiel wird auch bei Orientierungstönen leiser.", "The game also gets quieter during orientation tones.");
            A("m_ducklevel", "Lautstärke des Spiels beim Leiserwerden", "Game volume while ducked");
            A("m_ducklevel_help", "So laut bleibt das Spiel, während der Mod spricht.", "How loud the game stays while the mod speaks.");
            A("m_verbose", "Ausführliches Protokoll", "Verbose log");
            A("m_verbose_help", "Schreibt mehr Daten in das Protokoll. Nur zur Fehlersuche einschalten.", "Writes more data to the log. Only for troubleshooting.");
            A("m_everystart", "Mod-Menü bei jedem Start zeigen", "Show mod menu on every start");
            A("m_everystart_help", "Öffnet dieses Menü bei jedem Spielstart vor dem Spiel.", "Opens this menu before the game on every start.");
            A("m_donation", "Donation", "Donation");
            A("m_donation_help", "Öffnet die Spendenseite im Browser.", "Opens the donation page in the browser.");
            A("m_grybyx", "Accessibility Mods by Grybyx", "Accessibility Mods by Grybyx");
            A("m_grybyx_help", "Öffnet die Seite mit weiteren Zugänglichkeits-Mods.", "Opens the page with more accessibility mods.");
            A("m_save", "Speichern und schließen", "Save and close");
            A("m_save_help", "Speichert alle Einstellungen und setzt das Spiel fort. Auch mit F11 oder Start.", "Saves all settings and resumes the game. Also with F11 or Start.");
            A("m_discard", "Verwerfen und schließen", "Discard and close");
            A("m_discard_help", "Stellt alle Werte wie beim Öffnen wieder her. Auch mit Rücktaste oder B.", "Restores all values as they were when opening. Also with Backspace or B.");

            // buttons
            A("btn_lt", "linker Trigger", "left trigger");
            A("btn_rt", "rechter Trigger", "right trigger");
            A("btn_up", "hoch", "up");
            A("btn_down", "runter", "down");
            A("btn_left", "links", "left");
            A("btn_right", "rechts", "right");
            A("btn_ls", "linker Stick", "left stick");
            A("btn_rs", "rechter Stick", "right stick");
            A("btn_n", "Taste {0}", "button {0}");

            // directions and numbers
            A("meters", "{0} Meter", "{0} meters");
            A("meter1", "1 Meter", "1 meter");
            A("dir_right", "rechts", "right");
            A("dir_left", "links", "left");
            A("dir_up", "nach oben", "up");
            A("dir_down", "nach unten", "down");
            A("dir_here", "direkt hier", "right here");
            A("side_right", "rechts", "right");
            A("side_left", "links", "left");
            A("side_center", "vor dir", "in front of you");
            A("edir_left", "links", "left");
            A("edir_right", "rechts", "right");
            A("edir_up", "oben", "up");
            A("edir_down", "unten", "down");

            // texts
            A("portrait_hint", "Weiter mit A.", "Continue with A.");
            A("tutorial", "Tutorial", "Tutorial");
            A("no_focus", "Keine Auswahl bekannt.", "No selection known.");
            A("no_text", "Kein Text sichtbar.", "No text visible.");
            A("no_prompts", "Keine Tastenhinweise sichtbar.", "No button hints visible.");

            // combat
            A("player_ko", "Du bist K.O.", "You are knocked out.");
            A("money_gained", "{0} Dollar", "{0} dollars");
            A("enemy", "Gegner", "Enemy");
            A("enemy_new", "{0} {1}", "{0} {1}");
            A("enemies_n", "{0} Gegner. Nächster: {1} {2}", "{0} enemies. Nearest: {1} {2}");
            A("boss_appears", "Boss {0}, {1}", "Boss {0}, {1}");
            A("boss_defeated", "{0} besiegt!", "{0} defeated!");
            A("boss_hp", "{0}: noch {1} Prozent", "{0}: {1} percent left");
            A("lock_start", "Kampfbereich gesperrt. Besiege alle Gegner.", "Fight area locked. Defeat all enemies.");
            A("lock_end", "Bereich frei.", "Area cleared.");
            A("energy", "Energie {0} Prozent", "energy {0} percent");
            A("no_player", "Keine Spielfigur aktiv.", "No character active.");
            A("st_hp", "Gesundheit {0} Prozent, {1} von {2}", "Health {0} percent, {1} of {2}");
            A("st_level", "Stufe {0}", "Level {0}");
            A("st_money", "{0} Dollar", "{0} dollars");
            A("st_noenemies", "Keine Gegner sichtbar", "No enemies visible");
            A("st_enemies", "{0} Gegner sichtbar, nächster {1} {2}, Energie {3} Prozent", "{0} enemies visible, nearest {1} {2}, energy {3} percent");
            A("st_locked", "Kampfbereich gesperrt", "Fight area locked");

            A("enemy_thug", "Schläger", "Thug");
            A("enemy_cheerleader", "Cheerleaderin", "Cheerleader");
            A("enemy_tigerman", "Tigermann", "Tigerman");
            A("enemy_schoolboy", "Schüler", "Schoolboy");
            A("enemy_schoolgirl", "Schülerin", "Schoolgirl");
            A("enemy_cyborg", "Cyborg", "Cyborg");
            A("enemy_yakuzamale", "Yakuza", "Yakuza");
            A("enemy_yakuzaf", "Yakuza-Frau", "Yakuza woman");
            A("enemy_badcop", "Polizist", "Cop");
            A("enemy_zombieschoolboy", "Zombie", "Zombie");
            A("enemy_trash", "Müllmann", "Trash");
            A("enemy_waver", "Waver", "Waver");
            A("enemy_linda", "Linda", "Linda");

            // navigation
            A("exit_to", "Ausgang {1} nach {0}", "Exit {1} to {0}");
            A("door_to", "Tür nach {0}", "Door to {0}");
            A("unknown_room", "Unbekannter Raum", "Unknown room");
            A("unvisited", "noch nicht besucht", "not visited yet");
            A("quest_route", "Richtung Quest", "toward the quest");
            A("quest_dir", "Richtung Quest: {0}, {1}", "Toward the quest: {0}, {1}");
            A("shop", "Laden {0}", "Shop {0}");
            A("person", "Person {0}", "Person {0}");
            A("climb", "Kletterstelle", "Climbing spot");
            A("weapon", "Waffe {0}", "Weapon {0}");
            A("object", "Gegenstand {0}", "Object {0}");
            A("env_vendingmachine", "Verkaufsautomat", "Vending machine");
            A("env_poster", "Poster", "Poster");
            A("env_ball", "Ball", "Ball");
            A("env_box", "Kiste", "Box");
            A("no_targets", "Keine Ziele in diesem Raum.", "No targets in this room.");
            A("nav_on", "Peilton an", "Beacon on");
            A("nav_off", "Peilton aus", "Beacon off");
            A("arrived", "Angekommen: {0}.", "Arrived: {0}.");
            A("hint_hold", "{0} halten.", "Hold {0}.");
            A("hint_press", "{0} drücken.", "Press {0}.");
            A("hint_walk", "Weiter nach {0} laufen, um den Raum zu verlassen.", "Keep walking {0} to leave the room.");
            A("hint_climb", "Nach oben drücken zum Klettern.", "Push up to climb.");
            A("no_exits", "Keine Ausgänge gefunden", "No exits found");
            A("exits_n", "{0} Ausgänge", "{0} exits");
            A("exits_n_new", "{0} Ausgänge, davon {1} noch nicht besucht", "{0} exits, {1} not visited yet");
            A("pit_warn", "Vorsicht, Abgrund {0}", "Careful, drop {0}");
            A("walk_on", "Hinführen zu {0}, {1}. Bewege den linken Stick, um selbst zu steuern.", "Guiding to {0}, {1}. Move the left stick to take over.");
            A("walk_off", "Hinführen beendet.", "Guiding stopped.");
            A("walk_manual", "Hinführen beendet, du steuerst selbst.", "Guiding stopped, you are steering.");
            A("walk_enemy_no", "Gegner in der Nähe, erst kämpfen. Hinführen geht danach.", "Enemy nearby, fight first. Guiding works afterwards.");
            A("walk_enemy", "Hinführen angehalten, Gegner in der Nähe.", "Guiding paused, enemy nearby.");
            A("walk_lost", "Ziel verloren.", "Target lost.");
            A("walk_blocked", "Weg blockiert. Ziel {0}.", "Way blocked. Target {0}.");
            A("blocked", "Hier geht es nicht weiter {0}.", "You cannot go further {0}.");
            A("blk_right", "nach rechts", "right");
            A("blk_left", "nach links", "left");
            A("blk_up", "nach oben", "up");
            A("blk_down", "nach unten", "down");
            A("blocked_exit", "Nächster Ausgang: {0}, {1}.", "Next exit: {0}, {1}.");
            A("blocked_help", "Rechten Stick drücken führt dich hin.", "Press the right stick to be guided there.");
            A("blocked_lock", "Kampfbereich gesperrt. Erst alle Gegner besiegen.", "Fight area locked. Defeat all enemies first.");
            A("no_quest", "Keine aktive Quest.", "No active quest.");
            A("quests", "Quests", "Quests");
            A("quest_at", "{0}, Ort: {1}", "{0}, location: {1}");

            A("rate", "Tempo {0}", "Rate {0}");
            A("tonetest", "Alle Töne. Peilton zu einem Ausgang rechts, Peilton zum Ausgang Richtung Aufgabe links oben, Ziel erreicht. Gegner-Klopfen: auf deiner Linie mit Klick, oberhalb, unterhalb, schwacher Gegner. Angriffswarnung. Schaden, Heilung, wenig Leben. Gegner besiegt, Kampfbereich gesperrt, Kampfbereich frei. Abgrund. Spielzeug vor dir. Wand. Interaktion möglich, Bereich verlassen. Eingesammelt. Geld.", "All sounds. Beacon to an exit on the right, beacon to the exit toward the quest up left, target reached. Enemy knock: on your line with click, above, below, weak enemy. Attack warning. Damage, healing, low health. Enemy defeated, fight area locked, fight area cleared. Pit. Toy in front of you. Wall. Interaction possible, leaving the area. Picked up. Money.");
            A("protocol_saved", "Szenen-Protokoll gespeichert.", "Scene log saved.");
            // 0.1.1
            A("menu_updated", "RCGAccess wurde aktualisiert. Im Mod-Menü gibt es neue Einträge. Speichern und schließen setzt das Spiel fort.", "RCGAccess was updated. The mod menu has new entries. Save and close continues the game.");
            A("m_pickupspeak", "Eingesammeltes ansagen", "Announce pickups");
            A("m_pickupspeak_help", "Sagt beim Einsammeln, was du bekommen hast, zum Beispiel Essen mit Heilung oder einen Gegenstand. Dazu drei helle Töne. Geld hat eigene Töne.", "Says what you picked up, for example food with healing or an item, with three bright tones. Money has its own tones.");
            A("m_itemnav", "Wichtige Gegenstände ansagen und anpeilen", "Announce and target important items");
            A("m_itemnav_help", "Erscheint ein Gegenstand oder Essen, sagt der Mod nach dem Kampf, wo es liegt, und macht es zum Navigator-Ziel. Rechten Stick drücken führt dich hin.", "When an item or food appears, the mod says where it lies after the fight and makes it the navigator target. Press the right stick to be guided there.");
            A("m_countbtn", "Taste für Anzahl der Gegner", "Button for number of enemies");
            A("m_countbtn_help", "Diese Controller-Taste sagt, wie viele Gegner noch übrig sind und wo der nächste steht. Automatisch nimmt eine Taste, die das Spiel nicht benutzt. Auf der Tastatur: Strg plus F5.", "This controller button tells how many enemies are left and where the nearest one is. Automatic picks a button the game does not use. Keyboard: Ctrl plus F5.");
            A("door_locked", "Die Tür ist noch gesperrt.", "The door is still locked.");
            A("door_stay", "Bleib stehen, solange du hältst.", "Stay still while holding.");
            A("locked", "verschlossen", "locked");
            A("weapon_short", "{0}, {1}", "{0}, {1}");
            A("begging", "{0} bettelt, {1}. Anfassen zum Greifen, dann LT oder LB zum Rekrutieren.", "{0} is begging, {1}. Touch to grab, then LT or LB to recruit.");
            A("armed", "{0} hat {1}, {2}", "{0} holds {1}, {2}");
            A("cont_title", "Spiel vorbei.", "Game over.");
            A("cont_continue", "Fortsetzen", "Continue");
            A("cont_quit", "Beenden", "Quit");
            A("map_marks", "Kartenmarkierung: {0}", "Map marker: {0}");
            A("mark_here", "Die Kartenmarkierung ist in diesem Raum.", "The map marker is in this room.");
            A("m_damage", "Schutz vor Schaden", "Damage protection");
            A("m_damage_help", "Aus: normaler Schaden. Halber Schaden: jeder Treffer kostet nur die Hälfte. Unverwundbar: Treffer kosten keine Gesundheit, Stürze in Gruben auch nicht. Treffer hörst du trotzdem, bei Unverwundbar etwas leiser.", "Off: normal damage. Half damage: every hit costs only half. Invulnerable: hits and falls cost no health. You still hear hits, a bit softer when invulnerable.");
            A("dmg_half", "halber Schaden", "half damage");
            A("dmg_none", "unverwundbar", "invulnerable");
            A("room_goal_direct", "Kartenmarkierung {0}, über {1}", "Map marker {0}, through {1}");
            A("room_goal_via", "Weg zur Kartenmarkierung {0}, zuerst {1}", "Way to map marker {0}, first {1}");
            A("room_goal_unknown", "Kartenmarkierung {0}, Weg noch unbekannt, erkunde {1}", "Map marker {0}, way not known yet, explore {1}");
            A("then", "dann", "then");
            A("way", "Weg: {0}.", "Way: {0}.");
            A("hint_beggar", "Lauf in den Gegner hinein, um ihn zu greifen, dann LT.", "Walk into the enemy to grab it, then LT.");
            A("hint_touch", "Einfach darüber laufen.", "Just walk over it.");
            A("hint_food_full", "Deine Gesundheit ist voll, Essen kannst du erst aufheben, wenn du verletzt bist.", "Your health is full; you can only pick up food when hurt.");
            A("beggar_target", "Bettelnder Gegner {0}", "Begging enemy {0}");
            A("beg_guide", "Rechten Stick drücken führt dich hinein.", "Press the right stick to walk into it.");
            A("beg_over", "Der Gegner bettelt nicht mehr.", "The enemy is no longer begging.");
            A("grab_recruit", "{0} gegriffen. Jetzt LT drücken zum Rekrutieren!", "{0} grabbed. Press LT now to recruit!");
            A("grab", "{0} gegriffen.", "{0} grabbed.");
            A("recruited", "Rekrutiert! LT ruft die Hilfe später herbei.", "Recruited! LT calls for help later.");
            A("recruit_not_begging", "Rekrutieren geht nur, wenn der Gegner bettelt.", "You can only recruit an enemy while it begs.");
            A("recruit_busy", "Deine Hilfe ist gerade im Einsatz, rekrutieren geht erst danach.", "Your helper is busy right now; recruit afterwards.");
            A("door_locked_long", "Diese Tür ist verschlossen. Das Spiel öffnet sie erst später, nimm einen anderen Ausgang.", "This door is locked. The game opens it later; take another exit.");
            A("wave_done", "Alle Gegner hier besiegt. In etwa 20 Sekunden kommen neue, wenn du bleibst.", "All enemies here defeated. New ones arrive in about 20 seconds if you stay.");
            A("reward_first", "{0}: {1}.", "{0}: {1}.");
            A("reward_head", "Erhalten", "Received");
            A("reward_of", "{0} von {1}.", "{0} of {1}.");
            A("reward_next", "Weiter mit {0}.", "Continue with {0}.");
            A("reward_money", "{0} Dollar", "{0} dollars");
            A("reward_xp", "Erfahrung", "experience");
            A("map_new_marks", "Neue Kartenmarkierung: {0}.", "New map marker: {0}.");
            A("phone_open", "Handy, das Spiel ist pausiert", "Phone, the game is paused");
            A("phone_prev", "LT: {0}", "LT: {0}");
            A("phone_next", "RT: {0}", "RT: {0}");
            A("phone_close", "{0} schließt das Handy", "{0} closes the phone");
            A("phone_level", "Level {0}, Erfahrung {1} von {2}", "Level {0}, experience {1} of {2}");
            A("phone_world", "Gebiet {0}", "Area {0}");
            A("phone_world_locked", "noch nicht erkundet", "not explored yet");
            A("phone_world_open", "{0}: Gebietskarte", "{0}: area map");
            A("phone_here", "Du bist hier: {0}", "You are here: {0}");
            A("phone_marks_ex", "Missionsziel: {0}", "Mission goal: {0}");
            A("phone_marks_qu", "Weitere Markierung: {0}", "Other marker: {0}");
            A("phone_seen", "{0} von {1} Räumen besucht", "{0} of {1} rooms visited");
            A("phone_worldmap", "Gebietskarte {0}", "Area map {0}");
            A("phone_worldmap_keys", "LT und RT wechseln das Gebiet, B zurück", "LT and RT switch the area, B goes back");
            A("phone_quest", "Aufgabe: {0}", "Quest: {0}");
            A("phone_quest_loc", "Ort: {0}", "location: {0}");
            A("phone_quest_done", "erledigt", "done");
            A("phone_quest_open", "{0}: Einzelheiten", "{0}: details");
            A("phone_quest_back", "B zurück", "B goes back");
            A("phone_slot", "Fach {0}", "Slot {0}");
            A("phone_slot_locked", "gesperrt", "locked");
            A("phone_slot_empty", "leer", "empty");
            A("phone_slot_unknown", "Wirkung noch unbekannt", "effect not known yet");
            A("phone_slot_use", "{0}: essen", "{0}: eat");
            A("phone_stamina", "Leben", "health");
            A("move_dash", "Rennen", "run");
            A("move_plus", "plus", "plus");
            A("move_then", "dann", "then");
            A("tut_close", "Weiter mit A.", "Continue with A.");
            A("lang_auto_short", "automatisch", "automatic");
            A("none_free", "keine Taste frei", "no free button");
            A("btn_back", "Ansicht-Taste", "View button");
            A("bind_unknown", "Die Belegung des Spiels ist noch nicht bekannt.", "The game's button layout is not known yet.");
            A("bind_none", "Das Spiel benutzt keine dieser Zusatztasten.", "The game uses none of these extra buttons.");
            A("bind_used", "Das Spiel benutzt selbst: {0}.", "The game itself uses: {0}.");
            A("item", "Gegenstand", "Item");
            A("food", "{0}, Gesundheit plus {1} Prozent", "{0}, health plus {1} percent");
            A("picked", "Eingesammelt: {0}", "Picked up: {0}");
            A("left_n", "Noch {0}", "{0} left");
            A("count_lock", "Noch {0} Gegner im Kampfbereich, {1} sichtbar", "{0} enemies left in the fight area, {1} visible");
            A("count_room", "{0} Gegner im Raum, {1} sichtbar", "{0} enemies in the room, {1} visible");
            A("count_none", "Keine Gegner mehr", "No enemies left");
            A("count_nearest", "Nächster: {0} {1}", "Nearest: {0} {1}");
            A("item_target", "Gegenstand {0}", "Item {0}");
            A("item_appeared", "Gegenstand: {0}, {1}.", "Item: {0}, {1}.");
            A("items_appeared", "{0} Gegenstände.", "{0} items.");
            A("item_guide_help", "Rechten Stick drücken führt dich hin.", "Press the right stick to be guided there.");
            A("arrived_near", "Ganz nah an {0}, weiter geht es nicht. Ziel liegt {1}.", "Close to {0}, cannot get closer. Target is {1}.");
        }
    }
}
