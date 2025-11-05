param(
    [string]$ProjectDir = "src/My.Custom.Section.Plugin",
    [string]$Namespace = "My.Custom.Section.Plugin",
    [string]$sln_filename = "MyCustomJellyfinSection", #replace this later
    [string]$mode = "release", #release / debug
    [string]$dotnet_version = "9.0"
)

function ErrorCheck {
    #Stop the script if dotnet returns an error
    if ($LASTEXITCODE -ne 0) {
    return #throw "dotnet build failed with exit code $LASTEXITCODE"
}
    
}
dotnet restore

dotnet build ${ProjectDir}\${Namespace}.csproj -c Release
ErrorCheck

dotnet build ${sln_filename}.sln -c Release
ErrorCheck

$buildOut = (Resolve-Path .\${ProjectDir}\bin\${mode}\net9.0).Path
# jellyfin userdata folder > plugins > your_plugin_name_0.0.0.0 
$release_plugin_folder = "${Env:localappdata}\jellyfin\plugins\$($Namespace -replace '\.','')"

Copy-Item "$buildOut\*" $release_plugin_folder -Recurse -Force -Verbose
Get-ChildItem $release_plugin_folder -Recurse | Unblock-File


# Compare main dll to make sure it got replace. Unnecessary here, was using for other stuff, can remove later
$built = Get-Item ${buildOut}\${Namespace}.dll
$deployed = Get-Item ${release_plugin_folder}\${Namespace}.dll
"Built:    $($built.LastWriteTimeUtc) $($built.Length) bytes"
"Deployed: $($deployed.LastWriteTimeUtc) $($deployed.Length) bytes"

#Return

# Test It

# Find the last jellyfin log and remove it, that way we make sure to capture a clean one later
$jellyfin_log_folder = "${Env:localappdata}\jellyfin\log"
$jellyfin_last_log = Get-ChildItem -Path $jellyfin_log_folder -Filter log*.log 
| Sort-Object LastWriteTime -Descending 
| Select-Object -First 1

Remove-item -path $jellyfin_last_log -verbose -ErrorAction SilentlyContinue

Start-Process -FilePath pwsh -ArgumentList "-Command & 'C:\Program Files\Jellyfin\Server\jellyfin.exe' --datadir '${Env:localappdata}\jellyfin'"
Start-Sleep -sec 3

# Open new window with the live log

Start-Process powershell -ArgumentList "-NoExit","-Command","Get-Content -Path `"${jellyfin_last_log}`" -Tail 100 -Wait"