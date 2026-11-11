# RCGAccess – Accessibility Mod für River City Girls

**Deutsch** · [English below](#english)

RCGAccess macht das Beat 'em up **[River City Girls](https://store.steampowered.com/app/1049320/River_City_Girls/)** (Steam, PC) für blinde und sehbehinderte Spielerinnen und Spieler spielbar. Der Mod liest Menüs, Dialoge und Einblendungen mit einer SAPI-Stimme in der Sprache des Spiels vor. Dazu kommen Orientierungstöne für Gegner, Ausgänge, Gegenstände und Gesundheit sowie ein Navigator, der dich zu Türen und Zielen führt.

> **Vorabversion (Beta), 0.1.9.** Getestet ist das Spiel bis einschließlich zum ersten Endgegner. Fehlerberichte sind willkommen, siehe [Fehler melden](#fehler-melden).

Nur für **River City Girls** (2019). River City Girls Zero und River City Girls 2 werden nicht unterstützt.

## Download

Die fertige Version gibt es unter **[Releases](../../releases/latest)**. Die Datei heißt `RCGAccess_<Version>.zip`. Der Quellcode in diesem Repository ist für das Spielen nicht nötig.

## Funktionen

- **Sprache:** Menüs mit Wert und Position, Optionen, Schalter, Dialoge und Sprechblasen, Erzähltexte, Tutorials, Einblendungen, Belohnungen und Game-Over-Auswahl. Der Mod erkennt die Spielsprache und nimmt automatisch eine passende Stimme.
- **Handy (Pausemenü):** alle Seiten mit Hinweis auf LT und RT.
  - Charakter: Level, Erfahrung, Inventar mit Wirkung.
  - Karte: Gebiete, Missionsziele, Aufgaben mit Ort.
  - Bewegungen: komplette Tastenfolge jeder Technik.
  - Rekruten.
- **Gegner:**
  - Radar: Ein Klopfen kommt von der Seite des nächsten Gegners. Es wird schneller, je näher er ist, und leiser, je weniger Energie er hat.
  - Angriffswarnung, Dreiklang beim Sieg, Ansage der Anzahl auf Knopfdruck.
  - Bewaffnete und bettelnde Gegner werden angesagt, mit Hilfe beim Rekrutieren.
- **Navigator:**
  - Peiltöne zu Ausgängen, Türen, Läden, Personen und Gegenständen. Der rechte Stick wählt das Ziel.
  - Automatisches Hinführen mit Wegfinder.
  - Hinweis bei Sackgassen („Hier geht es nicht weiter nach rechts …“).
  - Der Mod lernt eine Raumkarte und erkennt den Weg zur Quest über die Kartenmarkierungen.
- **Gesundheit und Fundstücke:** Töne bei Schaden, Heilung und niedriger Gesundheit. Ansage von Eingesammeltem und Geld. Bossenergie in Schritten.
- **Schutz vor Schaden** (wählbar): halber Schaden oder unverwundbar.
- **Mod-Menü (F11):** Alle Funktionen lassen sich einzeln schalten. Beim ersten Start öffnet sich das Menü, bevor das Spiel beginnt.

## Installation

1. Den Inhalt von `RCGAccess_<Version>.zip` in den Spielordner entpacken, also dorthin, wo `RiverCityGirls.exe` liegt. Bei Steam ist das meist `C:\Program Files (x86)\Steam\steamapps\common\River City Girls`.
2. Das Spiel normal starten. Der Mod-Loader BepInEx richtet sich beim ersten Start ein.
3. Der Mod sagt „RCGAccess … geladen“ und öffnet sein Menü. Mit „Speichern und schließen“ geht es ins Spiel.

**Voraussetzungen:** Windows 10 oder 11, .NET Framework 4 (ist bei Windows dabei) und eine SAPI-Stimme in der Spielsprache, zum Beispiel Microsoft Hedda für Deutsch.

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

`build.sh` kompiliert mit Roslyn aus PowerShell 7. Dafür werden die `Managed`-DLLs des Spiels (`GAME_MANAGED`) und BepInEx 5.4.23 (`BEPINEX_CORE`) gebraucht. Die DLLs des Spiels sind nicht im Repository enthalten.

## Lizenz und Dank

- RCGAccess steht unter der [MIT-Lizenz](LICENSE). © 2026 Grybyx
- Der Download enthält [BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1), [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) (LGPL-2.1) und [HarmonyX](https://github.com/BepInEx/HarmonyX) (MIT). Details stehen in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- River City Girls © WayForward / Arc System Works. Dies ist ein inoffizieller Fan-Mod ohne Verbindung zu den Rechteinhabern.

**Links:** [Weitere Accessibility Mods by Grybyx](https://github.com/grybyx/) · [Donation](https://buymeacoffee.com/blindretrogamer/)

---

<a id="english"></a>
# RCGAccess – Accessibility mod for River City Girls

RCGAccess makes the beat 'em up **[River City Girls](https://store.steampowered.com/app/1049320/River_City_Girls/)** (Steam, PC) playable for blind and visually impaired players. The mod reads menus, dialogs and pop-ups with a SAPI voice in the game's language. It adds orientation sounds for enemies, exits, items and health, and a navigator that guides you to doors and goals.

> **Pre-release (beta), 0.1.9.** Tested up to and including the first boss. Bug reports are welcome, see [Reporting bugs](#reporting-bugs).

Only for **River City Girls** (2019). River City Girls Zero and River City Girls 2 are not supported.

## Download

Get the ready-to-use build from **[Releases](../../releases/latest)**: `RCGAccess_<version>.zip`. You do not need the source code in this repository to play.

## Features

- **Speech:** menus with value and position, options, toggles, dialogs and speech bubbles, narration, tutorials, pop-ups, rewards and the game over choice. The mod detects the game language and picks a matching voice automatically.
- **Phone (pause menu):** every page, with a hint for LT and RT.
  - Character: level, experience, inventory with effects.
  - Map: areas, mission goals, quests with location.
  - Moves: full button sequence of every move.
  - Recruits.
- **Enemies:**
  - Radar: a knock comes from the side of the nearest enemy. It gets faster when the enemy is closer and quieter when it has less energy.
  - Attack warning, triad on defeat, enemy count on a button.
  - Armed and begging enemies are announced, with help for recruiting.
- **Navigator:**
  - Beacon tones to exits, doors, shops, people and items. The right stick selects the target.
  - Guided walking with a path finder.
  - Dead end hints ("You can't go further right …").
  - The mod learns a room map and finds the way to the quest from the map markers.
- **Health and pickups:** tones for damage, healing and low health. Pickups and money are announced. Boss health is announced in steps.
- **Damage protection** (optional): half damage or invulnerable.
- **Mod menu (F11):** every feature can be switched on or off. It opens on the first start, before the game begins.

## Installation

1. Extract `RCGAccess_<version>.zip` into the game folder, the one containing `RiverCityGirls.exe`. On Steam this is usually `C:\Program Files (x86)\Steam\steamapps\common\River City Girls`.
2. Start the game normally. The mod loader BepInEx sets itself up on the first start.
3. The mod says "RCGAccess … loaded" and opens its menu. "Save and close" starts the game.

**Requirements:** Windows 10 or 11, .NET Framework 4 (included in Windows) and a SAPI voice in the game language.

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

`build.sh` compiles with Roslyn from PowerShell 7. It needs the game's `Managed` DLLs (`GAME_MANAGED`) and BepInEx 5.4.23 (`BEPINEX_CORE`). The game's DLLs are not part of this repository.

## License and credits

- RCGAccess is released under the [MIT License](LICENSE). © 2026 Grybyx
- The download contains [BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1), [UnityDoorstop](https://github.com/NeighTools/UnityDoorstop) (LGPL-2.1) and [HarmonyX](https://github.com/BepInEx/HarmonyX) (MIT). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- River City Girls © WayForward / Arc System Works. This is an unofficial fan mod, not affiliated with the rights holders.

**Links:** [More Accessibility Mods by Grybyx](https://github.com/grybyx/) · [Donation](https://buymeacoffee.com/blindretrogamer/)
