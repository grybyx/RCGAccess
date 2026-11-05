@echo off
rem RCGAccess: packs logs and settings into a ZIP on the desktop (date and time in the name).
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
 "$ts = Get-Date -Format 'yyyy-MM-dd_HH-mm';" ^
 "$desk = [Environment]::GetFolderPath('Desktop');" ^
 "$tmp = Join-Path $env:TEMP ('RCGAccess_Daten_' + $ts);" ^
 "New-Item -ItemType Directory -Force $tmp | Out-Null;" ^
 "if (Test-Path 'RCGAccess') { Copy-Item 'RCGAccess\*.log','RCGAccess\*.ini','RCGAccess\*.txt' $tmp -ErrorAction SilentlyContinue; if (Test-Path 'RCGAccess\Protokolle') { Copy-Item 'RCGAccess\Protokolle' $tmp -Recurse -ErrorAction SilentlyContinue } };" ^
 "if (Test-Path 'BepInEx\LogOutput.log') { Copy-Item 'BepInEx\LogOutput.log' $tmp };" ^
 "$ul = Join-Path $env:USERPROFILE 'AppData\LocalLow\WayForward\River City Girls';" ^
 "if (Test-Path $ul) { Copy-Item (Join-Path $ul '*.txt'),(Join-Path $ul '*.log') $tmp -ErrorAction SilentlyContinue };" ^
 "$zip = Join-Path $desk ('RCGAccess_Daten_' + $ts + '.zip');" ^
 "if (Test-Path $zip) { Remove-Item $zip };" ^
 "Compress-Archive -Path (Join-Path $tmp '*') -DestinationPath $zip;" ^
 "Remove-Item $tmp -Recurse -Force;" ^
 "$last = Get-Item 'RCGAccess\RCGAccess.log' -ErrorAction SilentlyContinue;" ^
 "Write-Host ('Fertig: ' + $zip);" ^
 "if ($last) { Write-Host ('Letztes Mod-Protokoll vom ' + $last.LastWriteTime.ToString('dd.MM.yyyy HH:mm')) }"
pause
