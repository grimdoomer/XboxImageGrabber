# XboxImageGrabber
Research tool to create images showing the memory profile of your Xbox console. The primary purpose of this tool was to help me "view" the memory layout of my Xbox console while developing the Halo 2 HD patch. May be useful to others for research in other areas...

## Requirements
This tool requires your Xbox console is running some form of "debug" BIOS and xbdm must be running on the console for it to connect. Can be used on a console with 64MB or 128MB of RAM.

## How to run
The tool has two actions it can perform, memory snapshot and backbuffer snapshot:
- If you run the exe with the "-memory" argument it will create a memory profile snapshot. If Halo 2 is detected as running the memory snapshot will contain additional info specific to Halo 2. If Halo 2 is not running you can still take a memory snapshot you just won't get any extra info on what memory is being used for.
- If you run the exe with no additional arguments it will save a snapshot of the GPU backbuffer. Useful if you want to take screenshots while running the Halo 2 HD patch as the modifications for using the upper 64MB of RAM will break the xbdm screenshot function. This only works if Halo 2 is the running title, for any other xbe it will fail with an error.

## Supported versions of Halo 2
Currently this tool only supports the v1.0 version of the Halo 2 xbe (the xbe found on the game disc). It doesn't support any of the title update xbe files.

## Compiling
The project is a C# .Net Core project and requires [ViridiX](https://github.com/Ernegien/ViridiX) library.
