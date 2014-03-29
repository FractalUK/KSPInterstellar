KSPInterstellar
===============

KSP Instellar Mod for Kerbal Space Program

This version of interstellar is modified and published by me, wavefunctionp. It is not published by FractalUK, the creator of Interstellar. Do not blame him for broke things with this version, instead contact me on the interstellar forum thread, forum pm or email or twitter: @wavefunctionp.

Instructions:

For players: Download the compiled addon here: https://www.dropbox.com/sh/3im5o8t4ahxnyx7/oO9MQ45um7/KSPInterstellar

For developers:

The zip should work out pretty much out of the box. You may need to update the unity and c# library references for the FNPlugin and OpenResourceSystem solutions. And maybe your output paths. All assets for the plugin are included as linked references from the addon folders in the project, so they shouldn't copy. You may want to copy my gitignore to your repo as well.

I have no idea how it will work when pulling from my repo. I had to do a lot of house keeping to get a clean and workable repo with visual studio.

Changes from fractaluk/interstellar as they stand today (3/28/14):

- Fixed data collection on magnetometer
- Replaced non-functional model for .625m arcjet thruster to rescaled 1.25m model (temp fix, fractaluk has a fixed version for the next big update)
- Improved reactor, radiator, and generator tooltips
- Added water and lithium resource maps to kerbin
- Added (currently unused) water and lithium resource assets
- Added atmospheric intake functionality to atmospheric scoops
- Added full initial electric charge to generators to aid in fusion reactor activation
- Charging disabled by default on the alcubierre drives
- Added electric charge to computer core, and increased torque to 5/5/5
- Added a more detailed tooltip description for the computer core
- Added tooltip details about power transmission to array and reciever descriptions
- Added tooltip details about generator attachment and modes
- Added note to GC/MS tooltip to indicate that it is also a science experiment.
- Added note that GRS is useful for detecting concentrations of uranium and thorium.
- Added a detailed description of the science labs capabilities
- Added a clarification of the crygenic helium tank, and its use with the IR telescope
- Added note that the magnetometer is also a science experiment
- Added note to clarify that the he-3 does not store helium, and is used as a reator fuel.
- Added tooltip to antimatter containers to indicate maximum capacity.
- Remove old hex can part files that were causing loading errors.
- Removed old methane tank part file that was causing loading errors.
- The large xenon tank now actually contains xenon.
- The UN tank now uses the correct model.
- Charging disabled by default for antimatter containers.
