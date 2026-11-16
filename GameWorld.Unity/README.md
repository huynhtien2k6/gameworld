# GameWorld Unity client prototype

This Unity package connects to the existing `GameWorld.Api` backend. It includes a runtime-generated village scene and a first playable loop for registration/login, character creation/selection, travel, combat, farming, harvesting, cooking, and resting. Gameplay decisions and rewards remain server-authoritative.

## Add to a Unity project

1. Create a 3D project with Unity 2022.3 LTS or Unity 6.
2. In Unity, open **Window > Package Management > Package Manager**, click **+**, choose **Install package from disk...**, and select this folder's `package.json`.
3. Create an empty scene and an empty GameObject named `GameWorldClient`.
4. Add the `GameWorld.UnityClient.GameWorldClient` component. It adds the API and village-scene components automatically.
5. Set **Api Base Url** in the component to the API address reachable from the target device.
6. Start the backend from the repository root with `dotnet run --project GameWorld.Api --launch-profile http`.

Use `http://localhost:5162` in the Unity Editor on the same computer. For Android Emulator, run the API bound to `http://0.0.0.0:5162` and use `http://10.0.2.2:5162`. For a physical device, use the development computer's LAN address and allow the port through its firewall. Use HTTPS and a trusted certificate for non-development environments; do not expose the development server publicly.

## Prototype limits

- Access and refresh tokens exist only in memory and are discarded when logging out or closing the app.
- The 3D village is assembled from Unity primitives at runtime; it is a visual prototype, not the finished low-poly/cel-shaded art pipeline.
- The camera can orbit the scene, but avatar movement, real-time multiplayer, chat, mobile touch controls, and asset streaming are not implemented.
- Install into a Unity project to compile and run; the repository's .NET build does not compile Unity scripts.
