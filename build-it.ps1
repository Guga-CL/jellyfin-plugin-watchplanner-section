function ErrorCheck {
    if ($LASTEXITCODE -ne 0) {
    return #throw "dotnet build failed with exit code $LASTEXITCODE"
}
    
}
dotnet restore

dotnet build src\My.Custom.Section.Plugin\My.Custom.Section.Plugin.csproj -c Release
ErrorCheck

dotnet build MyCustomJellyfinSection.sln -c Release
ErrorCheck