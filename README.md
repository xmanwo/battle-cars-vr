# Battle Cars VR

A Unity VR arena vehicle combat game targeting Meta Quest 3 / Android.

Players choose a battle car, fight waves of enemy vehicles, and select or upgrade weapons between waves. Weapons include guns, shotguns, flamethrowers, and mines.

## Development environment

- Unity Editor **6000.3.8f1** (see `ProjectSettings/ProjectVersion.txt`).
- Android Build Support, including the Android SDK & NDK Tools and OpenJDK modules.
- Meta XR SDK **85.0.0**, Universal Render Pipeline **17.3.0**, and OpenXR **1.16.1**, declared in `Packages/manifest.json`.
- Meta Quest 3 for device testing.

## Open and build

1. Clone this repository.
2. In Unity Hub, add the repository root as a project and open it with Unity 6000.3.8f1.
3. Allow Unity to download the packages and import the assets. The generated `Library` directory is intentionally excluded from version control.
4. Open `Assets/Scenes/Arena.unity`, the only enabled scene in the current build settings.
5. Select Android in Build Profiles and build an APK. Install it on a Quest 3 for testing.

This snapshot has been checked for project structure and asset metadata consistency. Package restoration, Editor compilation, and an Android build have not been verified as part of that check. Some scene references are supplied by packages and require package restoration before validation.

## Controls

| Controller | Input | Action |
| --- | --- | --- |
| Left | Thumbstick | Move the car |
| Left | Trigger | Brake / stop |
| Left | X | Reload supported weapons |
| Left | Y | Recover an overturned car |
| Right | Thumbstick | Aim; move between selection options |
| Right | Trigger | Fire; confirm a selection |
| Right | B | Drop a mine when supported by the equipped setup |

See `README_Battle_Cars.txt` for the original player instructions.

## Repository contents

Track `Assets/` together with its `.meta` files, `Packages/` (including both `manifest.json` and `packages-lock.json`), `ProjectSettings/`, and the project documentation.

The `.gitignore` excludes generated caches, local editor settings, build outputs, recovery snapshots, and `.unitypackage` installer/conversion archives with their corresponding metadata. Keep the excluded archives locally if they are needed for future render-pipeline conversions. No serialized GUID references to these archives were found during the static check.

The provided `Battle Cars.apk` is a build artifact and is excluded from source control. Attach a build to a GitHub Release when distributing the game.

## Third-party assets and attribution

The project includes third-party assets and packages, including FlatKit, TopDownEngine, environment packs, vehicle models, effects, and audio. Each remains subject to its own license. This README does not grant redistribution rights to those assets.

Use a private repository for personal backup until the included assets' licenses have been reviewed. Grant collaborator access only when permitted by the relevant licenses. Before publishing a public source repository, identify and remove or replace assets whose licenses prohibit source redistribution, and document the required separately obtained dependencies. Do not apply a blanket open-source license to third-party content.

The attribution recorded in `Assets/Readme.txt` is:

> "Generic passenger car pack" (https://skfb.ly/6sUFy) by Comrade1280 is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).

Preserve the included attribution and license files. Additional third-party license review is still required.
