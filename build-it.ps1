param(
    [string]$Namespace = "My.Custom.Section.Plugin",
    [string]$ProjectDir, # defaults to "src/${Namespace}"
    [string]$sln_filename = "", # optional - build all projects
    [string]$mode = "release", # release or debug (needs to check how debug works)
    [string]$dotnet_version = "9.0"
)

function ErrorCheck {
    #Stop the script if dotnet returns an error
    if ($LASTEXITCODE -ne 0) {
    return #throw "dotnet build failed with exit code $LASTEXITCODE"
}
    
}

if ([string]::IsNullOrWhiteSpace($ProjectDir)) {
    $ProjectDir = "src/${Namespace}"
}

dotnet restore

# * Build just the plugin project (fast)
dotnet build ${ProjectDir}\${Namespace}.csproj -c Release
ErrorCheck


# * Build the whole solution (all projects)
if ($sln_filename) {
    dotnet build ${sln_filename}.sln -c Release
    ErrorCheck
}

$project_build_source = (Resolve-Path .\${ProjectDir}\bin\${mode}\net9.0).Path

# * Create folder name for the plugin release
# ? Clean the plugin name (Namespace) by removing any dots and include the version (optional)
# TODO I could implement a way to auto update the version here in the script
$plugin_version = "1.0.0.0" # format: 0.0.0.0
$plugin_name_clean = $Namespace -replace '\.',''
$plugin_release_name = ($plugin_version) ? "${plugin_name_clean}_${plugin_version}" : $plugin_name_clean 

# ? jellyfin userdata folder > plugins > PluginName_0.0.0.0 
$release_plugin_folder = "${Env:localappdata}\jellyfin\plugins\${plugin_release_name}"

New-Item -ItemType Directory -Force -Path $release_plugin_folder | Out-Null
Copy-Item "$project_build_source\*" $release_plugin_folder -Recurse -Force -Verbose
Get-ChildItem $release_plugin_folder -Recurse | Unblock-File


# * Compare main dll to make sure it got replace. Unnecessary here, was using for other stuff, can remove later
$built = Get-Item ${project_build_source}\${Namespace}.dll
$deployed = Get-Item ${release_plugin_folder}\${Namespace}.dll
"Built:    $($built.LastWriteTimeUtc) $($built.Length) bytes"
"Deployed: $($deployed.LastWriteTimeUtc) $($deployed.Length) bytes"

#Return

# * launch jellyfin to see if the plugin works 

# ? Find the last jellyfin log and remove it, to make sure we capture a clean log
$jellyfin_log_folder = "${Env:localappdata}\jellyfin\log"
$jellyfin_last_log = Get-ChildItem -Path $jellyfin_log_folder -Filter log*.log 
$jellyfin_last_log = $jellyfin_last_log | Sort-Object LastWriteTime -Descending | Select-Object -First 1

Remove-item -path $jellyfin_last_log -verbose -ErrorAction SilentlyContinue

Start-Process -FilePath pwsh -ArgumentList "-Command & 'C:\Program Files\Jellyfin\Server\jellyfin.exe' --datadir '${Env:localappdata}\jellyfin'"
Start-Sleep -sec 10

$pattern_list = @(
    $Namespace
    $plugin_name_clean
    (Get-Item .).Name
)
$pattern_escaped_list = $pattern_list | ForEach-Object { [regex]::Escape($_) } 
$pattern_escaped_list = $pattern_list -join '|'

Write-Host "log filter, after the initial load display only lines where it matches with our `$pattern_list:"
$pattern_list

Get-Content $jellyfin_last_log -tail 200 | Where-Object { $_ -match $pattern_escaped_list } | ForEach-Object { $_; '' }

# * Opens a window with the jellyfin live log 
# Start-Process powershell -ArgumentList "-NoExit","-Command","Get-Content -Path `"${jellyfin_last_log}`" -Tail 100 -Wait"
# ? in case you want to start jellyfin as a service/ in the background 
# ? the Start-Process method already displays the log on the terminal so this is not necessary

Pause