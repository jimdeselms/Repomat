if not exist src\Repomat\lib mkdir src\Repomat\lib

copy src\Repomat\bin\debug\Repomat.dll src\Repomat\lib

.nuget\Nuget.exe pack src\Repomat\Repomat.csproj