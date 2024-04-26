@echo off
set outputFile=%1
set sourceDir=%2
set buildDir=%sourceDir%..\Build
set bepinexDir=%buildDir%\BepInEx
set pluginsDir=%bepinexDir%\plugins

if exist %pluginsDir% (
    if exist %pluginsDir%\Fika.Core.dll (
        del /f /q %pluginsDir%\Fika.Core.dll
    )
)

if not exist %buildDir% (
    mkdir %buildDir%
)

if not exist %bepinexDir% (
    mkdir %bepinexDir%
)

if not exist %pluginsDir% (
    mkdir %pluginsDir%
)

copy %outputFile% %pluginsDir%\Fika.Core.dll