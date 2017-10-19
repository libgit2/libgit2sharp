dotnet build "%~dp0"
robocopy /NJS /NJH /NDL "%~dp0..\bin\CodeGeneration\Debug\netstandard1.5" "%~dp0..\lkg" CodeGeneration.dll CodeGeneration.pdb
dotnet build "%~dp0..\libgit2sharp"
