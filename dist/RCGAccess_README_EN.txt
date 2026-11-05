RCGAccess – Accessibility mod for River City Girls
Version 0.1.8

New in version 0.1.8
- Rewards (e.g. after a boss): every reward is announced completely ("1000 experience", "100 dollars",
  "School key") with "1 of 4" and "Continue with A". A shows the next reward.
- Phone (Start, the game's pause menu): on opening "Phone, the game is paused" and which page LT and RT lead to.
  Start closes the phone.
  - Character page: level and experience; slots name the food, description and effect. A eats it.
  - Map page: areas with "You are here", mission goals and visited rooms; quests with location, A opens the details.
    A on an area opens the area map (LT/RT switch, B goes back).
  - Moves page: name, type and the complete button sequence of every move.
  - Recruits page: name and description.
- New map markers are announced. Map markers are known right after entering a room.
- Cutscene rooms are named like the room itself. Double arrival announcements are filtered.

New in version 0.1.7
- Locked doors are clearly explained on arrival; no double arrival announcement.
- Enemy waves: many rooms send a new wave about 20 seconds after the last enemy; the mod now announces this.
- Door goal fixed (only the real door area, not the player spawn markers). Path finder retries with a slimmer body.

New in version 0.1.6
- Recruiting: a begging enemy becomes the navigator target; a short right stick press walks into it to grab it.
  While holding it: "Press LT now to recruit!"; success and failure are announced.
- Food can only be picked up when your health is not full (always full when invulnerable); food is no longer offered
  as a target then. Items you only have to touch say "Just walk over it". Guiding stops more precisely at items.
- New path finder for guiding: walks around columns, desks, crates and walls.
- The dead end hint also describes the way. No double "Arrived"; door hints at most every 15 seconds.

New in version 0.1.5
- Damage protection in the mod menu: off, half damage or invulnerable. Hits stay audible.
- Rooms marked on the phone map are now navigator targets (right stick up/down, listed first); a short right stick press
  guides you to the exit on the way there.

New in version 0.1.4
- No orientation sounds during videos, tutorials, transitions and scripted scenes.
- "Arrived" only once per target and never in the middle of a fight.
- Locked doors are called "locked" and listed last (remembered in RCGAccess\verschlossen.txt).
- Shorter weapon hints, at most every 20 seconds per weapon. Game over screen announces the selected option.
- Quest direction uses the markers of the phone map. Shift+F3 names the marked rooms.
- Begging enemies and armed enemies are announced; armed enemies knock brighter.

New in version 0.1.3
- Guiding is now on the RIGHT stick (short press); the game uses the left stick press for taunting. Hold the right stick: beacon on/off.
- Doors stay silent while a fight area is locked. Leaving a door's area plays a short falling tone; stay in the area and hold X.
- Guiding to a door walks into the middle of the door area.

New in version 0.1.2
- Doors: the mod now targets the area in front of the door where you have to stand (the game's interaction area).
- Guiding to a door says "Arrived" exactly when the game makes the door usable; then hold X.
- Room protocols (doors, walls, boundaries) are written to RCGAccess\Protokolle on room entry and when guiding is blocked.

New in version 0.1.1
- Enemy knock, attack warning and defeat sound clearly come from the enemy's side.
- Enemy defeated: falling triad. In fight areas also "2 left" for the last three enemies.
- Number of enemies on a button the game does not use (mod menu "Button for number of enemies"); keyboard Ctrl+F5.
- Pickups are announced (food with healing, items). Important items and food are announced after fights and
  become the navigator target; press the left stick to be guided there.
- No more announcements from hidden windows (closed phone, hidden pop-ups). Narration is read.
- Guiding to doors walks to the walkable spot in front of the door.

New in version 0.1.0
- First version: spoken menus, dialogs, tutorials and pop-ups, enemy radar with knocking, attack warning,
  health tones, navigator with beacon to exits, shops and people, guided walking, dead end hints,
  room announcements, pit warning, own mod menu (F11).


INSTALLATION
1. Extract the ZIP into the game folder (the folder containing RiverCityGirls.exe,
   on Steam usually C:\Program Files (x86)\Steam\steamapps\common\River City Girls).
2. Start the game. The mod loader BepInEx sets itself up on the first start, which may take a few seconds longer.
3. On the first start the mod says "RCGAccess 0.1.8 loaded" and opens its mod menu before the game continues.
   "Save and close" starts the game.

Requirements: Windows 10 or 11 with .NET Framework 4 (included in Windows) and at least one SAPI voice
in the game language. The mod always speaks the game language with a matching voice; languages without
mod texts get English mod texts.


MOD MENU (F11)
The game is paused while the menu is open.
- Select entry: arrow up/down, d-pad or left stick up/down
- Change value: arrow left/right, d-pad left/right
- Toggle or activate: Enter, Space or A
- Explain entry: F1 or Y
- Discard and close: Backspace or B
- Save and close: F11 or Start
- Ctrl alone stops speech, also in the menu.


KEYS IN GAME
Keyboard:
- Ctrl alone or F10: stop speech
- F9: repeat last announcement
- F7 / Shift+F7: faster / slower speech
- F2: read all visible texts; F3: repeat menu selection; F4: read button hints
- Shift+F3: active quests with location
- F5: status; Shift+F5: room with all exits and targets; Ctrl+F5: visible enemies
- F6: direction to the navigator target; Shift+F6: tone test
- F8: beacon on/off; Shift+F8 / Ctrl+Shift+F8: next / previous target; Ctrl+F8: guide me to the target
- F11: mod menu; Ctrl+F12: scene log (troubleshooting)

Controller (can be turned off in the mod menu, only during gameplay):
- Press right stick briefly: guide me to the target on/off
- Hold right stick: beacon on/off
- Right stick up / down: previous / next target
- Right stick left: room with all exits
- Right stick right: status
PlayStation names: LB/RB = L1/R1, LT/RT = L2/R2, stick press = L3/R3.


FINDING THE WAY UP OR DOWN
Exits are often at the top (further back) or bottom (front) of a room, or behind doors.
- Entering a room announces the room name and number of exits.
- Walking into a boundary plays noise from that side, then the mod tells you the next exit and its direction.
- Press the right stick (or Ctrl+F8) to be guided to the selected target automatically. Moving the left stick yourself
  takes over again; an enemy closer than 4 meters pauses guiding.
- The mod learns rooms and their exits (RCGAccess\raumkarte.txt). Once it knows the way to the location of the
  active quest, the matching exit is called "toward the quest" and listed first.


TONES
- Beacon: short tone from the target direction. Stereo = side, higher = target further up/back, lower = further down/front,
  faster = closer. Triangle for exits, square for the exit toward the quest, sine for shops and people.
- Enemy knock: soft "tock" of the nearest enemy. Stereo = side, higher = further up, faster = closer,
  quieter = less enemy energy. Bosses knock lower.
- Attack warning: two short, bright, hard tones from the side of an attacking enemy nearby.
- Enemy defeated: falling triad. Fight area locked: two low hard tones; cleared: rising two-tone.
- Damage: low squeaky square. Healing: rising tone. Below 30 % health: low double pulse every 2.5 seconds.
- Wall or boundary: noise from the side. Drop nearby: falling tone from the side.
- Interaction possible: short rising tone, then the announcement. Money: two bright tones. Target reached: rising glide.
Shift+F6 plays the tones.


TROUBLESHOOTING DATA
Run RCGAccess_Daten_sammeln.bat in the game folder. It creates a ZIP with date and time in its name on the desktop.


UNINSTALL
Delete winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt and the folders BepInEx and RCGAccess.


LINKS
- Donation: https://buymeacoffee.com/blindretrogamer/
- Accessibility Mods by Grybyx: https://github.com/grybyx/
