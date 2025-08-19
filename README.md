# Vanilla Plus
[![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/VanillaPlus)](https://github.com/MidoriKami/VanillaPlus)

This plugin is a collection of smaller ui-focused modifcations to the game. Using KamiToolKit we are able to have more control than ever over adding or modifing native user interface elements.

This plugin uses the term "GameModification" to describe a singular purposed module that modifies the game in some way, this is broadly categorized as one of the four following categories.

> [!IMPORTANT]  
> All features of this plugin must comply with Dalamuds Plugin Rules and Policies
> 
> No automating server interactions, no modifications that would give clear unfair advantages to other players will be accepted
> 
> No modifications that specifically target PvP parts or components of any form will be accepted

The purpose of this plugin is to enhance the game through quality of life features, displaying available information in smarter ways, and addressing quirky problems with the games design/implementation.

This plugin is not intended to play the game for you, nor make any decisions on your behalf.

### Game Behavior Modification

These are modifications on how the game performs a task or function. The modifications can change or replace the behavior of certain parts of the game.

For example Faster Scrollbars, this feature makes the game scroll further (configurable) than normal for each tick of the mousewheel, this reduces on the tedious amount of scrolling normally required to navigate the games menus.

### UI Modification

Generally this will either be additions to the native ui in some small way, or just slightly modifying how something displays.

For example Fade Unavailable Actions, will fade out buttons that you don't have access to, this isn't adding any ui elements, but its primary purpose is to modify these elements.

For example Target CastBar Countdown, adds a completely new text node to the castbar element that displays how much time is remaining on the targets cast.

### Custom Native Windows

The configuration window for this plugin is an example of a native window, these are windows that are built using the games native rendering system itself to be indistinguishable from other ingame windows.

A couple examples of what this may contain in the future are, a window that shows the last 20 items looted and where they were looted from, a window that shows your inventory items in a vertical list format instead of the normal grid format.

### Custom Native Overlay

Custom native overlays are elements that persist on the screen and are generally intended to be for HUD or informational purposes.

One potential example of this would be a modification that adds icons to the screen to point you towards your next objective, or to point to nearby gathering nodes.
(for those familiar with Umbra or Compass, that would be along these lines)

# Contributing

Contributions to this project are welcomed and encouraged, you can use existing GameModifications as reference on how to make your own, but here are a couple requirements:

Your GameModification must be contained inside a folder of the same name, even if your modification is just one file.

You are welcome and encouraged to use multiple CS files to implement your game modification, but if your modification is excessively large or complex it might be rejected, before working on something you suspect will turn out to be large and complex, I encourage you to reach out to me beforehand.

If you are writing functions that you suspect may be helpful for other people implementing game modifications you can add static utility classes to the Utilities folder, currently there is Addon, Agent, Assets, and Config.

If instead you are writing helpers that are more specialized you are encouraged to implment extension methods, as those will be the most intuitive to use for others.

> [!TIP]
> If you need to implement a extension method for a struct that you normally have as a pointer you can use the following format:
> 
> `public static void MethodName(this ref StructName instance)`
>
> This will allow you to use the extension method as follows:
>
> `someInstanceOfStructName->MethodName();`
