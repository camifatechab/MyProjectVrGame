# Rover Kit - Milestone 1 Setup

This kit is being built as a reusable rover UI package, not a one-off scene setup.

## Current Scope

Milestone 1 covers:

- seat beacon logic
- mount prompt logic
- rover UI binder
- shared theme asset type

## Scripts

- `RoverTheme.cs`
- `RoverUIBinder.cs`
- `RoverSeatBeaconController.cs`
- `RoverMountPromptController.cs`
- `RoverUIState.cs`

## Rover Anchor Contract

Create these empty child transforms on the rover prefab:

- `UI_SeatAnchor`
- `UI_PromptAnchor`
- `UI_DashboardAnchor`
- `UI_WarningAnchor`
- `UI_LeftHintAnchor`
- `UI_RightHintAnchor`

Only `UI_SeatAnchor` and `UI_PromptAnchor` are needed for Milestone 1.

## First Editor Setup

1. Add `RoverUIBinder` to the rover root.
2. Assign `RoverPhysicsController` or keep the legacy rover driver if using the older rover.
3. Create a visual object near the seat and add `RoverSeatBeaconController` to it.
4. Create a world-space prompt object near the entry side and add `RoverMountPromptController` to it.
5. Drag both module references into `RoverUIBinder`.
6. Assign the seat and prompt anchors.
7. Create a `RoverTheme` asset and assign it to the binder.

## Visual Direction Recommendation

For the first pass, avoid the old floor ring.

Use:

- a seat-area emissive glow
- a soft accent light
- very light floating particles
- a small holographic mount prompt

Avoid:

- large opaque panels
- floor rings
- bright white UI blocks
- aggressive flashing

## First Acceptance Check

The first milestone is correct when:

- the rover feels inviting from medium distance
- the prompt only appears when the player is close enough to mount
- all invitation visuals disappear when mounted
- the setup can be reused by reassigning anchors on another rover
