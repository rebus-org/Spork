@echo off

echo Restoring packages...
dotnet restore -r win-x64 -p:RuntimeIdentifiers=win-x64 --interactive
if %ERRORLEVEL% neq 0 (
 	goto exit_fail
)

echo Testing...
dotnet test --no-restore
if %ERRORLEVEL% neq 0 (
 	goto exit_fail
)

echo Publishing...
dotnet publish Spork -c Release --self-contained --no-restore -r win-x64 -p:PublishSingleFile=true;PublishTrimmed=true 
if %ERRORLEVEL% neq 0 (
 	goto exit_fail
)





goto exit_success
:exit_fail
exit /b 1
:exit_success