# Works
# Execute in the main plugin root folder, where you create all your plugins
# create repo folder and init

# Configuration
$plugin_name = 'MyCustomJellyfinSection' # Use FirstLetterUppercase format or make changes to the script
$plugin_class_name = $plugin_name -creplace '([a-z]+)([A-Z])', '$1.$2' # Example: MyTestProject becomes: My.Test.Project
$plugin_display_name = "My Custom Section"
$dotnet_version = "9.0"

Write-Host "
Script configured to use:
$( get-variable -Name 'plugin*', 'dotnet_version' | Out-String )
This will be the full path of the repo:
'$( join-path (Get-Location) $plugin_name )'

Only continue if you really want to create a new basic repo folder for your plugin/project.
"
Pause
mkdir $plugin_name
Set-Location $plugin_name
git init

# create solution and project
dotnet new sln -n $plugin_name
mkdir src
Set-Location src
dotnet new classlib -n $plugin_class_name -f net$dotnet_version
Set-Location ..
dotnet sln add src/$plugin_class_name/$plugin_class_name.csproj

# open in VS Code
code .

git add .
git commit -m "Initial plugin skeleton: net$dotnet_version classlib"


dotnet add src/$plugin_class_name package MediaBrowser.Common
dotnet add src/$plugin_class_name package MediaBrowser.Controller
dotnet add src/$plugin_class_name package MediaBrowser.Model


Pause

.\create-plugin-files.ps1 -ProjectDir "src/$plugin_class_name" -Namespace $plugin_class_name -Guid "22222222-3333-4444-5555-666666666666" -DisplayText $plugin_display_name
