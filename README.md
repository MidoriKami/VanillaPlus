# Vanilla Plus
[![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/VanillaPlus)](https://github.com/MidoriKami/VanillaPlus)

This plugin is a collection of smaller ui-focused modifcations to the game. Using KamiToolKit we are able to have more control than ever over adding or modifing native user interface elements.

This plugin uses the term "GameModification" to describe a singular purposed module that modifies the game in some way, this is broadly categorized as one of the four following categories.

> [!IMPORTANT]  
> All features of this plugin must comply with Dalamuds Plugin Rules and Policies
> 
> No automating server interactions
>
> No modifications that would give clear unfair advantages to players
> 
> No modifications that specifically target PvP parts or components of any form

The purpose of this plugin is to enhance the game through quality of life features, displaying available information in smarter ways, and addressing quirky problems with the games design/implementation.

This plugin is not intended to play the game for you, nor make any decisions on your behalf.

### Game Behavior Modification

These are modifications on how the game performs a task or function. The modifications can change or replace the behavior of certain parts of the game.

For example Faster Scrollbars, this feature makes the game scroll further (configurable) than normal for each tick of the mousewheel, this reduces on the tedious amount of scrolling normally required to navigate the games menus.

## UI Modifications

Generally this will either be additions to the native ui in some small way, or just slightly modifying how something displays.

For example Fade Unavailable Actions, will fade out buttons that you don't have access to, this isn't adding any ui elements, but its primary purpose is to modify these elements.

For example Target CastBar Countdown, adds a completely new text node to the castbar element that displays how much time is remaining on the targets cast.

## Custom Native Windows

The configuration window for this plugin is an example of a native window, these are windows that are built using the games native rendering system itself to be indistinguishable from other ingame windows.

New windows include Better Teleport Window, which is a fully replacement for the games teleport window with several enhanced features.

Another example is the Fate List Window, which shows a list of all active fates in the map you are currently occupying, ordered by how long is remaining for each fate.

## Custom Native Overlay

Custom native overlays are elements that persist on the screen and are generally intended to be for HUD or informational purposes.

There are a couple currency related overlays, such as Currency Warning, which shows an animated icon on your screen to warn you when set currency values are either too high or too low.

There are also Clock overlys, and other forms of Currency overlays.

# Contributing

Contributions to this project are welcomed and encouraged, you can use existing GameModifications as reference on how to make your own, but here are a couple requirements:

Your GameModification must be contained inside a folder of the same name, even if your modification is just one file.

You are welcome and encouraged to use multiple CS files to implement your game modification, but if your modification is excessively large or complex it might be rejected, before working on something you suspect will turn out to be large and complex, I encourage you to reach out to me beforehand.

> [!TIP]
> ### Getting Started Contributing
> 
> 1. Fork this project
> 2. Clone your forked repository locally
> 3. Copy and paste "SampleGameModification" folder from `DevFeatures/SampleGameModification`
> 4. Move copy into `Features/` folder
> 5. Rename the class, folder, and file, then fill out the Modification Info in the struct
>
> When a module is toggled on the OnEnable function is called, and when a module is toggled off, the OnDisable function is called
>
> There are debug modules that you can use for easy prototyping in the `DevFeatures` folder
>
> Do not commit any modifications to any files in the `DevFeatures` folder
