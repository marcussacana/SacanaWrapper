call "%VS140COMNTOOLS%vcvars.bat"
call "%VS140COMNTOOLS%VSVars32.bat"
call "%VS140COMNTOOLS%VsDevCmd.bat"
set PATH=%PATH%;C:\Windows\Microsoft.NET\Framework\v4.0.30319\
echo off
cls
setlocal enabledelayedexpansion

for /f "delims=" %%s in ('dir /b /s *.cs') do (
	set File=%%~nxs
	set Name=%%~ns
	set References=
	
	for /f "tokens=*" %%r in ('FINDSTR /C:"#IMPORT" !File!') do (
	call :GetReference "%%r"
	if "!References!" == "" (
		set References=!str!
	) else (
		set References=!References!,!str! 
	))
	
	if exist tmp.cs (del /Q tmp.cs)
	if exist "!Name!.dll" (del /Q "!Name!.dll")
	
	findstr /V "#IMPORT" "!File!">"tmp.cs"

	if "!References!"=="" (
		csc "/lib:%CD%" /debug:pdbonly "/out:!Name!.dll" /t:library tmp.cs
	) else (
		csc "/lib:%CD%" /debug:pdbonly "/out:!Name!.dll" /t:library "/r:!References!" tmp.cs
	)
	
	if exist "!Name!.dll" (
		call :ColorEcho Green "!File! - BUILD SUCCESSFULLY"
	) else (
		call :ColorEcho Red "!File! - FAILED TO BUILD"
	)
	del /Q tmp.cs
)

goto :eof
:GetReference
set str=%1
for /f "useback tokens=*" %%a in ('!str!') do set str=%%~a
if not "!str:~0,1!" == "#" (
	set str=!str:~2!
)
set str=!str:~8!
set str=!str:^%CD^%\Plugins\=\!
call :TRIM str
set str=!str:System.Linq=System.Core!
set str=!str:System.Collections.Generic=System.Core!
set str=!str:System.Text=System.Core!
goto :eof

:ColorEcho
powershell write-host -fore %1 %2
goto :eof


:TRIM
SetLocal EnableDelayedExpansion
Call :TRIMSUB %%%1%%
EndLocal & set %1=%tempvar%
GOTO :EOF

:TRIMSUB
set tempvar=%*
GOTO :EOF