call "%VS140COMNTOOLS%vcvars.bat"
call "%VS140COMNTOOLS%VSVars32.bat"
call "%VS140COMNTOOLS%VsDevCmd.bat"
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
	findstr /V "#IMPORT" "!File!">"tmp.cs"

	csc "/lib:%CD%" /t:library "/r:!References!" tmp.cs
	if exist tmp.dll (
		del !Name!.dll
		ren tmp.dll !Name!.dll
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
goto :eof

:ColorEcho
powershell write-host -fore %1 %2
goto :eof