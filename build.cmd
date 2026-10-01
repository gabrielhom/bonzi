@echo off
rem Compila o Bonzi.exe com o csc.exe que já vem no Windows (.NET Framework 4), sem instalar nada.
rem assets\ vem do pacote npm clippyjs@0.1.0 (dist/agents/bonzi), extraído do Bonzi.acs original.
pushd "%~dp0"
set F=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
"%F%\csc.exe" /nologo /codepage:65001 /target:winexe /out:Bonzi.exe ^
  /r:"%F%\WPF\PresentationFramework.dll" /r:"%F%\WPF\PresentationCore.dll" /r:"%F%\WPF\WindowsBase.dll" ^
  /r:"%F%\System.Xaml.dll" /r:"%F%\System.Web.Extensions.dll" ^
  /r:"%WINDIR%\Microsoft.NET\assembly\GAC_MSIL\System.Speech\v4.0_4.0.0.0__31bf3856ad364e35\System.Speech.dll" ^
  /resource:assets\agent.json,agent.json /resource:assets\map.png,map.png ^
  /resource:assets\1.mp3,1.mp3 /resource:assets\2.mp3,2.mp3 /resource:assets\3.mp3,3.mp3 /resource:assets\4.mp3,4.mp3 ^
  /resource:assets\5.mp3,5.mp3 /resource:assets\6.mp3,6.mp3 /resource:assets\7.mp3,7.mp3 ^
  Bonzi.cs
set ERRO=%ERRORLEVEL%
popd
exit /b %ERRO%
