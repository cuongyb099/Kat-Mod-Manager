Just a quick heads up: This tool was built entirely on a 'vibe coding' flow. Since I was more focused on making it work and keeping it 100% safe rather than following strict enterprise standards, the code might look a bit messy or 'ugly' in some areas. Hope you guys can overlook the mess as long as it gets the job done and stays secure!
The tool is split into two main components: the Client (UI) and the DLL (Core Logic).

The Client: This is the user interface—the "skin" of the app. It handles what you see on the screen and sends your commands to the engine.

The DLL (KatDll): This is the "brain" or the engine. It contains the most important logic: communicating with the server, managing files, and the anti-reseller security system (licensing).

To be completely honest with you guys: My original plan was to keep the DLL code private. It contains the core logic that protects the mods and manages the licenses. As a developer, that's the "secret sauce" you usually want to keep under lock and key.

But at the end of the day, I realized that trust is more important than a piece of code. I decided to share everything anyway—the UI, the DLL, the whole thing. I want you to feel 100% safe when using this manager, even if it means my licensing logic is out in the open.

🏗 How to Build (Step-by-Step)
Because this project uses a Native AOT core, the build process requires two separate steps. Please follow this order carefully:

📋 Prerequisites
Before you start, make sure you have the following installed:

.NET 10.0 SDK (Required for the latest AOT optimizations).

Visual Studio 2022 (v17.12 or newer) / Visual Studio 2025.

Workload: ".NET Desktop Development" (Ensure you check the C++ Build Tools option, as it's required for Native AOT).

🚀 The Double-Build Process
Step 1: Build the Core (KatDll)
Navigate to the KatDll folder.

Run the Build.bat file.

This script will handle the complex Native AOT compilation for you.

Once finished, locate the newly built KatDll.dll in the output folder.

Step 2: Link & Build the Client
Copy the KatDll.dll you just built.

Paste it into the KATTHEDEV folder (the directory containing the KATTHEDEV.csproj file).

Open the solution in Visual Studio.

Build the Mod Manager Client project.

You're all set! The manager is now ready to run.
