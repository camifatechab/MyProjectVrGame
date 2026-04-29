# Audio Audit Report

Date: 2026-04-25
Project: `MyProjectVrGame`
Scope: static audit of project audio assets, scene serialization, scripts, and project audio settings

## Executive Summary

The project already contains a decent amount of level-specific audio content and several custom gameplay audio systems, but the overall audio architecture is still early-stage.

What is working:
- There are 161 audio files in the repo, with dedicated audio folders for Menu, CampfireHub, Swimming, Rover, Controllers Trial, and Jetpack.
- Several custom scripts already support immersive behavior: 3D ambient emitters, creature audio, positional pickups, underwater transitions, squid proximity tension, and a partially spatial rover radio.
- Swimming and creature-related systems show the strongest immersion intent.

What is missing or weak:
- No project Audio Mixer assets were found.
- No spatializer plugin is configured in `ProjectSettings/AudioManager.asset`.
- No audio reverb zones or audio filter components were found in the project scan.
- Most serialized scene `AudioSource` components in the inspected gameplay scenes are effectively authored as 2D/default-output sources.
- `Assets/Levels/Controllers Trial/Hongdou_de_la_suerte/SceneCopyFinal.unity` contains 16 `AudioListener` components on camera objects, which is a likely runtime risk if more than one listener is enabled at once.

Main conclusion:

The project has enough clips and enough script hooks to become immersive, but it still needs one focused audio polish pass:
- separate audio into mixer buses
- move environmental sounds to true world-space emitters
- keep UI/body/status sounds intentionally near-head or 2D
- add environmental transitions, occlusion, and reverb behavior
- clean up scene listener/source setup

## Method

This report is based on:
- `ProjectSettings/AudioManager.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- scene/prefab YAML inspection
- script inspection of key audio systems
- audio asset inventory under `Assets`

This was not a listening pass and not a live Unity runtime pass, so runtime-only activation states may differ from serialized defaults.

## Project-Level Findings

### 1. Global Audio Settings

From `ProjectSettings/AudioManager.asset`:
- Master volume is `1`
- `VirtualVoiceCount = 512`
- `RealVoiceCount = 64`
- `VirtualizeEffects = 1`
- `SpatializerPlugin` is empty
- `AmbisonicDecoderPlugin` is empty

Impact:
- Unity audio is active and configured normally.
- VR/HRTF-grade positional audio is not enabled at the project level.
- Directionality will be weaker than it could be for a VR title unless a spatializer is added.

### 2. Mixer / Routing

No `.mixer` assets were found in the repo.

In inspected gameplay scenes and prefabs, `OutputAudioMixerGroup` is consistently null/default.

Impact:
- No clean separation for music / dialogue / ambience / UI / vehicle / creature / hazards.
- No easy way to duck dialogue over ambience.
- No bus-level low-pass, snapshots, underwater filtering, or global balancing workflow.

### 3. Reverb / Filtering / Environmental Processing

No uses were found for:
- `AudioReverbZone`
- `AudioReverbFilter`
- `AudioLowPassFilter`
- `AudioHighPassFilter`
- `AudioEchoFilter`
- `AudioDistortionFilter`
- `AudioChorusFilter`

Impact:
- Indoor/outdoor differences are likely visual only.
- Water, caves, enclosed rover spaces, and large structures currently have limited acoustic identity.

## Audio Asset Inventory

Total audio files found under `Assets`: `161`

By extension:
- `.mp3`: 127
- `.wav`: 30
- `.ogg`: 4

Level audio counts under `Assets/Levels`:
- `6 Jetpack`: 42
- `Controllers Trial`: 33
- `5 Rover`: 19
- `3 CampfireHub`: 16
- `1 Menu`: 12
- `4 Swimming`: 10
- `2 CrashToPlanet`: 9

Largest level audio folders:
- `Assets/Levels/6 Jetpack/Assets/Audios/JetpackAudios`
- `Assets/Levels/5 Rover/SFX`
- `Assets/Levels/Controllers Trial/Dialogue/Main Level Dialogue`
- `Assets/Levels/6 Jetpack/Assets/Audios/CreatureAudios`
- `Assets/Levels/1 Menu/Menu Transfer/SFX`
- `Assets/Levels/3 CampfireHub/SFX/Dialogue`

General read:
- The project has more than enough clips to support a richer soundscape.
- A lot of content is dialogue and event SFX.
- Environmental bed design is lighter than it should be relative to the scale of the worlds.

## Scene Audit

### Enabled Build Scenes

From `ProjectSettings/EditorBuildSettings.asset`, the enabled scenes are:
- `Assets/Levels/Controllers Trial/Hongdou_de_la_suerte/Menu.unity`
- `Assets/Levels/Controllers Trial/Hongdou_de_la_suerte/Introduction.unity`
- `Assets/Levels/Controllers Trial/Hongdou_de_la_suerte/SceneCopyFinal.unity`

#### Menu

Observed serialized sources: 4

Observed usage:
- button press sounds
- dialogue trigger
- menu music (`SpaceTravel.mp3`)

Current state:
- all inspected authored sources are effectively 2D
- no environmental world emitters
- no mixer routing

Assessment:
- Functional for UI/menu use
- not intended as an immersive space yet

#### Introduction

Observed serialized sources: 7

Observed usage:
- button press sounds
- dialogue
- warp/computer event audio
- menu music carryover

Current state:
- serialized sources are effectively 2D
- one `AudioListener` on `Camera`
- two sources appear to exist without assigned clips in serialized data

Assessment:
- supports narration and transition beats
- weak environmental depth

#### SceneCopyFinal

Observed serialized sources: 16

Observed usage:
- multiple dialogue lines
- horn/start event sounds
- rover boost panel sound
- `RadioSpeaker` source with no assigned serialized clip

Current state:
- all inspected serialized sources are effectively 2D
- 16 serialized `AudioListener` components on cinematic camera objects
- little evidence of localized environmental audio beds

Assessment:
- dialogue/event coverage exists
- environmental immersion is low
- listener cleanup is important before polish

Risk:
- multiple enabled listeners can create Unity warnings and unstable audio perspective behavior

### Major Level Scenes Outside Current Build

#### CampfireHub

Observed serialized sources in `Assets/Levels/3 CampfireHub/CampfireHub.unity`: 1

Observed usage:
- background music only

Assessment:
- audio identity is currently very thin for a hub scene
- this level would benefit a lot from layered ambience

#### Swimming

Observed serialized sources in `Assets/Levels/4 Swimming/Swimming.unity`: 14

Observed state:
- 10 sources behave like 2D/default-authored sources
- 4 sources are authored with non-zero pan-level behavior
- many sources are `PlayOnAwake`
- many serialized sources do not have clips directly assigned, meaning the real behavior is likely script-driven

Good signs from scripts:
- `Assets/Scripts/AmbientSound.cs` creates true 3D ambient emitters with custom distance settings
- `Assets/Scripts/UnderwaterAudioTransition.cs` handles underwater/surface ambience fading
- `Assets/Levels/4 Swimming/Scripts/UnderwaterAtmosphere.cs` adds breathing, heartbeat, and depth stress
- `Assets/Levels/4 Swimming/Scripts/SpaceshipPiece.cs` creates 3D pickup audio
- `Assets/Scripts/Audio/SquidAudio.cs` creates 3D squid vocalization and 3D proximity heartbeat

Assessment:
- strongest immersive foundation in the project
- still missing acoustic transitions such as reverb/low-pass/occlusion
- could become excellent with one polish pass

#### Rover

Observed serialized sources in `Assets/Levels/5 Rover/Scene/Rover Level.unity`: 17

Observed usage:
- dialogue trigger zones
- repeated geyser/wind sources
- rover engine/brake/offroad clips on `Vehicle Rover`
- boost panel
- rock impact audio
- finish dialogue

Current serialized state:
- all inspected serialized sources are effectively 2D-authored
- default output routing

Important script findings:
- `Assets/Levels/5 Rover/RoverScripts/RoverController.cs` runs continuous engine audio and one-shot vehicle sounds
- `Assets/Levels/5 Rover/RoverScripts/RoverRadioController.cs` uses `spatialBlend = 0.35`, `minDistance = 0.6`, `maxDistance = 5`
- many pickups and hazard sounds use `AudioSource.PlayClipAtPoint`, which is good for world placement
- `PlayerHealth` uses a 2D heartbeat layer, which is appropriate for player-state feedback

Assessment:
- good event coverage
- vehicle scene sounds are present
- environmental audio is not yet convincingly world-spatialized
- geysers/wind especially should feel like space anchors, not general flat playback

#### Jetpack

Observed serialized sources in `Assets/Levels/6 Jetpack/Jetpack.unity`: 1

Meaning:
- the scene itself is not heavily authored with static `AudioSource` components
- the real jetpack audio design is mostly runtime-script driven

Important script findings:
- `Assets/Levels/6 Jetpack/Scripts/JetpackAudioManager.cs` creates ignition/thrust/boost/wind/warning/landing sources as 2D sounds that follow the player
- `Assets/Levels/6 Jetpack/Scripts/FootstepAudioManager.cs` also uses 2D footsteps
- `Assets/Levels/6 Jetpack/Scripts/CrystalAudioManager.cs` uses mostly 2D hum/pulse/combo/victory layers
- `Assets/Scripts/Jetpack/CreatureAudioManager.cs` is a strong full-3D creature audio system with distance settings and loop layers

Assessment:
- player-centric feedback is implemented
- world ambience is under-authored in the scene
- creature audio is good
- crystal and jetpack layers are readable, but many are head-locked rather than world-rooted

## Script-Level Audio Design Review

### Strong Existing Systems

#### `Assets/Scripts/AmbientSound.cs`

Strength:
- explicitly configures full 3D ambient playback
- custom min/max distance
- good base utility for waterfalls, wind, hums, wildlife, vents

Value:
- this should become the main environmental emitter pattern for the project

#### `Assets/Scripts/Jetpack/CreatureAudioManager.cs`

Strength:
- one of the best systems in the repo
- multiple dedicated sources
- full 3D placement on creature
- supports loops, calls, flaps, breathing, landing, fades

Value:
- this is already close to shippable immersion logic

#### `Assets/Scripts/UnderwaterAudioTransition.cs` and `Assets/Levels/4 Swimming/Scripts/UnderwaterAtmosphere.cs`

Strength:
- good layered mood design
- underwater/surface crossfade
- breathing + heartbeat tension
- supports depth-based stress

Value:
- strong foundation for a memorable underwater soundscape

### Systems That Are Functional But Not Yet Immersive Enough

#### `Assets/Levels/6 Jetpack/Scripts/JetpackAudioManager.cs`

Current behavior:
- ignition/thrust/wind/warning/landing are authored as 2D

Assessment:
- good for readability and player feedback
- less good for VR embodiment if there is no matching world-space exhaust/external propulsion presence

#### `Assets/Levels/6 Jetpack/Scripts/CrystalAudioManager.cs`

Current behavior:
- collection, hum, pulse, combo, victory are mostly 2D

Assessment:
- readable, but crystals would feel more magical if at least the local hum/pulse came from crystal positions rather than from the player rig

#### `Assets/Levels/5 Rover/RoverScripts/RoverController.cs`

Current behavior:
- engine loop plays continuously
- functional but not obviously separated into interior/body/terrain layers

Assessment:
- rover movement is covered
- embodiment could be stronger with tire grit, suspension rattle, chassis creak, boost exhaust, and better outside-world contrast

## Keep 2D vs Convert to 3D

Not everything should become 3D in VR.

### Keep 2D or Near-Head

Recommended to remain 2D or only lightly spatialized:
- UI clicks
- subtitle/dialogue system control cues
- heartbeat / low-health / oxygen warning layers
- very close body sounds like breathing
- some jetpack thrust bed if it represents internal player feedback

### Convert or Expand as 3D World Audio

Recommended to be world-spatialized:
- geysers / vents / steam eruptions
- waterfalls / surface water / underwater bubble plumes
- fire / campfire crackle
- station machinery hum
- creature calls and movement
- crystal hums
- environmental wind points
- rockfalls / hazards / mechanical failures
- radio speakers if they are physical objects in the world

## Add / Remove Recommendations

### Highest Priority Additions

1. Add an Audio Mixer structure
- Suggested buses: `Master`, `Music`, `Dialogue`, `UI`, `Player`, `Vehicle`, `Creatures`, `Environment`, `Hazards`
- Use it for ducking dialogue over ambience and for underwater snapshots

2. Enable a VR spatializer plugin
- This is the biggest single improvement for directional presence
- Especially important for creatures, geysers, bubbles, machinery, and off-screen threats

3. Build an environmental emitter pass per level
- Place persistent 3D emitters for wind, water, fire, machinery, fauna, and hazard zones
- Reuse `AmbientSound.cs` as the standard component

4. Add environmental transitions
- underwater low-pass / muffling
- cave or interior reverb
- surface-to-underwater tonal shift
- vehicle-near vs vehicle-far perspective change

5. Clean up listener and source setup in cinematic scenes
- especially `SceneCopyFinal.unity`

### Best Adds Per Level

#### Menu / Introduction
- low sci-fi room tone
- subtle ship electronics
- UI hover/select variations
- transition swells when scene changes

#### Controllers Trial / SceneCopyFinal
- localized machinery hum
- environmental wind in exterior shots
- camera-transition whooshes
- distance-based world beds under dialogue, kept low

#### CampfireHub
- campfire crackle
- distant insects or night creatures
- wind pass-throughs
- station electrical hum
- metal groans / antenna resonance if the scene is industrial

#### Swimming
- distant creature calls
- cave rumbles
- current/pressure beds
- stronger localized bubble emitters
- occluded underwater filtering when entering structures or caves

#### Rover
- layered wheel/terrain loops by surface type
- suspension clunks
- distant volcano/vent beds
- better rover exterior exhaust/boost presence
- stronger 3D placement for geysers and hazard rocks

#### Jetpack
- platform beacon hums in world space
- crystal-local pulse/hum emitters
- altitude gust zones
- landing pad resonance
- exterior propulsion burst for jumps/boosts

### Likely Removal / Cleanup Candidates

1. Repeated flat geyser/wind sources in Rover
- several `Wind` objects reuse the same clip
- keep some, but vary pitch, distance, placement, and clip selection

2. Empty or clip-less serialized sources
- seen in Introduction, Swimming, Jetpack, and `RadioSpeaker`
- if they are placeholders, document them
- if obsolete, remove them

3. Duplicate cinematic listeners
- keep one active listener strategy per shot system

4. Any persistent 2D environmental loops that should be world-rooted
- especially if the source is physically visible in the level

## Practical Next-Step Plan

Recommended order:

1. Create mixer buses and reroute all important sources
2. Decide which sounds are intentionally player-head sounds versus world sounds
3. Convert environmental emitters level by level, starting with Rover and CampfireHub
4. Add underwater/interior/exterior filtering and transitions
5. Clean `SceneCopyFinal` camera/listener setup
6. Run an in-headset listening pass and rebalance distances, priority, and loudness

## Bottom Line

The project is not missing audio content. It is missing audio structure.

The strongest immersive systems already exist in code, especially for:
- underwater mood
- creature audio
- ambient emitter tooling

The weakest areas are:
- global routing
- environmental spatialization consistency
- acoustic transitions
- cinematic listener cleanup

If you want the game to feel much more immersive, the best return will come from:
- mixer + spatializer first
- then a dedicated environmental 3D emitter pass
- then transition/reverb/occlusion polish
