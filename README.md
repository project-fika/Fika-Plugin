# Fika - Bepinex plugin

Client-side changes to make multiplayer work.

## /!\ **NOTICE** /!\

If you somehow ended up here while you just want to play a stable release,
please download the release from the discord [here](https://discord.com/channels/1202292159366037545/1224454502531469373).

If you're interested to contribute, then you're at the right place!

## State of the project

There are few bugs left. The goal now is to look back and refactor old code to
make it better, as a lot of it is not efficient or easy to read.

## Contributing

You are free to fork, improve and send PRs to improve the project. Please try
to make your code coherent for the other developers.

## Requirements

- [Visual Studio Code](https://code.visualstudio.com/)
- [.NET SDK 8.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

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

<img src="https://mirrors.creativecommons.org/presskit/buttons/88x31/png/by-nc-sa.png" alt="cc by-nc-sa" width="196" height="62" style="float:right">

This project is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode.en).

### Credits

**Project** | **License**
----------- | -----------------------------------------------------------------------
Aki.Modules | [NCSA](https://dev.sp-tarkov.com/SPT-AKI/Modules/src/branch/master/LICENSE.md)
SIT         | [NCSA](./LICENSE-SIT.md) (`Forked from SIT.Client master:9de30d8`)
Open.NAT    | [MIT](https://github.com/lontivero/Open.NAT/blob/master/LICENSE) (for UPnP implementation)
LiteNetLib  | [MIT](https://github.com/RevenantX/LiteNetLib/blob/master/LICENSE.txt) (for P2P UDP implementation)
