@echo off
rem 清理构建残留：删除版本目录中除本模组 DLL 外的复制品，并移除 obj/
set "exclude_file=Clap_Your_Hands.dll"
set "root=%~dp0Clap_Your_Hands"

for %%d in (1.6\Assemblies 1.7\Assemblies) do (
  pushd "%root%\%%d"
  for /f "delims=" %%i in ('dir /b /a-d ^| findstr /v /x "%exclude_file%"') do (
    del "%%i"
  )
  popd
)
rd "%root%\obj" /s /q
pause
