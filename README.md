# Fika - Bepinex plugin

Client-side changes to make multiplayer work.

## Requirements

- [Visual Studio Code](https://code.visualstudio.com/)
- [.NET SDK 8.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Build

### Debug / Release

**Tool**   | **Action**
---------- | ------------------------------
Powershell | `dotnet build`
VSCode     | `Terminal > Run Build Task...`

### GoldMaster

1. Have no certificates yet? > `Properties/signing/generate.bat`
2. `dotnet build --configuration GoldMaster`

## Licenses

<img src="https://mirrors.creativecommons.org/presskit/buttons/88x31/png/by-nc-sa.png" alt="cc by-nc-sa" width="196" height="62" style="float:right">

This project is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode.en).

### Dependencies

**Project** | **License**
----------- | -----------------------------------------------------------------------
Aki.Modules | [NCSA](https://dev.sp-tarkov.com/SPT-AKI/Modules/src/branch/master/LICENSE.md)
SIT         | [None](./LICENSE-SIT.md)
Open.NAT    | [MIT](https://github.com/lontivero/Open.NAT/blob/master/LICENSE)
LiteNetLib  | [MIT](https://github.com/RevenantX/LiteNetLib/blob/master/LICENSE.txt)
