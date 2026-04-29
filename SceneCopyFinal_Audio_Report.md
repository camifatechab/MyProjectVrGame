# SceneCopyFinal Audio Report

Date: 2026-04-25
Scene: `Assets/Levels/Controllers Trial/Hongdou_de_la_suerte/SceneCopyFinal.unity`
Scope: static audit of audio sources, audio listeners, referenced clips, and audio-capable scripts in this scene.

## Summary

`SceneCopyFinal` has functional audio for dialogue, rover radio, boost feedback, laser/target combat hooks, jetpack/crystal systems, waterfall ambience, and flying creature audio. The strongest immersive pieces are script-driven, especially `AmbientSound.cs`, `CreatureAudioManager.cs`, `RoverRadioController.cs`, and `LaserTarget.cs`.

The scene is not yet consistently immersive. Most scene-authored `AudioSource` components are flat/default-output sources, there is no mixer routing, the rover radio is configured as fully 2D in this scene, and several major systems have missing clip assignments.

## Scene Audio Sources

The scene contains 16 serialized `AudioSource` components.

All 16:
- route to default output, not an Audio Mixer group
- have `Spatialize = 0`
- have `PanLevel = 0`
- use default distance settings, `MinDistance = 1`, `MaxDistance = 500`

### Serialized Sources

| GameObject | Clip | Play On Awake | Notes |
|---|---:|---:|---|
| `Dialogue` | `Dialogue 1.mp3` | No | Trigger dialogue |
| `Dialogue 2` | `Dialogue 2.mp3` | No | Trigger dialogue |
| `Dialogue 3` | `Dialogue 3.mp3` | No | Trigger dialogue |
| `Dialogue 4` | `Dialogue 4.mp3` | No | Trigger dialogue |
| `Dialogue 5` | `Dialogue 5.mp3` | No | Trigger dialogue |
| `Dialogue 6` | `Dialogue 6.mp3` | No | Trigger dialogue |
| `Dialogue 7` | `Dialogue 7.mp3` | No | Trigger dialogue |
| `Dialogue 8` | `Dialogue 8.mp3` | No | Trigger dialogue |
| `Dialogue 9` | `Dialogue 9.mp3` | No | Trigger dialogue |
| `Dialogue 10` | `Dialogue 10.mp3` | No | Trigger dialogue |
| `Dialogue 11` | `Dialogue 11.mp3` | No | Trigger dialogue |
| `Start` | `Start.mp3` | Yes | Plays at scene start |
| `Horn` | `Horn.mp3` | No | Event cue |
| `High Pitch Horn` | `Horn.mp3` | No | Duplicate horn cue |
| `Boost Panel Trigger` | `Boost Panel.mp3` | No | Rover boost panel |
| `RadioSpeaker` | none assigned directly | Yes | Controlled by rover radio script |

### Direct Concerns

`RadioSpeaker` has `PlayOnAwake = 1` but no assigned clip in the serialized `AudioSource`. This is probably harmless because `RoverRadioController` assigns tracks at runtime, but it is untidy and should be set to `PlayOnAwake = false`.

Most dialogue sources use priority `0`, which is the highest Unity source priority. That helps prevent dialogue cutoff, but using it everywhere leaves less room to prioritize critical player-state audio deliberately.

`Start.mp3` plays on awake. Confirm that this is intended and does not overlap the first trigger or cinematic line.

## Audio Listeners

The scene contains 16 enabled `AudioListener` components on camera objects.

Context from the project owner:
- only the main gameplay camera is intended to work during gameplay
- the other cameras were used for recording shots

At scene load, only one of those camera GameObjects appears active:
- active: `Intro to the World Camera`
- inactive: the other 15 listener cameras

Listener-bearing cameras:
- `Volcano Bird's Eye View Camera`
- `Last View Camera`
- `Jetpack Camera`
- `TrailerVolcano01`
- `Rover Show 2 Camera`
- `Panorama Floating Islands Camera`
- `Rover Show 1 Camera`
- `Teleporter Camera`
- `Sunset Camera`
- `Dive Down Camera`
- `Crystal to Outside of Volcano Camera`
- `Swamp Bird's Eye Camera`
- `Dragon Camera`
- `Swamp Panorama Run Camera`
- `Intro to the World Camera`
- `Road to Volcano Camera`

Assessment:

This is no longer a high-severity gameplay audio issue if the recording cameras stay inactive. Treat it as cleanup and documentation risk. The scene should make the intended camera ownership obvious so a future edit does not accidentally activate a recording camera with its own listener.

Recommendation:
- Keep the gameplay listener on the main/XR camera.
- Move recording cameras under a `Recording Cameras` parent if they are kept in the scene.
- Disable or remove `AudioListener` components from recording-only cameras unless they are needed for a specific capture workflow.

## Runtime Audio Systems In This Scene

### Dialogue

Script:
- `Assets/Levels/Controllers Trial/Scripts/DialogueTriggerZone.cs`

Behavior:
- plays an array of clips with `audioSource.PlayOneShot(clip)`
- waits for each clip length plus a delay
- triggers only from objects tagged `Player`
- supports `playOnlyOnce`

Current use:
- dialogue objects from `Dialogue 1` through `Dialogue 11`
- `Start`
- `Horn` / `High Pitch Horn`

Assessment:
- functional and simple
- currently uses scene-authored 2D/default audio sources
- good candidate for a dedicated `Dialogue` mixer group

Recommendation:
- Keep dialogue mostly non-spatial if it represents narration or radio comms.
- Use subtle spatialization only when the voice is meant to come from a physical speaker.

### Waterfall Ambience

Script:
- `Assets/Scripts/AmbientSound.cs`

Scene references:
- 4 instances reference `Assets/Levels/6 Jetpack/Assets/WaterfallSound.wav`
- volume `1`
- min distance `5`
- max distance `30`
- loop enabled
- play on awake enabled

Assessment:
- this is the best environmental audio pattern in the scene
- script creates true 3D ambience at runtime
- object names are from prefab/stripped references, so they are harder to audit by name from YAML alone

Recommendation:
- Keep this approach.
- Add more world emitters using the same component for wind, volcano rumble, machinery hum, crystal resonance, and swamp ambience.
- Rename instantiated ambience objects clearly in the scene if possible.

### Rover Radio

Script:
- `Assets/Levels/5 Rover/RoverScripts/RoverRadioController.cs`

Scene object:
- `Rover_PhysicsTest`

Tracks:
- `Track 01` through `Track 05`
- all from `Assets/Levels/Controllers Trial/MusicForRover`

Scene settings:
- `radioVolume = 0.2`
- `playOnMount = 1`
- `spatialBlend = 0`
- `minDistance = 0.6`
- `maxDistance = 5`

Assessment:
- good feature
- currently fully 2D in this scene, so the min/max distance values do not matter
- less immersive than the script default, which supports partial spatialization

Recommendation:
- Change `spatialBlend` to around `0.3` to `0.5` so the radio feels attached to the rover while still readable.
- Route to a `Music` or `VehicleRadio` mixer group.
- Consider ducking the radio when dialogue plays.

### Flying Creature Audio

Script:
- `Assets/Scripts/Jetpack/CreatureAudioManager.cs`

Scene settings:
- `spatialBlend = 1`
- `minDistance = 1`
- `maxDistance = 50`
- flight ambient, breathing, wing flap, mount, dismount, landing, and call clips assigned
- `creatureCallExcited` is not assigned

Assessment:
- strong immersive setup
- correct use of 3D audio for a moving world object

Recommendation:
- Keep as a model for future creature and moving-object audio.
- Assign an excited call if the gameplay has an intense flight/combat moment.
- Route creature sounds to a `Creatures` mixer bus.

### Jetpack Audio

Script:
- `Assets/Levels/6 Jetpack/Scripts/JetpackAudioManager.cs`

Scene issue:
- all major clip fields are unassigned in this scene:
  - ignition
  - thrust loop
  - boost
  - low fuel warning
  - landing soft/hard
  - platform touchdown
  - altitude wind
  - speed wind

Related script:
- `AutoJetpackController` references `Assets/Levels/6 Jetpack/Assets/Jetpacksound.mp3`

Assessment:
- the scene has the manager but not the full audio content connected
- jetpack flight may sound much thinner than intended

Recommendation:
- Assign the manager clips or remove the manager if a different jetpack audio path is authoritative.
- Keep warning and body feedback near-head.
- Add a separate 3D exhaust or burst source if other players/world objects should hear propulsion.

### Crystal Audio

Script:
- `Assets/Levels/6 Jetpack/Scripts/CrystalAudioManager.cs`

Assigned clips:
- crystal collect
- humming crystal
- crystal spawn
- crystal pulse
- crystal combo
- final collection sound

Current behavior from script:
- collect, hum, pulse, combo, victory sources are mostly 2D
- spawn can use `PlayClipAtPoint`

Assessment:
- readable gameplay feedback
- less environmental than it could be

Recommendation:
- Keep collection and victory readable.
- Move local crystal hum/pulse to each crystal position or add small 3D emitter children to crystals.
- Route to `Interactables` or `Environment` depending on whether the sound is feedback or world ambience.

### Combat / Laser Audio

Scripts:
- `Assets/Levels/Controllers Trial/Scripts/LaserShooter.cs`
- `Assets/Levels/Controllers Trial/Scripts/LaserTarget.cs`

Laser target:
- several target instances reference `Dragon death killing shot.mp3`
- `LaserTarget` creates/uses a 3D audio source for death sound

Laser shooter:
- creates a 2D `AudioSource`
- fire, hit, empty click, and travel whoosh clip fields appear unassigned in this scene
- `requireFlying = 0`

Assessment:
- target death audio is correctly spatialized
- shooter audio may be silent if the fields remain unassigned

Recommendation:
- Assign shooter clips if laser combat is active in this scene.
- Keep controller fire/empty click mostly 2D or near-hand.
- Keep impact/death sounds 3D at the hit object.

### Slow Terrain Audio

Script:
- `Assets/Levels/5 Rover/RoverScripts/SlowTerrain.cs`

Scene references:
- many instances reference `Assets/Levels/5 Rover/SFX/Offroad.mp3`

Assessment:
- useful terrain feedback, but repeated identical one-shots can become fatiguing

Recommendation:
- Use a small variation set for mud/offroad entry.
- Add pitch and volume randomization if not already present.
- Route to `Vehicle` or `Environment`.

## Immersion Gaps Specific To SceneCopyFinal

1. Too many important sounds are authored as flat/default scene sources.
2. The scene has no Audio Mixer routing.
3. Recording-only cameras still carry listeners, which is cleanup/documentation risk.
4. The rover radio is fully 2D despite having distance fields.
5. Jetpack manager clips are unassigned.
6. Laser shooter clips are unassigned.
7. Environmental ambience exists, but mostly as waterfall instances only.
8. Horn and high-pitch horn reuse the same `Horn.mp3` clip.

## Add / Remove Plan For This Scene

### Add

- Add an `AudioMixer` and route this scene through groups: `Dialogue`, `UI`, `Vehicle`, `Radio`, `Environment`, `Creatures`, `Combat`.
- Add 3D ambient emitters for volcano rumble, wind gusts, machinery hum, floating-island air movement, and distant environmental beds.
- Add partial spatialization to rover radio, around `0.3` to `0.5`.
- Add assigned clips to `JetpackAudioManager` or remove/disable it if unused.
- Add assigned clips to `LaserShooter` if laser combat remains active.
- Add audio ducking so dialogue reduces radio/music/environment levels temporarily.
- Document the main gameplay camera/listener and group recording-only cameras clearly.

### Remove Or Clean

- Set `RadioSpeaker` `PlayOnAwake` to false.
- Remove unused or silent managers only if gameplay confirms they are not needed.
- Remove or disable listeners from recording-only cameras when they are no longer needed.
- Avoid using the same `Horn.mp3` for both horn roles unless one is pitch-shifted or processed.
- Rename stripped/prefab ambience instances so the hierarchy communicates what each sound represents.

## Priority Fixes

1. Create mixer groups and route `SceneCopyFinal` audio.
2. Clean up or document recording-only camera listeners.
3. Set rover radio to partial 3D and route it.
4. Assign or remove unassigned jetpack and laser audio fields.
5. Add environmental 3D ambience beyond waterfalls.
6. Add dialogue ducking and verify the `Start.mp3` timing.

## Practical Sound Design Direction

The scene appears to cover a cinematic/world-tour style experience with rover, jetpack, dragon/combat, floating islands, waterfalls, and staged dialogue. The audio should support that with clear layers:

- Dialogue: clean, centered, ducking other layers
- Rover: radio, engine/body movement, terrain contact
- Flight: wind altitude, wing motion, creature breathing/calls
- World: waterfalls, volcano, floating-island wind, distant impacts
- Combat: hand/controller feedback in 2D, target impacts/deaths in 3D

The scene already has the hooks for this. The next improvement should be wiring and balancing, not searching for more random clips.

## Immersion Upgrade Plan

This scene should sound like one continuous alien landscape rather than separate gameplay systems. The best upgrade is to build layers around locations and player state.

### Foundation First

1. Add an Audio Mixer.
- Suggested groups: `Dialogue`, `Radio`, `Vehicle`, `Player`, `Creatures`, `Environment`, `Hazards`, `UI`.
- Add a `DialogueFocus` snapshot that lowers `Radio`, `Vehicle`, and `Environment` by roughly 6-10 dB while voice lines play.
- Add a `Danger` snapshot that raises `Hazards`, `Creatures`, and low-frequency rumble when near lava, dragon/combat, or critical moments.

2. Use clear 3D emitter prefabs.
- Make reusable prefabs such as `AudioEmitter_Wind`, `AudioEmitter_Lava`, `AudioEmitter_Swamp`, `AudioEmitter_Waterfall`, `AudioEmitter_Machinery`.
- Base them on `AmbientSound.cs` or a slightly upgraded version that supports mixer group, random start time, random pitch, and optional fade in/out.
- Name every placed sound object by purpose, for example `SFX_Lava_Rumble_NearBridge` or `SFX_Swamp_Insects_WestPool`.

3. Keep player-state audio separate from world audio.
- Player-state sounds can remain 2D: oxygen, warnings, heartbeat, UI, subtitles, some jetpack body feedback.
- Physical objects should be 3D: lava, swamp pools, waterfalls, radio speakers, vents, crystals, creatures, impacts.

### Rover Area

Goal: make the rover feel heavy, mechanical, and connected to terrain.

Add these layers:
- `Rover_Engine_Loop`: tied to speed and acceleration, near the rover body.
- `Rover_Tire_Gravel`: fades in on normal ground movement.
- `Rover_Tire_Mud`: fades in on swamp/slow terrain.
- `Rover_Suspension_Creak`: random one-shots on bumps and hard turns.
- `Rover_Boost_Exterior`: 3D burst at rear/exhaust position.
- `Rover_Radio`: partial 3D, `spatialBlend` around `0.35`, short max distance around `5`.

Recommended settings:
- Engine/body: `spatialBlend 0.2-0.4` if player rides inside/near it.
- External boost and impacts: `spatialBlend 1.0`.
- Tire loops: `spatialBlend 0.5-0.8`, low volume, tied to terrain state.

What to remove or reduce:
- Avoid repeating the same `Offroad.mp3` every time the rover enters slow terrain.
- Replace repeated one-shots with loop layers plus occasional randomized grit/clunk one-shots.

### Swamp Area

Goal: create a damp, close, living sound field.

Add these 3D emitters:
- `Swamp_Insects_Bed`: wide looping bed, several low-volume emitters around the area.
- `Swamp_Bubbles`: short randomized bubbling one-shots from water/mud pools.
- `Swamp_Mud_Gloop`: triggered by rover or player movement through slow terrain.
- `Swamp_Distant_Call`: rare distant creature call, long cooldown.
- `Swamp_Reeds_Rustle`: wind/rustle near vegetation clusters.

Recommended settings:
- Insects: multiple emitters, low volume, `minDistance 5`, `maxDistance 30-45`.
- Bubbles/gloop: `minDistance 1`, `maxDistance 10-18`, pitch randomization.
- Distant calls: `minDistance 20`, `maxDistance 80`, low priority, rare.

Important:
- Do not make the swamp one loud loop. Use several quieter emitters so head movement reveals the space.

### Lava / Volcano Area

Goal: make lava feel dangerous before the player sees it.

Add these layers:
- `Lava_Low_Rumble`: low-frequency loop, 3D, placed at lava/volcano body.
- `Lava_Crackles`: small random one-shots around lava edges.
- `Lava_GasVent`: hiss loops or bursts from vents.
- `Lava_BubblePop`: random pops from lava pools.
- `Volcano_Distant_Roar`: rare large rumble, wide range.
- `Heat_Danger_Tone`: subtle near-head tension layer only near danger zones.

Recommended settings:
- Rumble: `spatialBlend 0.7-1.0`, `minDistance 8-15`, `maxDistance 80-120`.
- Crackles/pops: `spatialBlend 1.0`, `minDistance 1-3`, `maxDistance 15-25`.
- Gas vents: directional placement near visible steam/particles.

Implementation detail:
- Add trigger zones near lava that fade in the `Danger` mixer snapshot and heat tension layer.
- If the player moves behind rock or terrain, lower high frequencies with a low-pass filter or swap to a muffled variant.

### Waterfalls / Floating Islands

Goal: make vertical space readable through sound.

Keep:
- current `AmbientSound.cs` waterfall setup.

Improve:
- Add separate `Waterfall_Distant_Roar`, `Waterfall_NearSpray`, and `Waterfall_Mist` layers.
- Use larger max distance for the distant roar and shorter range for spray.
- Randomize start offset so four waterfall emitters do not phase together.

Recommended settings:
- Distant roar: `minDistance 10`, `maxDistance 80`, low-pass slightly.
- Near spray: `minDistance 2`, `maxDistance 20`, brighter.
- Mist/drips: short randomized one-shots near cliffs or wet surfaces.

### Flight / Creature / Jetpack

Goal: sell height, speed, and companion presence.

Creature:
- Keep `CreatureAudioManager` as the model.
- Assign `creatureCallExcited` for high-speed, danger, or final moments.
- Add stereo-safe wing flap variations if current flap repeats too obviously.

Jetpack:
- Assign clips to `JetpackAudioManager` if this manager is active in the scene.
- Use 2D/near-head for player warning and internal thrust feedback.
- Use 3D emitters for external boost bursts, landing impacts, and platform touchdowns.

Wind:
- Add altitude wind and speed wind layers.
- Fade altitude wind by player height.
- Fade speed wind by velocity.
- Keep these mostly near-head because they represent what the player experiences while flying.

### Dialogue And Story Beats

Goal: keep story clear without flattening the world.

Recommended:
- Route all dialogue to `Dialogue`.
- During dialogue, duck `Radio`, `Vehicle`, `Environment`, and `Creatures`.
- Keep narration/radio comms centered or 2D.
- If a line comes from a visible object, such as a speaker or horn, use partial 3D.

Specific cleanup:
- Verify `Start.mp3` play-on-awake timing.
- Decide whether `Horn` and `High Pitch Horn` should be different sounds.
- If they use the same clip, pitch-shift or EQ one so the player reads them as different cues.

### Best First Implementation Pass

Do this in order:

1. Fix simple scene settings.
- `RadioSpeaker.playOnAwake = false`
- `RoverRadioController.spatialBlend = 0.35`
- document or disable recording camera listeners

2. Add environment emitters.
- lava/volcano rumble
- swamp bed and bubbles
- floating island wind
- waterfall distant/near layers

3. Wire missing active gameplay audio.
- assign `JetpackAudioManager` clips if used
- assign `LaserShooter` clips if combat is used
- assign `creatureCallExcited` if there is a major flight moment

4. Add mixer routing and ducking.
- this is what will keep the scene from becoming loud and messy

5. Do a headset listening pass.
- stand still and rotate head near each biome
- drive the rover through normal and slow terrain
- fly high and low
- trigger dialogue while radio and ambience are active

## 3D Audio Asset / Prefab Decisions

Do not put audio on every prop. Put 3D audio on anchor objects that the player can approach, circle, pass, or identify visually. Small repeated props should usually stay silent unless they are part of a pooled/randomized ambience system.

### High Priority 3D Audio Targets

| Scene object or prefab | Add 3D audio? | Recommended sound role | Suggested range |
|---|---:|---|---:|
| `Rover_PhysicsTest` / `RoverBody` | Yes | engine body, chassis, radio, boost/exhaust | `2-18m` |
| `RadioSpeaker` | Yes, partial | rover radio as a physical source | `0.6-5m` |
| `Boost Panel Trigger` | Yes | boost pad activation / electric hum | `1-12m` |
| `Swamp`, `Swamp (1)`, `Swamp (2)`, `Swamp (3)` | Yes | insects, wet mud bed, distant wet ambience | `8-45m` |
| `Water` objects | Yes | water lap, bubbles, wet edge ambience | `3-30m` |
| `WaterVolume-74976` | Yes | underwater/near-water transition zone | trigger based |
| `Lava_planet` | Yes | large lava bed, deep heat rumble | `15-120m` |
| `Lava_machine` / `Lava_machine.prefab` | Yes | mechanical heat hum, pressure pulses | `3-35m` |
| `Volcano Environment` | Yes | distant volcano roar / ground rumble | `20-140m` |
| `Volcano (Missing Prefab...)` | Yes, after fixing prefab | volcano focal source if restored | `20-140m` |
| `FirePoint` objects | Yes | flame crackle, gas hiss, ignition burst | `2-18m` |
| `smoke (1)` / smoke prefabs | Yes | vent hiss, ash plume, low whoosh | `3-30m` |
| Existing waterfall/effects prefab instances | Yes | waterfall roar, spray, mist | `5-80m` |
| `FloatingIsland` | Yes | high wind, resonance, distant air movement | `10-80m` |
| `Start Rock Isle` | Yes | subtle wind/edge ambience | `8-45m` |
| `Dragon` / flying creature prefab | Yes | wing flap, breathing, calls, movement | `1-50m` |
| `Rock Spawner` / `Rock Spawn Location` objects | Yes | rockfall warning, impact, rolling debris | `5-60m` |
| Crystal collectibles / crystal manager targets | Yes | local hum/pulse, spawn, collect | `1-18m` |
| `Teleporter Camera` area / teleporter object if present | Yes | sci-fi hum, charge, warp burst | `3-30m` |

### Medium Priority 3D Audio Targets

| Scene object or prefab | Add 3D audio? | Recommended sound role | Suggested range |
|---|---:|---|---:|
| `RoverRoad` / `RoverRoad_Base` | Maybe | terrain bed only if road has special material | `5-25m` |
| `TerrainRoverCourse` | Maybe | broad terrain layer via trigger zones, not direct source | trigger based |
| `Plants` groups | Maybe | localized rustle only near dense vegetation | `2-15m` |
| `Hive_plants_destoryable` | Yes if interactive | organic hum / damage reaction / break sound | `2-18m` |
| `Rocks` / `Small_rocks` groups | Usually no | only add falling/impact audio if interactive | event based |
| `Lights Volcano`, `Lights Swamp`, `Lights Floating Islands` | Maybe | electrical hum only if visually mechanical/magical | `2-15m` |
| `Horn` / `High Pitch Horn` | Yes if physical | horn source from object location | `5-50m` |

### Keep 2D Or Near-Head

These should not become full world audio:
- narration dialogue
- subtitles/typewriter UI
- low-health/heartbeat player state
- oxygen or warning UI
- laser trigger click if it represents the controller
- jetpack warning and internal thrust feedback

These can be partially spatialized only if there is a visible source:
- radio voice
- computer voice
- horn
- teleporter announcements

### Specific Prefab Candidates

Use these existing prefab families as audio anchors:

- `Assets/Alien_planets_Vol2/Prefabs/Effects/Waterfall.prefab`
- `Assets/Alien_planets_Vol2/Prefabs/Effects/Lava_pull.prefab`
- `Assets/Alien_planets_Vol2/Prefabs/Effects/smoke.prefab`
- `Assets/Alien_planets_Vol2/Prefabs/Effects/big_smoke.prefab`
- `Assets/Alien_planets_Vol2/Prefabs/Structures/Lava_machine.prefab`
- `Assets/Alien_planets_Vol2/Prefabs/Plants/Lava/*`
- `Assets/Alien_planets_Vol2/Prefabs/rocks/Lava/*`
- `Assets/Alien_planets_Vol2/Prefabs/Plants/Hive/*`
- `Assets/Alien_planets_Vol2/Prefabs/rocks/Hive/*`
- `Assets/Alien_planets_Vol2/Prefabs/Plants/Sky/*`
- `Assets/Alien_planets_Vol2/Prefabs/rocks/Sky/*`

Audio should usually be added through child objects, not by modifying every visual mesh prefab directly. Example:

- `Lava_machine`
- child: `SFX_LavaMachine_Hum`
- child: `SFX_LavaMachine_PressurePulse`

This keeps visual prefabs reusable while making sound placement explicit in the scene.

### Recommended Emitter Presets

| Preset | Use for | Spatial blend | Min distance | Max distance | Loop |
|---|---|---:|---:|---:|---:|
| `SFX_Emitter_Small_OneShot` | bubbles, crackles, small impacts | `1.0` | `1` | `10-18` | No |
| `SFX_Emitter_Medium_Loop` | machinery, swamp bed, fire, water edge | `1.0` | `3-6` | `25-45` | Yes |
| `SFX_Emitter_Large_Loop` | volcano, waterfall, floating island wind | `0.8-1.0` | `10-20` | `80-140` | Yes |
| `SFX_Player_NearHead` | warning, heartbeat, internal jetpack | `0-0.2` | n/a | n/a | Depends |
| `SFX_Physical_Radio` | rover speaker / computer speaker | `0.3-0.5` | `0.6` | `5-12` | Depends |

### First Placement Batch

Start with these exact targets:

1. `RadioSpeaker`
- Set partial 3D.
- Keep short range so it feels mounted in the rover.

2. `Lava_planet`, `Lava_machine`, `FirePoint`, `smoke (1)`
- Add lava rumble, machine hum, crackles, and hiss.

3. `Swamp`, `Swamp (1)`, `Swamp (2)`, `Swamp (3)`, `Water`
- Add swamp bed, bubbles, wet mud/gloop, and water edge loops.

4. Existing waterfall/effects instances
- Split into distant roar and near spray if the same waterfall is visually important.

5. `Rock Spawner` and `Rock Spawn Location` objects
- Add warning rumble and 3D impacts when rocks spawn or land.

6. `Rover_PhysicsTest`
- Add tire/terrain layers and external boost burst.

7. `FloatingIsland` / `Start Rock Isle`
- Add high-altitude wind and subtle island resonance.

### Implementation Rule

Every 3D audio object should answer one question:

What should the player learn by hearing this from a direction?

If the answer is unclear, keep it as a low-volume global bed or do not add it.
