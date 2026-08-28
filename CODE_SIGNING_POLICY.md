# Code signing policy

Free code signing provided by SignPath.io, certificate by SignPath Foundation.

## Project

LeipzigCraft Launcher

Repository: https://github.com/deinVater94/LeipzigCraft-Launcher

## Team roles

- Authors / committers: deinVater94
- Reviewers: deinVater94
- Approvers: deinVater94

Changes submitted by external contributors must be reviewed by a project maintainer before they are merged.

Every release-signing request must be manually reviewed and approved by an authorized approver.

## Build and release policy

Official LeipzigCraft Launcher binaries are built from the public source repository using GitHub Actions and GitHub-hosted Windows runners.

Release binaries intended for code signing must originate from the official repository and from the repository's version-controlled build workflow.

The project does not permit manually substituted binaries to be presented as official signed builds.

## Privacy

See [PRIVACY.md](PRIVACY.md).

The launcher communicates with network services only to provide functionality explicitly requested by the user, including Microsoft/Minecraft authentication, Minecraft/Fabric installation, LeipzigCraft update checks and downloads, and game/server connectivity.

## Uninstallation

See [UNINSTALL.md](UNINSTALL.md).

## Security

Private keys used by the LeipzigCraft pack-manifest signing system are not stored in this repository.

The launcher embeds only the corresponding public verification keys.

All maintainers with repository or SignPath access must use multi-factor authentication.
