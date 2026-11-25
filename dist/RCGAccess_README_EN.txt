RCGAccess – Accessibility mod for River City Girls
Version 0.4.8

New in version 0.4.8
- Mode select: the name of every option is now reliably read too ("Difficulty, Normal" instead of only "Normal").
- Weapon hints ("Weapon Bench, X") come at most every 15 seconds per kind of weapon, every 40 seconds in a fight,
  instead of again for every bench.
- Bus stop arrival points no longer show up as a second door in the navigator (e.g. "Door to Back Alley" in City
  Streets 2, "Door to Junk Yard" in the Transit Station) that guiding could never reach.

New in version 0.4.7
- Main menu, file select: on entering, the selected save file is read completely ("File A, Completion 35%,
  Downtown, City Streets") instead of "Sub-area" and jumbled texts of the other files.
- Mode select: every option is read with its name and value ("Number of Players, 1 Player"); the hint
  "How many players? >>" is left out. When toggling (e.g. Normal/Hard) the new value is said at once, also when
  switching back and forth quickly.
- Mission messages that appear while a save file is loaded in the main menu are no longer read.

New in version 0.4.6
- Ease boss mechanics, Hibari: the needle now really hits every time. Before, it often did not bounce at all because
  it hit you first; now it hits Hibari as soon as it comes close to you. Hibari stays stunned 6 seconds longer.
- Stunned boss as a navigator target: while Hibari lies stunned, she is at the top of the target list. Pressing the
  left stick leads you straight to her (also past enemies, running); "At Hibari. Attack!" once you are close on her
  line. Pressing the right stick guides you there with the tone. When she gets up, guiding ends.
- The mod menu is now also shown on screen (for sighted helpers): menu name, all entries with their values, the
  selected entry highlighted, its help and the controls below. Speech stays the same.

New in version 0.4.5
- New under Enemies and fighting: "Damage to bosses" (normal, double, fourfold, one hit per phase). Bosses have fixed
  fight phases; with "one hit per phase" each of your hits sends the boss into its next phase, two to four hits
  defeat it. A single hit for the whole fight is not possible because the game enforces the phases. Hibari can only
  be hurt while she lies stunned on the floor.
- New under Enemies and fighting: "Ease boss mechanics" (off). With Hibari the homing needle always hits her after
  its first bounce, wherever you stand; just run under the needle. Then she falls and is stunned: the triad and the
  announcement tell you where she lies.
- Hibari, homing needle without assist: the calculation now includes depth. The needle flies at an angle towards
  you and keeps drifting in depth after bouncing; if you are not on Hibari's line it misses her. The announcement
  therefore also says up or down ("Good spot: left 2 meters, 1 meter up"); the tone gets higher when you are too
  far up, lower when too far down. In 0.4.4 the arena was not detected, so it always said "no safe spot"; fixed.
- Several new enemies at once are all named, e.g. "2 new enemies: Schoolgirl left 3 meters, Schoolgirl right
  4 meters". Before, only one was named while the radar followed the nearer one, so the tone sometimes came from
  the right although "left" was announced.

New in version 0.4.4
- Hibari, homing needle: the mod calculates how the needle bounces off walls, floor and ceiling and finds the
  nearest spot from where it hits Hibari. Announcement "Good spot: right 3 meters", then a tone guides you there
  (from the spot's direction); a steady high tone = stay here. When she shoots: "Needle! Run under it!" (hit
  predicted) or "Needle! Dodge!".
- Hibari stunned: the announcement also says where she lies ("right 4 meters").
- Dropped items (e.g. an apple) no longer count as obstacles.

New in version 0.4.3
- Own volume for each sound: in the mod menu every toggle with a sound is directly followed by its volume (0 to 200
  percent), e.g. "Enemy radar", then "Enemy radar volume". New volumes for: high on-your-line tone, boss locator,
  fight sounds, attack warning, health, recruit, beacon and guided navigation, interaction, items, wall and
  obstacle, abyss and toy, pickup, money. "Volume of all sounds" (Sounds and volume) stays the base volume. Each
  change plays the sound.
- Boss announcements with explanation only the first time, then short ("Homing needle!", "Needle storm!",
  "Stunned, attack!").
- Hibari: with the homing needle the mod says which lower corner to go to (opposite Hibari) and how far it is.
  Run under the needle there, don't jump over it; then it bounces and hits Hibari.

New in version 0.4.2
- Boss locator (Enemies and fighting, on): the boss has its own sound, separate from the enemy radar, which then
  follows the other enemies. Low double hum from its side = not on your line, higher double hum = on your line,
  faster = closer. Rising tone = boss in the air, cannot be reached now. Bright double tone = can be hit now.
- Announce boss moves (Enemies and fighting, on). With Hibari: "needle storm, dodge", "homing needle coming at you"
  (plus fast high ticks from the needle's direction; lower when it flies back to Hibari), "Hit, Hibari is falling",
  "Hibari is stunned on the floor, attack now" with a triad, "3 seconds left", "Hibari is back up", "needle cyclone,
  keep away". Other bosses: the name of the move.
- How to beat Hibari: she mostly flies up high and cannot be hit then. Dodge the homing needle; when it bounces and
  hits Hibari, she falls and lies stunned on the floor for about 7 seconds. Attack then.
- Recruit: one LT press was announced twice ("comes to help" and "already fighting"); fixed. "1 heart".

New in version 0.4.1
- Quest areas as navigator targets: spots where something starts as soon as you walk in (a conversation, a quest
  item) are called "Quest area, walk into it" and come first in the target list. Afterwards "already entered".
  Example: in the pawn shop the area on the right at the counter.
- Sabu statues (side quest) are navigator targets: "Sabu statue, breakable". Attack it until it breaks.

New in version 0.4.0
- Enemy radar as an echo sounder: only one tone per beat. Low tone = enemy not on your line, high tone = on your
  line (now your attacks hit). Slow when it is far away, faster and faster as it comes closer. The extra click is gone.
- New toggle "Radar: up and down in the low tone" (Enemies and fighting, off): the low tone is then a bit higher for
  enemies further up and a bit lower for enemies further down.
- Obstacles instead of walls: walking into furniture, a crate or a car gives two dull knocks and "Obstacle, walk
  around it". Walls and room boundaries make noise as before. An enemy in the way no longer gives a wall sound.
- The log notes the depth distance of your hits (to fine-tune the line).
- New version numbers: 0.4.0 follows 0.3.9 (version 0.3.10 was a counting mistake).
- The mod menu opens once by itself because of the new toggle.

New in version 0.3.10
- Runway: automatic guiding (left stick) takes a run-up before an abyss by itself (a bit back, then running) and
  jumps at the edge. Abysses up to about 4.5 meters are planned (before 3.5, too little).
- Guiding never walks over an edge without jumping, it stops in front of it.
- Guided navigation at an abyss: announcement of the width and how to jump; at the edge a high double tone = press
  A now. Without a complete way it says so instead of "Almost there".
- Fixed an error that disturbed guiding after a fall.

New in version 0.3.9
- Guided navigation: press the right stick briefly (or Alt+F8). You walk yourself, the mod shows the way:
  - A guide tone comes from the side you should walk to (left/right).
  - Pitch: steady = you are on the line, lower = you are too far down, higher = you are too far up.
  - Hints for each stretch ("right 4 meters, 1 meter up"), "Obstacle, jump up" and "Abyss, take a run-up and
    jump". At the goal the arrival sound with a hint.
  - Press the right stick again to end it.
- Automatic guiding is now on the LEFT stick (press), also to begging enemies and items. The game's taunt on the
  left stick is off for this. Switchable in the mod menu (Navigation and guiding, "Left stick: automatic guiding");
  then the old layout applies.
- Holding the right stick still switches the beacon.
- Guiding jumps across abysses (up to about 3.5 meters, with a run-up), e.g. on the runway.
- "Careful, drop" is said only once per abyss until you have moved away; the warning sound comes less often.
- The mod menu opens once by itself because of the new entry.

New in version 0.3.8
- Recruits (new toggle "Recruit announcements" under Enemies and fighting, on):
  - LT or LB calls your recruit: sound and "Reina comes to help, 3 of 3 hearts".
  - If nothing happens, the mod says why: no recruit, no hearts left, already fighting or not ready yet (with seconds).
  - Announcement when the recruit is hit (hearts left) and when it is ready again.
  - The status (F5 or right stick right) names the recruit with hearts and whether it is ready.
- "You fell" only comes for a real fall into the depth, no longer when stepping down from furniture, steps or stages.

New in version 0.3.7
- Exits you use by walking into them (e.g. in the construction building, onward from the elevator) now have the right
  goal: guiding walks to a spot really inside the exit area instead of stopping at its edge.
- The room protocol also lists the room's scripts and cutscenes (helps with troubleshooting).

New in version 0.3.6
- Mod menu with submenus: Speech and voice, Reading, Enemies and fighting, Navigation and guiding, Sounds and volume,
  Advanced. A or Enter opens a submenu, B or Backspace (or the entry "Back") goes back. Donation, Accessibility Mods
  by Grybyx, Save and Discard stay in the main menu.
- Only one entry "Voice for …": it lists the voices of the language the mod speaks now (the game language).
  Your previous choice is kept.
- Guiding detects abysses and holes in the floor and walks around them (e.g. at the construction site). If you
  fall anyway, guiding ends with "You fell" instead of walking on from the respawn point.
- Climbing spots: guiding walks right to the climbing spot and climbs up (or down from the top) by itself, then says
  "Reached the top". If it does not work, the mod tells you.
- The mod menu opens once by itself on the next start because of the new layout.

New in version 0.3.5
- New speech output choice in the mod menu (second entry "Speech output"), built with Prism by Ethin Probst:
  - OneCore, Windows voices (default, recommended): the modern Windows voices such as Microsoft Zira or David.
  - SAPI: the classic SAPI voices as before, including 32-bit voices such as Eloquence.
  - Screen reader: your running screen reader (NVDA, JAWS, ZDSR, System Access, ZoomText and others) speaks with its
    own voice and rate, and texts also appear on the braille display. If no screen reader is running, OneCore speaks
    and the mod tells you so.
- The choice applies immediately, you hear the new output right in the menu.
- The German and English voice are remembered separately for OneCore and SAPI.
- The mod menu opens once by itself on the next start because of the new entry.

New in version 0.3.4
- Arrival door: the guard against unwanted going back now also works when the game offers the door right as you appear
  (mall: shops and food court).
- "Way blocked" at locked doors or fight areas now says "Way closed … closed for now, usually until all enemies are
  defeated or the story opens it".

New in version 0.3.3
- Guiding can now jump onto furniture and steps when the way leads over them (e.g. in Pattinson's house over the couch
  and shelves to Pattinson, or onto the step in front of the backyard door). It still prefers walking around low obstacles.
- Pattinson's house: after taking the keys there are no more toy warnings and jumps.

New in version 0.3.2
- Pattinson's house: the squeaky spots run across the room, you have to jump over them. Guiding looks for the narrowest
  place, takes a run-up and jumps by itself (A). Jump yourself with A; the warning now says "Careful, toy …, jump over it with A".

New in version 0.3.1
- Pattinson's house: the squeaky spots are long diagonal strips. The mod now checks their real shape instead of a rough
  rectangle; guiding finds the way between them, and "Careful, toy" only comes when a spot really lies in front of you.
  The last bit to the goal never cuts across such a spot.

New in version 0.3.0
- Learning the sounds: in the mod menu every feature with its own sound plays that sound after the announcement
  (switch off with "Play sounds in the mod menu"). New entry "Listen to all sounds" plays all sounds with
  explanations, like Shift+F6.
- Pattinson's house: Pattinson is asleep, squeaky toys wake him and he throws you out. The mod says so when you enter,
  warns with a bright double beep and "Careful, toy left" when a toy lies right in front of you, and guiding walks
  around the toys. New target "Pattinson, take the keys": hold X there.
- Because of the new entries, the mod menu opens once by itself on the next start.

New in version 0.2.9
- Enemies on your line: if the nearest enemy stands at the same depth as you (neither further up nor further down),
  every knock is followed by a bright click; then your attacks hit. Higher up knocks clearly higher, further down
  clearly lower. Can be switched off in the mod menu ("Bright click when an enemy is on your line").
- People and shops: holding X is enough now; the mod prevents the punch that used to interrupt the holding (before,
  only X together with block helped). With an enemy closer than 3 meters, X punches as usual.
- Because of the new entry, the mod menu opens once by itself on the next start.

New in version 0.2.8
- Dialogs and subtitles are only read when switched on in the mod menu, also in conversations with people.
- Doors: guiding only ends when you really stand in the door area; "Open doors after guiding" then opens the door
  reliably. The goal now lies a little inside the door area instead of on its edge.

New in version 0.2.7
- People: if you hold X at a person for more than 1.5 seconds and the game does not start a conversation, the mod starts
  it (the game's rules still apply, e.g. no conversation during a fight). The reason is written to the log.

New in version 0.2.6
- People, shops and objects: guiding only ends when the game offers the interaction, and says correctly whether
  X has to be pressed or held (people like Godai: hold X).

New in version 0.2.5
- Nobody talks during a fight: the mod now says so instead of staying silent.

New in version 0.2.4
- No more unwanted going back: after changing rooms you stand in the area of the door you came through. X (held or
  pressed for attacks) no longer takes you straight back there. Step away a little first, then it works as usual.
  If you select that door in the navigator and let it guide you, it works right away.
- New enemies are announced with the side they are on at the moment of speaking; outdated announcements are dropped.
- Guiding walks around benches, garbage cans and other big objects.
- Title screen options: switches, text speed and volumes say the chosen value.
- "Save & Quit" warns: the game only saves when you enter a room.
- Shop names are also correct when you step up to a shop.

New in version 0.2.3
- Shops: your money is announced as soon as the shop's own announcement has finished and is no longer cut off.
  After each purchase "… dollars left" follows. In a shop, right stick right or F5 says your money at any time.
- After buying, the game asks "Use now?": the mod adds "A eats it now, B puts it in your inventory."
- "???" on items now reads "effect not known yet" (the game shows the effect only after eating it once).

New in version 0.2.2
- Shops now have their correct names (before, some stands were called "Tony's Toiletries", e.g. Merv's Burger and
  Discount Sushi in front of the mall).

New in version 0.2.1
- Begging enemies no longer count as "enemy nearby": they no longer block guiding, the beacon or fight detection.
- Shops: "Your money" is now reliably announced when entering a shop.

New in version 0.2.0
- Game settings: switches say their name and the chosen value ("Subtitles: On"), as do text speed and volumes in
  percent. Changing a value announces the new one.
- Shops: on opening "Your money: … dollars", after each purchase "… dollars left". Items you cannot afford say "too expensive".
- Guiding is faster: on long straight stretches the character runs (mod menu "Run while guiding").
- Doors: guiding walks the last bit straight into the door area and then holds X for you, so you go right into the
  next room (mod menu "Open doors after guiding"). Moving the left stick cancels it.
- Because of the new entries, the mod menu opens once by itself on the next start.

New in version 0.1.9
- Beacon: if guiding switched the beacon on, it goes off again when guiding ends. It only stays on if you switched it on
  yourself (hold the right stick or F8).
- The beacon is silent while an enemy is closer than about 4 meters; the enemy knock has priority.
- Guiding does not start at all with an enemy nearby: "Enemy nearby, fight first."
- Phone, area map: after A on an area, the area, your location, mission goals and visited rooms are announced.

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
3. On the first start the mod says "RCGAccess 0.4.4 loaded" and opens its mod menu before the game continues.
   "Save and close" starts the game.

Requirements: Windows 10 or 11 with .NET Framework 4 (included in Windows) and a voice in the game language:
a Windows voice for OneCore (Settings, Time & language, Speech), or a SAPI voice for SAPI. With the setting
Screen reader, your screen reader speaks. The mod always speaks the game language with a matching voice; languages without
mod texts get English mod texts.


MOD MENU (F11)
The game is paused while the menu is open.
- Select entry: arrow up/down, d-pad or left stick up/down
- Change value: arrow left/right, d-pad left/right
- Toggle, activate or open a submenu: Enter, Space or A
- Explain entry: F1 or Y
- Back from a submenu: Backspace or B (or the entry "Back")
- Discard and close: Backspace or B in the main menu
- Save and close: F11 or Start
- Ctrl alone stops speech, also in the menu.

Main menu:
- Speech and voice: mod language, speech output (OneCore, SAPI or screen reader), voice for the current language,
  speech rate, speech volume.
- Reading: menus, dialogs, chatter, tutorials and pop-ups, room announcement, pickups, money amount.
- Enemies and fighting: radar, bright click on your line, knock volume, radar range, enemy announcements, attack
  warning, health sounds, boss health, enemy count button, damage protection, damage to bosses, ease boss mechanics.
- Navigation and guiding: mod functions on the controller, interaction hints, items as targets, dead end hints,
  pit warning, pause guiding for enemies, run while guiding, open doors.
- Sounds and volume: orientation sound volume, money sounds, lower the game during speech or sounds, how much,
  play sounds in the menu, listen to all sounds.
- Advanced: detailed log, mod menu on every start.
- Donation, Accessibility Mods by Grybyx, Save and close, Discard and close.


KEYS IN GAME
Keyboard:
- Ctrl alone or F10: stop speech
- F9: repeat last announcement
- F7 / Shift+F7: faster / slower speech
- F2: read all visible texts; F3: repeat menu selection; F4: read button hints
- Shift+F3: active quests with location
- F5: status; Shift+F5: room with all exits and targets; Ctrl+F5: visible enemies
- F6: direction to the navigator target; Shift+F6: tone test
- F8: beacon on/off; Shift+F8 / Ctrl+Shift+F8: next / previous target; Ctrl+F8: automatic guiding; Alt+F8: guided navigation
- F11: mod menu; Ctrl+F12: scene log (troubleshooting)

Controller (can be turned off in the mod menu, only during gameplay):
- Press right stick briefly: guided navigation on/off (you walk, tone and hints show the way)
- Press left stick: automatic guiding on/off
- Hold right stick: beacon on/off
- Right stick up / down: previous / next target
- Right stick left: room with all exits
- Right stick right: status
PlayStation names: LB/RB = L1/R1, LT/RT = L2/R2, stick press = L3/R3.


FINDING THE WAY UP OR DOWN
Exits are often at the top (further back) or bottom (front) of a room, or behind doors.
- Entering a room announces the room name and number of exits.
- Walking into a boundary plays noise from that side, then the mod tells you the next exit and its direction.
- Press the left stick (or Ctrl+F8) to be guided to the selected target automatically. Moving the left stick yourself
  takes over again; an enemy closer than 4 meters pauses guiding.
- The mod learns rooms and their exits (RCGAccess\raumkarte.txt). Once it knows the way to the location of the
  active quest, the matching exit is called "toward the quest" and listed first.


TONES
- Beacon: short tone from the target direction. Stereo = side, higher = target further up/back, lower = further down/front,
  faster = closer. Triangle for exits, square for the exit toward the quest, sine for shops and people.
- Enemy radar (echo sounder, since 0.4.0): one tone per beat from the side of the nearest enemy. Low tone = not on
  your line, high tone = on your line (your attacks hit). Slow = far away, faster and faster = closer. Quieter =
  less enemy energy, harder sound = armed. Optionally the low tone is a bit higher or lower for enemies up or down.
- Obstacle (furniture, crate, car): two dull knocks from the side; wall or room boundary: noise.
- Attack warning: two short, bright, hard tones from the side of an attacking enemy nearby.
- Enemy defeated: falling triad. Fight area locked: two low hard tones; cleared: rising two-tone.
- Damage: low squeaky square. Healing: rising tone. Below 30 % health: low double pulse every 2.5 seconds.
- Wall or boundary: noise from the side. Drop nearby: falling tone from the side.
- Interaction possible: short rising tone, then the announcement. Money: two bright tones. Target reached: rising glide.
- Squeaky toy right in front of you (Pattinson's house): bright double beep from the side.
Shift+F6 or the mod menu entry "Listen to all sounds" plays all sounds; in the mod menu every feature also plays its sound.


TROUBLESHOOTING DATA
Run RCGAccess_Daten_sammeln.bat in the game folder. It creates a ZIP with date and time in its name on the desktop.


UNINSTALL
Delete winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt and the folders BepInEx and RCGAccess.


LINKS
- Donation: https://buymeacoffee.com/blindretrogamer/
- Accessibility Mods by Grybyx: https://github.com/grybyx/
