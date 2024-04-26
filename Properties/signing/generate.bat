@echo off

:: change <2022> to the MSBuildTools version you're using
:: call "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsDevCmd.bat"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"

sn -k private.snk
sn -p private.snk public.snk sha256

pause