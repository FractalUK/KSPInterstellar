Crossfeed Enabler
by NathanKell

A partmodule: adds a fuel crossfeed between the part it's added to, and the part this part is surface-attached to. Use it for radial tanks (or wings or...)

License CC-BY-SA

Includes Module Manager (by sarbian, swamp_ig, and ialdabaoth). See thread for details, license, and source: http://forum.kerbalspaceprogram.com/threads/55219

Source on GitHub at https://github.com/NathanKell/CrossFeedEnabler/

Installation: Extract folder in zip to GameData. By default includes cfg to apply to radial RCS tanks, the mini radial RCS from Realism Overhaul, and all procedural wings and tanks. (NOTE: Requires ModuleManager, which by now you really should have.)

To add to other parts:
Add:
MODULE
{
	name = ModuleCrossFeed
}
to the cfg, or do it via MM.
For example, create a MM node and add it to some cfg.

@PART[YourPartNameHere]
{
	MODULE
	{
		name = ModuleCrossFeed
	}
}

=================
Changelog:
=================
v3.3
* Recompiled for KSP 1.0 (thanks to Padishar and Angel-125)
* Toggling crossfeed applies to symmetry counterparts (thanks Angel-125)

v3.2
*Recompiled for KSP 0.90 (thanks to Padishar for fixes)

v3.1
*Recompiled for KSP 0.25
*Turns off crossfeed on Squad radial decouplers to prevent weird things from happening. The pylon, however, still has it, so you can still make droptanks (or just use a fuel line).
*Bundle Module Manager

v3.0.2
*Bugfix: No more NREs in editor
*Removed unneeded update code

v3.0.1
*Bugfix: no longer slow in VAB/SPH
*Fixed more internal logic issues.

v3.0 \/
*Allow toggling in editor and in flight
*Allow hiding of the toggle button
*Better logic internally

v2.2 \/
*Update to .24.2

v2.1 \/
*Update to .24.1

v2   \/
*Add support for Stretchies and PP tanks.
*Add support for KSPX parts (Supernovy)
*Update to 0.24

v1   \/
Initial release