# LeipzigCraft Launcher

<p align="center">
  <img src="Assets/leipzigcraft-logo.png" alt="LeipzigCraft" width="320">
</p>

<p align="center">
  <strong>Open-source Windows launcher for the LeipzigCraft Minecraft community server.</strong>
</p>

<p align="center">
  Minecraft 1.21 · Fabric Loader 0.17.3 · Windows x64
</p>

---

## Download

The latest public release is available under:

**[GitHub Releases](https://github.com/deinVater94/LeipzigCraft-Launcher/releases)**

> The current release candidate is not yet covered by the final trusted code-signing setup.
> Windows SmartScreen may therefore show a warning when launching the executable.

## What the launcher does

LeipzigCraft Launcher is designed to make joining the LeipzigCraft server as simple as possible, including for players who have never installed Minecraft mods manually.

It provides:

- Microsoft / Minecraft authentication
- Automatic Minecraft 1.21 setup
- Fabric Loader 0.17.3
- Automatic LeipzigCraft modpack installation
- Incremental mod updates
- SHA-256 verification of downloaded files
- Cryptographically signed update-manifest verification
- Isolated installation under `%APPDATA%\LeipzigCraft`
- Automatic LeipzigCraft multiplayer server entry
- Protection against duplicate game launches
- Separate launcher and game state from the user's normal `.minecraft` installation

## Update security

The launcher retrieves the LeipzigCraft update manifest from:

`https://leipzigcraft.com/launcher/pack.json`

The corresponding detached signature is retrieved from:

`https://leipzigcraft.com/launcher/pack.sig`

The launcher contains only the public verification keys.

Before accepting an update manifest, the launcher verifies its cryptographic signature. If the signature is missing or invalid, the update is rejected before any managed mod files are downloaded.

Individual downloaded files are also verified using SHA-256 hashes stored in the signed manifest.

Private manifest-signing keys are never stored in this repository.

## Game installation

The launcher maintains its own installation under:

```text
%APPDATA%\LeipzigCraft\game
```

This keeps LeipzigCraft separate from the user's regular Minecraft installation.

The configured Minecraft environment is:

```text
Minecraft:     1.21
Fabric Loader: 0.17.3
```

## Multiplayer server

The launcher automatically adds LeipzigCraft as the first entry in the multiplayer server list of its isolated Minecraft installation.

```text
Name:    LeipzigCraft
Address: 185.9.104.131:10100
```

## Building

Official Windows binaries are built from this repository using GitHub Actions on GitHub-hosted Windows runners.

The project targets:

```text
.NET 8
WPF
Windows x64
```

The build workflow produces a self-contained Windows executable.

## Open Source

LeipzigCraft Launcher is open-source software licensed under the [MIT License](LICENSE).

## Code signing policy

See the [Code signing policy](CODE_SIGNING_POLICY.md).

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Privacy

See the [Privacy Policy](PRIVACY.md).

## Uninstall

See the [Uninstallation instructions](UNINSTALL.md).

## Security

If you discover a security issue involving the launcher or its update mechanism, please avoid publishing exploit details before the maintainers have had a reasonable opportunity to investigate.

## Disclaimer

LeipzigCraft is an independent community project and is not affiliated with, endorsed by, authorized by, or associated with Mojang Studios or Microsoft.

Minecraft is a trademark of Microsoft Corporation and/or Mojang Studios.
