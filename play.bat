@echo off
setlocal

REM Define the folder path where the videos are located

REM !!! vvv THIS IS WHAT YOU WANT TO CHANGE vvv !!!
set "folderPath=C:\Users\david\OneDrive\Documents\VOCA-UK\In-Person\RakuCon\2023\Rendered Videos"
rem !!! ^^^ THIS IS WHAT YOU WANT TO CHANGE ^^^ !!!

REM Define the monitor inputs
set "lyricMonitor=%~1"
set "nonLyricMonitor=%~2"

REM Define the search input
set "searchInput=%~3"

REM Define to sleep or not
set "sleep=%~4"

REM Exit if the search input is empty
if "%searchInput%"=="" (
  echo Usage: %~nx0 lyricMonitor nonLyricMonitor "searchInput" [sleep]
  pause
  exit /b
)

REM Define the path to the MPV executable
set "mpvPath=./mpv/mpv.exe"

REM Enable delayed expansion for variables
setlocal enabledelayedexpansion

REM Loop through the files in the folder
for /r "%folderPath%" %%F in (*.mp4) do (
  REM Get the file name without extension
  set "fileName=%%~nF"
  
  REM Check if the file name matches the search input
  if "!fileName!"=="%searchInput%" (
    REM Check if a lyric version exists
    if exist "!folderPath!\[LYRIC] !fileName!.mp4" (
      REM Open the lyric version in fullscreen on the first monitor
      start "" /B "%mpvPath%" --player-operation-mode=pseudo-gui --fs --fs-screen=%lyricMonitor% --no-audio "!folderPath!\[LYRIC] !fileName!.mp4"

      REM Timeout for video delays
      if not "%sleep%"=="" (
        REM This should generally be 1250 unless something is horribly wrong
        CSCRIPT SLEEP.VBS %sleep%

        REM Open the non-lyric version in fullscreen on the second monitor
        start "" /B "%mpvPath%" --player-operation-mode=pseudo-gui  --fs --fs-screen=%nonLyricMonitor% "!folderPath!\!fileName!.mp4"
      ) else (
        REM Open the non-lyric version in fullscreen on the second monitor
        start "" /B "%mpvPath%" --player-operation-mode=pseudo-gui  --fs --fs-screen=%nonLyricMonitor% "!folderPath!\!fileName!.mp4"
      )
    ) else (
      REM Open the non-lyric version in fullscreen on the first monitor
      start "" /B "%mpvPath%" --player-operation-mode=pseudo-gui --fs --fs-screen=%lyricMonitor% "!folderPath!\!fileName!.mp4"
      
      REM Open a blank window on the second monitor
      start "" /B cmd /c
    )
    
    REM Exit the loop after finding the matching file
    exit /b
  )
)

REM Display an error message if no matching file was found
echo No matching file found.
pause