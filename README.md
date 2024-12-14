# Fika - Bepinex plugin

[![Discord](https://img.shields.io/discord/1202292159366037545?style=plastic&logo=discord&logoColor=FFFFFF&label=Fika%20Discord)](https://discord.gg/project-fika)
[![Downloads](https://img.shields.io/github/downloads/project-fika/Fika-Plugin/total?style=plastic&logo=github)](https://github.com/project-fika/Fika-Plugin/releases/latest)
![Size](https://img.shields.io/github/languages/code-size/project-fika/Fika-Plugin?style=plastic&logo=github)
![Issues](https://img.shields.io/github/issues/project-fika/Fika-Plugin?style=plastic&logo=github)
[![License](https://img.shields.io/badge/CC--BY--NC--SA--4.0-blue?style=plastic&logo=creativecommons&logoColor=FFFFFF&label=License)](https://github.com/project-fika/Fika-Plugin/blob/main/LICENSE.md)
[![Crowdin](https://badges.crowdin.net/project-fika/localized.svg)](https://crowdin.com/project/project-fika)

Client-side changes to make multiplayer work.

## State of the project

There are few bugs left. The goal now is to look back and refactor old code to
make it better, as a lot of it is not efficient or easy to read.

## Contributing

You are free to fork, improve and send PRs to improve the project. Please try
to make your code coherent for the other developers.

## Requirements

- [Visual Studio Code](https://code.visualstudio.com/)
- [.NET SDK 8.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Setup

1. Copy-paste the contents of `EscapeFromTarkov_Data/Managed/` into
    `References/`
2. Copy-paste from SPT.Modules `project/Shared/Hollowed/hollowed.dll` into
    `References/`

## Build

### Debug / Release

**Tool**   | **Action**
---------- | ------------------------------
PowerShell | `dotnet build`
VSCode     | `Terminal > Run Build Task...`

You have to create a `References` folder and populate it with the required
dependencies from your game installation for the project to build.

### GoldMaster

1. Have no certificates yet? > `Properties/signing/generate.bat`
2. `dotnet build --configuration GoldMaster`

## Licenses

[<img src="https://mirrors.creativecommons.org/presskit/buttons/88x31/svg/by-nc-sa.svg" alt="cc by-nc-sa" width="180" height="63" align="right">](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode.en)

This project is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode.en).

### Credits

**Project** | **License**
----------- | -----------------------------------------------------------------------
SPT.Modules | [NCSA](https://dev.sp-tarkov.com/SPT/Modules/src/branch/master/LICENSE.md)
SIT         | [NCSA](./LICENSE-SIT.md) (`Forked from SIT.Client master:9de30d8`)
Open.NAT    | [MIT](https://github.com/lontivero/Open.NAT/blob/master/LICENSE) (for UPnP implementation)
LiteNetLib  | [MIT](https://github.com/RevenantX/LiteNetLib/blob/master/LICENSE.txt) (for P2P UDP implementation)
Mirror Networking  | [MIT](https://github.com/MirrorNetworking/Mirror/blob/master/LICENSE) (for Snapshot Interpolation implementation)

<a href="https://crowdin.com/?utm_term=click-badge-add-on" rel="nofollow"><img style="width:140;height:40px" src="https://badges.crowdin.net/badge/light/crowdin-on-dark.png" srcset="https://badges.crowdin.net/badge/light/crowdin-on-dark.png 1x,https://badges.crowdin.net/badge/light/crowdin-on-dark@2x.png 2x" alt="Crowdin | Agile localization for tech companies" /></a>
