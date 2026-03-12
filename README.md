# Minimal ECS + NetCode Demo

This repository includes a minimal Unity Entities/NetCode sample under `Assets/NetCodeSample`.

## What's included

- A simple level definition authored via `LevelAuthoring` (player spawn + static platform colliders).
- A player ghost prefab flow (`PlayerGhostAuthoring` + `PlayerGhostPrefabAuthoring`).
- Client input capture (`PlayerInputCommand`) for horizontal movement and jump.
- Predicted movement/jump in `PlayerPredictedMovementSystem`.
- Server-side replicated position/velocity (`PlayerReplicatedStateSystem`) and client reconciliation for non-owned ghosts.
- Bootstrap (`NetCodeSampleBootstrap`) that automatically creates client+server worlds and auto-connects on port `7979`.

## Unity scene setup (one-time)

1. Create a scene and add an empty GameObject named `NetCodeConfig`.
2. Add these components to `NetCodeConfig`:
   - `LevelAuthoring` (edit platform list and spawn point as needed),
   - `PlayerGhostPrefabAuthoring`.
3. Create a `Player` prefab and add:
   - `Ghost Authoring Component` (set to **Predicted**),
   - `PlayerGhostAuthoring`,
   - physics authoring components (`Physics Shape` + dynamic body),
   - a visible mesh (cube/capsule).
4. Assign the `Player` prefab to `PlayerGhostPrefabAuthoring.Player Prefab`.
5. Enter Play Mode.

## Run as dedicated server

- Build/run with Unity NetCode server launch arguments (headless player) and the same scene.
- The bootstrap binds auto-connect port `7979`; server and clients should use this port.

## Run one or more clients

- Start one editor/client instance in Play Mode.
- Start additional standalone clients (or ParrelSync clones) pointing to the same server.
- Each connected client receives a predicted player ghost.

## Expected behavior

- Local player responds immediately to `A/D` or arrow keys and `Space` for jump (predicted movement).
- Static level platforms are present in each world from `LevelAuthoring` data.
- Server remains authoritative for physics state.
- Other clients observe synced movement/jump from replicated position and velocity.
