# CarSetup Guide (Beginner Friendly)

This guide shows how to build reusable traffic car prefabs that work with waypoint movement.

## 1) Create Vehicle Configuration Assets (Multiple Car Types)

1. In Unity Project window, right click:
   `Create -> Traffic System -> Vehicles -> Vehicle Configuration`
2. Create one asset per type, for example:
   - `VehicleConfig_Sedan`
   - `VehicleConfig_Truck`
   - `VehicleConfig_Bus`
3. Tune each type:
   - `Max Speed`: top speed
   - `Acceleration`: how quickly it gains speed
   - `Braking`: how quickly it slows down
   - `Turn Speed`: turning smoothness
   - `Stopping Distance`: how close it gets before switching waypoint

## 2) Recommended Car Prefab Hierarchy

Use this structure for clean, reusable prefabs:

- `TrafficCar_Base` (root)
- `CarBody` (mesh root)
- `Wheel_FL`
- `Wheel_FR`
- `Wheel_RL`
- `Wheel_RR`

Attach movement scripts/components on `TrafficCar_Base` (root).

## 3) Required Components on Car Root

Add these to `TrafficCar_Base`:

- `Rigidbody`
- `BoxCollider` (or `MeshCollider` convex for custom body, but BoxCollider is recommended for beginners)
- `CarWaypointFollower`
- `ObstacleDetection`

Assign in `CarWaypointFollower`:

- `Waypoint Path` -> your scene path object with `WaypointPath`
- `Vehicle Configuration` -> one of your config assets
- `Car Rigidbody` -> root Rigidbody
- Wheel references -> `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`
- `Obstacle Detection` -> usually auto-finds on same GameObject

Assign in `ObstacleDetection`:

- `Ray Origin` -> optional front bumper transform (recommended)
- `Detection Range` -> how far car checks ahead
- `Brake Force` -> how fast it slows for traffic
- `Minimum Stopping Distance` -> queue gap distance
- `Vehicle Layer Mask` -> only the `Vehicle` layer

## 4) Rigidbody Settings (Recommended)

For stable traffic behavior:

- `Mass`: `1000` to `1800` (cars)
- `Drag`: `0.05` to `0.2`
- `Angular Drag`: `1` to `3`
- `Use Gravity`: enabled
- `Is Kinematic`: disabled
- `Interpolate`: `Interpolate`
- `Collision Detection`: `Continuous` (or `Continuous Dynamic` for faster cars)
- Constraints:
  - Freeze Rotation X: enabled
  - Freeze Rotation Z: enabled

These keep cars upright while allowing yaw turning.

## 5) Collider Settings (Recommended)

- Use one `BoxCollider` on root that covers the vehicle body.
- Keep wheel meshes outside the collider bounds if needed.
- Avoid very large colliders because they cause early collisions.
- For trucks/buses, use a longer box collider matching body length.

Optional advanced setup:
- Add a second small front trigger collider for obstacle detection later.

## 6) Obstacle Detection Setup

1. Create a Unity layer named `Vehicle`.
2. Put all traffic car roots on the `Vehicle` layer.
3. In `ObstacleDetection`, set `Vehicle Layer Mask` to only `Vehicle`.
4. Recommended raycast position:
   - Add child empty object `RayOrigin_Front`
   - Place it near front bumper center
   - Assign this transform to `Ray Origin`
5. Turn on `Show Debug Ray` while tuning:
   - Green ray = clear
   - Red ray = detected vehicle

Queue behavior:
- If a car is close ahead, speed multiplier drops toward `0`.
- Car brakes smoothly and stops before collision.
- When lane is clear again, multiplier returns to `1` and car resumes automatically.

## 7) Reusable Prefab Workflow

1. Build one fully working car in scene (`TrafficCar_Base`).
2. Drag it into `Assets/TrafficSystem/Prefabs/` to create prefab.
3. Name it clearly, for example:
   - `TrafficCar_Sedan`
   - `TrafficCar_Truck`
4. Duplicate prefab for new variants.
5. On each duplicate, assign a different `VehicleConfiguration` asset.
6. Replace only mesh/scale/collider size as needed.

This keeps one movement script and many car types.

## 8) How to Duplicate Traffic Cars in Scene

1. Drag prefab from `Assets/TrafficSystem/Prefabs/` into scene.
2. Duplicate with `Ctrl + D`.
3. Change `Starting Waypoint Index` on each duplicate to spread traffic.
4. Optionally assign different config assets (sedan/truck/bus) per duplicate.

## 9) Quick Test Checklist

- Waypoint path has at least 2 waypoints.
- Car has Rigidbody + Collider + CarWaypointFollower.
- CarWaypointFollower references path and vehicle config.
- Press Play:
  - Car accelerates smoothly
  - Car turns naturally
  - Car keeps moving forward only
  - Car switches waypoints automatically
  - Cars stop behind each other without crashing
  - Cars resume movement when obstacle clears
