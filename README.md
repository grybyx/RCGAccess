# RCGAccess – Accessibility mod for River City Girls

**English** · [Deutsch weiter unten](#deutsch)

RCGAccess makes the beat 'em up **[River City Girls](https://store.steampowered.com/app/1049320/River_City_Girls/)** (Steam, PC) playable for blind and visually impaired players. The mod reads menus, dialogs and pop-ups in the game's language, through the modern Windows OneCore voices, classic SAPI voices or your own screen reader. It adds orientation sounds for enemies, exits, items and health, and a navigator that guides you to doors and goals.

> **Pre-release (beta), 0.3.5.** Tested into the second area (Crosstown). Bug reports are welcome, see [Reporting bugs](#reporting-bugs).

## About the game

River City Girls (WayForward, 2019) is a side-scrolling beat 'em up with role-playing elements and a spin-off of the classic River City / Kunio-kun series. When their boyfriends Kunio and Riki are kidnapped, the high school girls Kyoko and Misako fight their way through six districts of River City to get them back. You beat up gangs with combos, throws, weapons and special moves, level up and learn new moves, buy items in shops, take on side quests and recruit defeated enemies as helpers. The story is told in manga panels and anime cutscenes; play alone or in local co-op for two.

Only for **River City Girls** (2019). River City Girls Zero and River City Girls 2 are not supported.

## Download

Get the ready-to-use build from **[Releases](../../releases/latest)**: `RCGAccess_<version>.zip`. You do not need the source code in this repository to play.

## Features

- **Speech output (choice in the mod menu):**
  - OneCore (default): the modern Windows voices.
  - SAPI: classic SAPI voices, including 32-bit voices such as Eloquence.
  - Screen reader: NVDA, JAWS, ZDSR, System Access, ZoomText and others speak with their own voice and rate, also on the braille display. Without a running screen reader, OneCore speaks.
- **Speech:** menus with value and position, options, toggles, dialogs and speech bubbles, narration, tutorials, pop-ups, rewards and the game over choice. The mod detects the game language and picks a matching voice automatically.
- **Phone (pause menu):** every page, with a hint for LT and RT.
  - Character: level, experience, inventory with effects.
  - Map: areas, mission goals, quests with location.
  - Moves: full button sequence of every move.
  - Recruits.
- **Enemies:**
  - Radar: a knock comes from the side of the nearest enemy. It gets faster when the enemy is closer and quieter when it has less energy. Higher = enemy further up, lower = further down, bright click = enemy on your line.
  - Attack warning, triad on defeat, enemy count on a button.
  - Armed and begging enemies are announced, with help for recruiting.
- **Navigator:**
  - Beacon tones to exits, doors, shops, people and items. The right stick selects the target.
  - Guided walking with a path finder; runs on long stretches, jumps onto furniture and steps when needed and can walk straight through the door.
  - Dead end hints ("You can't go further right …").
  - The mod learns a room map and finds the way to the quest from the map markers.
- **Health and pickups:** tones for damage, healing and low health. Pickups and money are announced. Boss health is announced in steps.
- **Shops:** your money is announced on opening and after each purchase; items you cannot afford are marked.
- **Damage protection** (optional): half damage or invulnerable.
- **Mod menu (F11):** every feature can be switched on or off, features with their own sound play it, "Listen to all sounds" explains all sounds. It opens on the first start, before the game begins.

## Installation

1. Extract `RCGAccess_<version>.zip` into the game folder, the one containing `RiverCityGirls.exe`. On Steam this is usually `C:\Program Files (x86)\Steam\steamapps\common\River City Girls`.
2. Start the game normally. The mod loader BepInEx sets itself up on the first start.
3. The mod says "RCGAccess … loaded" and opens its menu. "Save and close" starts the game.

**Requirements:** Windows 10 or 11 and .NET Framework 4 (included in Windows). For OneCore a Windows voice in the game language, for SAPI a SAPI voice; with the screen reader setting your screen reader speaks.

**Uninstall:** delete `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt` and the folders `BepInEx` and `RCGAccess`.

## Controls (short)

| Controller | Function |
| --- | --- |
| Press right stick briefly | Guide me to the target on/off |
| Hold right stick | Beacon on/off |
| Right stick up / down | Previous / next target |
| Right stick left / right | Room with exits / status |
| View (chosen automatically) | Number of enemies |

| Keyboard | Function |
| --- | --- |
| F11 | Mod menu |
| Ctrl alone or F10 / F9 | Stop speech / repeat |
| F2 / F3 / F4 | Read screen / repeat selection / button hints |
| Shift+F3 | Active quests with location |
| F5 / Shift+F5 / Ctrl+F5 | Status / room with exits / enemies |
| F6 / Shift+F6 | Direction to target / tone test |
| F8 / Ctrl+F8 | Beacon / guided walking |

The full manual is included in the download: `RCGAccess_README_EN.txt` (English) and `RCGAccess_LIESMICH.txt` (German).

<a id="reporting-bugs"></a>
## Reporting bugs

After playing, run `RCGAccess_Daten_sammeln.bat` in the game folder. It puts a ZIP with logs on your desktop. Attach it to an [issue](../../issues), with one sentence about the problem and roughly when it happened.

## Building

`build.sh` compiles with Roslyn from PowerShell 7. It needs the game's `Managed` DLLs (`GAME_MANAGED`) and BepInEx 5.4.23 (`BEPINEX_CORE`). The game's DLLs are not part of this repository. The release also contains `prism.dll` (Windows x64) from the [Prism releases](https://github.com/ethindp/prism/releases).

## License and credits

- RCGAccess is released under the [MIT License](LICENSE). © 2026 Grybyx
- Speech output through [Prism](https://github.com/ethindp/prism) by Ethin Probst (MPL-2.0).
- The download contains [BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1), [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) (LGPL-2.1), [HarmonyX](https://github.com/BepInEx/HarmonyX) (MIT) and [Prism](https://github.com/ethindp/prism) (MPL-2.0). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- River City Girls © WayForward / Arc System Works. This is an unofficial fan mod, not affiliated with the rights holders.

**Links:** [More Accessibility Mods by Grybyx](https://github.com/grybyx/) · [Donation](https://buymeacoffee.com/blindretrogamer/)

---

<a id="deutsch"></a>
# RCGAccess – Accessibility Mod für River City Girls

RCGAccess macht das Beat 'em up **[River City Girls](https://store.steampowered.com/app/1049320/River_City_Girls/)** (Steam, PC) für blinde und sehbehinderte Spielerinnen und Spieler spielbar. Der Mod liest Menüs, Dialoge und Einblendungen in der Sprache des Spiels vor, wahlweise mit den modernen Windows-Stimmen (OneCore), klassischen SAPI-Stimmen oder über deinen Screenreader. Dazu kommen Orientierungstöne für Gegner, Ausgänge, Gegenstände und Gesundheit sowie ein Navigator, der dich zu Türen und Zielen führt.

> **Vorabversion (Beta), 0.3.5.** Getestet ist das Spiel bis in das zweite Gebiet (Crosstown). Fehlerberichte sind willkommen, siehe [Fehler melden](#fehler-melden).

## Über das Spiel

River City Girls (WayForward, 2019) ist ein seitlich scrollendes Beat 'em up mit Rollenspiel-Elementen und ein Ableger der klassischen River-City- bzw. Kunio-kun-Reihe. Als ihre Freunde Kunio und Riki entführt werden, prügeln sich die Schülerinnen Kyoko und Misako durch sechs Stadtteile von River City, um sie zurückzuholen. Du kämpfst gegen Banden mit Kombos, Würfen, Waffen und Spezialangriffen, steigst im Level auf, lernst neue Techniken, kaufst in Läden ein, erledigst Nebenaufgaben und rekrutierst besiegte Gegner als Helfer. Erzählt wird in Manga-Bildern und Anime-Zwischensequenzen; gespielt wird allein oder zu zweit an einem PC.

Nur für **River City Girls** (2019). River City Girls Zero und River City Girls 2 werden nicht unterstützt.

## Download

Die fertige Version gibt es unter **[Releases](../../releases/latest)**. Die Datei heißt `RCGAccess_<Version>.zip`. Der Quellcode in diesem Repository ist für das Spielen nicht nötig.

## Funktionen

- **Sprachausgabe (Auswahl im Mod-Menü):**
  - OneCore (Standard): die modernen Windows-Stimmen.
  - SAPI: klassische SAPI-Stimmen, auch 32-Bit-Stimmen wie Eloquence.
  - Screenreader: NVDA, JAWS, ZDSR, System Access, ZoomText und andere sprechen mit eigener Stimme und eigenem Tempo, auch auf der Braillezeile. Läuft kein Screenreader, spricht OneCore.
- **Sprache:** Menüs mit Wert und Position, Optionen, Schalter, Dialoge und Sprechblasen, Erzähltexte, Tutorials, Einblendungen, Belohnungen und Game-Over-Auswahl. Der Mod erkennt die Spielsprache und nimmt automatisch eine passende Stimme.
- **Handy (Pausemenü):** alle Seiten mit Hinweis auf LT und RT.
  - Charakter: Level, Erfahrung, Inventar mit Wirkung.
  - Karte: Gebiete, Missionsziele, Aufgaben mit Ort.
  - Bewegungen: komplette Tastenfolge jeder Technik.
  - Rekruten.
- **Gegner:**
  - Radar: Ein Klopfen kommt von der Seite des nächsten Gegners. Es wird schneller, je näher er ist, und leiser, je weniger Energie er hat. Höher = Gegner weiter oben, tiefer = weiter unten, heller Klick = Gegner auf deiner Linie.
  - Angriffswarnung, Dreiklang beim Sieg, Ansage der Anzahl auf Knopfdruck.
  - Bewaffnete und bettelnde Gegner werden angesagt, mit Hilfe beim Rekrutieren.
- **Navigator:**
  - Peiltöne zu Ausgängen, Türen, Läden, Personen und Gegenständen. Der rechte Stick wählt das Ziel.
  - Automatisches Hinführen mit Wegfinder; rennt auf langen Strecken, springt bei Bedarf auf Möbel und Stufen und geht auf Wunsch direkt durch die Tür.
  - Hinweis bei Sackgassen („Hier geht es nicht weiter nach rechts …“).
  - Der Mod lernt eine Raumkarte und erkennt den Weg zur Quest über die Kartenmarkierungen.
- **Gesundheit und Fundstücke:** Töne bei Schaden, Heilung und niedriger Gesundheit. Ansage von Eingesammeltem und Geld. Bossenergie in Schritten.
- **Läden:** Ansage des eigenen Geldes beim Öffnen und nach jedem Kauf, zu teure Waren werden genannt.
- **Schutz vor Schaden** (wählbar): halber Schaden oder unverwundbar.
- **Mod-Menü (F11):** Alle Funktionen lassen sich einzeln schalten, jede Funktion mit eigenem Ton spielt ihn vor, „Alle Töne anhören“ erklärt alle Töne. Beim ersten Start öffnet sich das Menü, bevor das Spiel beginnt.

## Installation

1. Den Inhalt von `RCGAccess_<Version>.zip` in den Spielordner entpacken, also dorthin, wo `RiverCityGirls.exe` liegt. Bei Steam ist das meist `C:\Program Files (x86)\Steam\steamapps\common\River City Girls`.
2. Das Spiel normal starten. Der Mod-Loader BepInEx richtet sich beim ersten Start ein.
3. Der Mod sagt „RCGAccess … geladen“ und öffnet sein Menü. Mit „Speichern und schließen“ geht es ins Spiel.

**Voraussetzungen:** Windows 10 oder 11 und .NET Framework 4 (ist bei Windows dabei). Für OneCore eine Windows-Stimme in der Spielsprache, zum Beispiel Microsoft Katja oder Hedda, für SAPI eine SAPI-Stimme; mit der Einstellung Screenreader spricht dein Screenreader.

**Deinstallation:** `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt` sowie die Ordner `BepInEx` und `RCGAccess` löschen.

## Steuerung (Kurzfassung)

| Controller | Funktion |
| --- | --- |
| Rechten Stick kurz drücken | Zum Ziel hinführen an/aus |
| Rechten Stick halten | Peilton an/aus |
| Rechter Stick hoch / runter | Vorheriges / nächstes Ziel |
| Rechter Stick links / rechts | Raum mit Ausgängen / Status |
| Ansicht (automatisch gewählt) | Anzahl der Gegner |

| Tastatur | Funktion |
| --- | --- |
| F11 | Mod-Menü |
| Strg allein oder F10 / F9 | Sprache stoppen / wiederholen |
| F2 / F3 / F4 | Bildschirm lesen / Auswahl wiederholen / Tastenhinweise |
| Umschalt+F3 | Aktive Quests mit Ort |
| F5 / Umschalt+F5 / Strg+F5 | Status / Raum mit Ausgängen / Gegner |
| F6 / Umschalt+F6 | Richtung zum Ziel / Tontest |
| F8 / Strg+F8 | Peilton / Hinführen |

Die vollständige Anleitung liegt im Download: `RCGAccess_LIESMICH.txt` (Deutsch) und `RCGAccess_README_EN.txt` (Englisch).

## Fehler melden

Nach dem Spielen `RCGAccess_Daten_sammeln.bat` im Spielordner starten. Die Datei legt eine ZIP mit Protokollen auf den Desktop. Diese ZIP bitte zusammen mit einem Satz zum Problem und der ungefähren Uhrzeit als [Issue](../../issues) einreichen.

## Selbst bauen

`build.sh` kompiliert mit Roslyn aus PowerShell 7. Dafür werden die `Managed`-DLLs des Spiels (`GAME_MANAGED`) und BepInEx 5.4.23 (`BEPINEX_CORE`) gebraucht. Die DLLs des Spiels sind nicht im Repository enthalten. Das Release enthält zusätzlich `prism.dll` (Windows x64) aus den [Prism-Releases](https://github.com/ethindp/prism/releases).

## Lizenz und Dank

- RCGAccess steht unter der [MIT-Lizenz](LICENSE). © 2026 Grybyx
- Sprachausgabe über [Prism](https://github.com/ethindp/prism) von Ethin Probst (MPL-2.0).
- Der Download enthält [BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1), [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) (LGPL-2.1), [HarmonyX](https://github.com/BepInEx/HarmonyX) (MIT) und [Prism](https://github.com/ethindp/prism) (MPL-2.0). Details stehen in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- River City Girls © WayForward / Arc System Works. Dies ist ein inoffizieller Fan-Mod ohne Verbindung zu den Rechteinhabern.

**Links:** [Weitere Accessibility Mods by Grybyx](https://github.com/grybyx/) · [Donation](https://buymeacoffee.com/blindretrogamer/)
