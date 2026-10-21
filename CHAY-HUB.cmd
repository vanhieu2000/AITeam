@echo off
rem Bam dup: build (neu can) va chay AITeam Hub o nen, roi mo trinh duyet http://127.0.0.1:5057
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0team.ps1" hub
if errorlevel 1 pause
