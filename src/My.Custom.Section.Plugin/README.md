Obs.: This is not fully functional yet, it's work in progress. Not sure if/when I will finish it.  
While I use and create a few scripts, programming is just a hobby, I'm still a little lost on how to properly use vscode + github and I know very little of C#, so if you see something odd that's why. Basically most of the time I don't know what I'm doing. 

## Objective

Add a custom section in Jellyfin home page without having to create/maintain a fork, or directly inject .js to server files (ex: index.html).

The easiest path to achieve that is to probably use the plugin: Home Screen Sections, because of what is described in their github, [more specifically here](https://github.com/IAmParadox27/jellyfin-plugin-home-sections?tab=readme-ov-file#adding-your-own-sections), I will talk about that later too.

## Context

Jellyfin doesn't really give the admins many tools to change their servers Home page, simple stuff like creating new sections ([/jellyfin-web/tree/master/src/components/homesections/sections](https://github.com/jellyfin/jellyfin-web/tree/master/src/components/homesections/sections)) is not supported.

AFAIK the plugin Home Screen Sections replaces(?) the Home, and, as already mentioned and explained later, it also allow users to create their own sections, but that doesn't seems to be fully functional yet, in the sense that you can just enable an option and point to a file that initiates your custom section. 

So I decided to create a small plugin that will hold my custom section to use with Home Screen Sections, no idea if it will work nor if there is a "better" solution.

Home Screen Sections need other 2 plugins to work (same author) and they all have github pages, so I will use:

1. [Home Screen Sections (Modular Home)](https://github.com/IAmParadox27/jellyfin-plugin-home-sections) | [File Tree](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/tree/main/src/Jellyfin.Plugin.HomeScreenSections)
1. [File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation ) | [File Tree](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation/tree/main/src/Jellyfin.Plugin.FileTransformation)
1. [Plugin Pages](https://github.com/IAmParadox27/jellyfin-plugin-pages ) | [File Tree](https://github.com/IAmParadox27/jellyfin-plugin-pages/tree/main/src/Jellyfin.Plugin.PluginPages)

From the github page of Home Screen Sections:

<blockquote>

### Adding your own sections
> This is great an' all but I want a section that doesn't exist here. Can I make one?

Yep! Home Screen Sections exposes a static interface which can be used to register sections.

Due to issues with Jellyfin's plugins being loaded into different load contexts this cannot be referenced directly. Instead you can use reflection to invoke the plugin directly to register your section.

1. Prepare your payload
```json
{
    "id": "00000000-0000-0000-0000-000000000000", // Guid
    "displayText": "", // What text should be displayed by default for your section
    "limit": 1, // The number of times this section can appear up to
    "route": "", // The route that should be linked on the section header, if applicable
    "additionalData": "", // Any accompanying data you want sent to your results handler
	"resultsAssembly": GetType().Assembly.FullName, // Example value is a string from C# that should be resolved before adding to json
	"resultsClass": "", // The name of the class that should be invoked from the above assembly
	"resultsMethod": "" // The name of the function that should be invoked from the above class
}
```
2. Send your payload to the home screen sections assembly
```csharp
Assembly? homeScreenSectionsAssembly =
	AssemblyLoadContext.All.SelectMany(x => x.Assemblies).FirstOrDefault(x =>
		x.FullName?.Contains(".HomeScreenSections") ?? false);

if (homeScreenSectionsAssembly != null)
{
	Type? pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");

	if (pluginInterfaceType != null)
	{
		pluginInterfaceType.GetMethod("RegisterSection")?.Invoke(null, new object?[] { payload });
	}
}
```

When your section results method is invoked you will receive an object representing the following json format (it will try to serialize it to the type you specify in the signature)
```json
{
  "UserId": "", // The GUID of the user that is requesting the section
  "AdditionalData": "" // The additional data you sent in the registration
}
```

You must make sure that your section results method returns a `QueryResult<BaseItemDto>`.

</blockquote>

## My Setup:

1. I already have jellyfin configured and working (version 11.11.2)
1. I'm using caddy + duckdns for reverse proxy & DDNS, for more security and to have a domain to access my server without having to remember an IP
1. In jellyfin I generated an API Key: "External applications are required to have an API key in order to communicate with the server. Keys are issued by logging in with a normal user account or manually granting the application a key." -may need this later-
1. The plugins/ prerequisites mentioned are also working, default settings.