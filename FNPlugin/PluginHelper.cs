﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour
    {
        public const double FIXED_SAT_ALTITUDE = 13599840256;
        public const int REF_BODY_KERBOL = 0;
        public const int REF_BODY_KERBIN = 1;
        public const int REF_BODY_MUN = 2;
        public const int REF_BODY_MINMUS = 3;
        public const int REF_BODY_MOHO = 4;
        public const int REF_BODY_EVE = 5;
        public const int REF_BODY_DUNA = 6;
        public const int REF_BODY_IKE = 7;
        public const int REF_BODY_JOOL = 8;
        public const int REF_BODY_LAYTHE = 9;
        public const int REF_BODY_VALL = 10;
        public const int REF_BODY_BOP = 11;
        public const int REF_BODY_TYLO = 12;
        public const int REF_BODY_GILLY = 13;
        public const int REF_BODY_POL = 14;
        public const int REF_BODY_DRES = 15;
        public const int REF_BODY_EELOO = 16;

        public static bool using_toolbar = false;

        public const int interstellar_major_version = 13;
        public const int interstellar_minor_version = 5;

        protected static bool plugin_init = false;
        protected static bool is_thermal_dissip_disabled = false;
        protected static bool is_panel_heating_clamped = false;
        protected static bool is_reciever_temp_tweaked = false;
        protected static GameDatabase gdb;
        protected static bool resources_configured = false;

        public static bool TechnologyIsInUse { get { return (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX); } }

        public static ConfigNode PluginSettingsConfig { get { return GameDatabase.Instance.GetConfigNode("WarpPlugin/WarpPluginSettings/WarpPluginSettings"); } }

        public static string getPluginSaveFilePath()
        {
            return KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg";
        }

        public static string getPluginSettingsFilePath()
        {
            return KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/WarpPluginSettings.cfg";
        }

        public static bool isThermalDissipationDisabled()
        {
            return is_thermal_dissip_disabled;
        }

        public static bool isSolarPanelHeatingClamped()
        {
            return is_panel_heating_clamped;
        }

        public static bool isRecieverCoreTempTweaked()
        {
            return is_reciever_temp_tweaked;
        }

        public static bool hasTech(string techid)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");
                foreach (ConfigNode scenario in scenarios)
                {
                    if (scenario.GetValue("name") == "ResearchAndDevelopment")
                    {
                        ConfigNode[] techs = scenario.GetNodes("Tech");
                        foreach (ConfigNode technode in techs)
                        {
                            if (technode.GetValue("id") == techid)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool upgradeAvailable(string techid)
        {
            if (HighLogic.CurrentGame != null)
            {
                if (PluginHelper.TechnologyIsInUse)
                {
                    if (techid != null && PluginHelper.hasTech(techid))
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public static float getKerbalRadiationDose(int kerbalidx)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                if (sought_kerbal.HasValue("totalDose"))
                {
                    float dose = float.Parse(sought_kerbal.GetValue("totalDose"));
                    return dose;
                }
                return 0.0f;
            }
            catch (Exception ex)
            {
                print(ex);
                return 0.0f;
            }
        }

        public static ConfigNode getKerbal(int kerbalidx)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                return sought_kerbal;
            }
            catch (Exception ex)
            {
                print(ex);
                return null;
            }
        }

        public static void saveKerbalRadiationdose(int kerbalidx, float rad)
        {
            try
            {
                string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
                ConfigNode config = ConfigNode.Load(persistentfile);
                ConfigNode gameconf = config.GetNode("GAME");
                ConfigNode crew_roster = gameconf.GetNode("ROSTER");
                ConfigNode[] crew = crew_roster.GetNodes("CREW");
                ConfigNode sought_kerbal = crew[kerbalidx];
                if (sought_kerbal.HasValue("totalDose"))
                {
                    sought_kerbal.SetValue("totalDose", rad.ToString("E"));
                }
                else
                {
                    sought_kerbal.AddValue("totalDose", rad.ToString("E"));
                }
                config.Save(persistentfile);
            }
            catch (Exception ex)
            {
                print(ex);
            }
        }

        public static ConfigNode getPluginSaveFile()
        {
            ConfigNode config = ConfigNode.Load(PluginHelper.getPluginSaveFilePath());
            if (config == null)
            {
                config = new ConfigNode();
                config.AddValue("writtenat", DateTime.Now.ToString());
                config.Save(PluginHelper.getPluginSaveFilePath());
            }
            return config;
        }

        public static ConfigNode getPluginSettingsFile()
        {
            ConfigNode config = ConfigNode.Load(PluginHelper.getPluginSettingsFilePath());
            if (config == null)
            {
                config = new ConfigNode();
            }
            return config;
        }

        public static bool lineOfSightToSun(Vessel vess)
        {
            Vector3d a = PluginHelper.getVesselPos(vess);
            Vector3d b = FlightGlobals.Bodies[0].transform.position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                if (referenceBody.flightGlobalsIndex == 0)
                { // the sun should not block line of sight to the sun
                    continue;
                }
                Vector3d refminusa = referenceBody.position - a;
                Vector3d bminusa = b - a;
                if (Vector3d.Dot(refminusa, bminusa) > 0)
                {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude)
                    {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static Vector3d getVesselPos(Vessel v)
        {
            Vector3d v1p = (v.state == Vessel.State.ACTIVE) ? (Vector3d)v.transform.position : v.GetWorldPos3D();
            return v1p;
        }

        public static float getMaxAtmosphericAltitude(CelestialBody body)
        {
            if (!body.atmosphere)
            {
                return 0;
            }
            return (float)-body.atmosphereScaleHeight * 1000.0f * Mathf.Log(1e-6f);
        }

        public static float getScienceMultiplier(int refbody, bool landed)
        {
            float multiplier = 1;

            if (refbody == REF_BODY_DUNA || refbody == REF_BODY_EVE || refbody == REF_BODY_IKE || refbody == REF_BODY_GILLY)
            {
                multiplier = 5f;
            }
            else if (refbody == REF_BODY_MUN || refbody == REF_BODY_MINMUS)
            {
                multiplier = 2.5f;
            }
            else if (refbody == REF_BODY_JOOL || refbody == REF_BODY_TYLO || refbody == REF_BODY_POL || refbody == REF_BODY_BOP)
            {
                multiplier = 10f;
            }
            else if (refbody == REF_BODY_LAYTHE || refbody == REF_BODY_VALL)
            {
                multiplier = 12f;
            }
            else if (refbody == REF_BODY_EELOO || refbody == REF_BODY_MOHO)
            {
                multiplier = 20f;
            }
            else if (refbody == REF_BODY_DRES)
            {
                multiplier = 7.5f;
            }
            else if (refbody == REF_BODY_KERBIN)
            {
                multiplier = 1f;
            }
            else if (refbody == REF_BODY_KERBOL)
            {
                multiplier = 15f;
            }
            else
            {
                multiplier = 0f;
            }

            if (landed)
            {
                if (refbody == REF_BODY_TYLO)
                {
                    multiplier = multiplier * 3f;
                }
                else if (refbody == REF_BODY_EVE)
                {
                    multiplier = multiplier * 2.5f;
                }
                else
                {
                    multiplier = multiplier * 2f;
                }
            }

            return multiplier;
        }

        public static float getImpactorScienceMultiplier(int refbody)
        {
            float multiplier = 1;

            if (refbody == REF_BODY_DUNA || refbody == REF_BODY_EVE || refbody == REF_BODY_IKE || refbody == REF_BODY_GILLY)
            {
                multiplier = 7f;
            }
            else if (refbody == REF_BODY_MUN || refbody == REF_BODY_MINMUS)
            {
                multiplier = 5f;
            }
            else if (refbody == REF_BODY_JOOL || refbody == REF_BODY_TYLO || refbody == REF_BODY_POL || refbody == REF_BODY_BOP)
            {
                multiplier = 9f;
            }
            else if (refbody == REF_BODY_LAYTHE || refbody == REF_BODY_VALL)
            {
                multiplier = 11f;
            }
            else if (refbody == REF_BODY_EELOO || refbody == REF_BODY_MOHO)
            {
                multiplier = 14f;
            }
            else if (refbody == REF_BODY_DRES)
            {
                multiplier = 8f;
            }
            else if (refbody == REF_BODY_KERBIN)
            {
                multiplier = 0.5f;
            }
            else
            {
                multiplier = 0f;
            }
            return multiplier;
        }

        public static string getFormattedPowerString(double power)
        {
            if (power > 1000)
            {
                if (power > 20000)
                {
                    return (power / 1000).ToString("0") + " GW";
                }
                else
                {
                    return (power / 1000).ToString("0.0") + " GW";
                }
            }
            else
            {
                if (power > 20)
                {
                    return power.ToString("0") + " MW";
                }
                else
                {
                    if (power > 1)
                    {
                        return power.ToString("0.0") + " MW";
                    }
                    else
                    {
                        return (power * 1000).ToString("0.0") + " KW";
                    }
                }
            }
        }

        public void Update()
        {
            this.enabled = true;
            AvailablePart intakePart = PartLoader.getPartInfoByName("CircularIntake");
            if (intakePart != null)
            {
                if (intakePart.partPrefab.FindModulesImplementing<AtmosphericIntake>().Count <= 0 && PartLoader.Instance.IsReady())
                {
                    plugin_init = false;
                }
            }

            if (!resources_configured)
            {
                ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode("WarpPlugin/WarpPluginSettings/WarpPluginSettings");
                if (plugin_settings != null)
                {
                    if (plugin_settings.HasValue("ThermalMechanicsDisabled"))
                    {
                        PluginHelper.is_thermal_dissip_disabled = bool.Parse(plugin_settings.GetValue("ThermalMechanicsDisabled"));
                        Debug.Log("[KSP Interstellar] ThermalMechanics set to : " + 
                            (!PluginHelper.is_thermal_dissip_disabled).ToString());
                    }
                    if (plugin_settings.HasValue("SolarPanelClampedHeating"))
                    {
                        PluginHelper.is_panel_heating_clamped = bool.Parse(plugin_settings.GetValue("SolarPanelClampedHeating"));
                        Debug.Log("[KSP Interstellar] Solar panels clamped heating set to enabled: " + 
                                    PluginHelper.is_panel_heating_clamped.ToString());
                    }
                    if (plugin_settings.HasValue("RecieverTempTweak"))
                    {
                        PluginHelper.is_reciever_temp_tweaked = bool.Parse(plugin_settings.GetValue("RecieverTempTweak"));
                        Debug.Log("[KSP Interstellar] Microwave reciever CoreTemp tweak is set to enabled: " +
                                    PluginHelper.is_reciever_temp_tweaked.ToString());
                    }
                    resources_configured = true;
                }
                else
                {
                    showInstallationErrorMessage();
                }

            }



            if (!plugin_init)
            {
                gdb = GameDatabase.Instance;
                plugin_init = true;

                AvailablePart kerbalRadiationPart = PartLoader.getPartInfoByName("kerbalEVA");
                if (kerbalRadiationPart.partPrefab.Modules != null)
                {
                    if (kerbalRadiationPart.partPrefab.FindModulesImplementing<FNModuleRadiation>().Count == 0)
                    {
                        kerbalRadiationPart.partPrefab.gameObject.AddComponent<FNModuleRadiation>();
                    }
                }
                else
                {
                    kerbalRadiationPart.partPrefab.gameObject.AddComponent<FNModuleRadiation>();
                }

                List<AvailablePart> available_parts = PartLoader.LoadedPartsList;
                foreach (AvailablePart available_part in available_parts)
                {
                    Part prefab_available_part = available_part.partPrefab;
                    try
                    {
                        if (prefab_available_part.Modules != null)
                        {

                            if (prefab_available_part.FindModulesImplementing<ModuleResourceIntake>().Count > 0)
                            {
                                ModuleResourceIntake intake = prefab_available_part.Modules["ModuleResourceIntake"] as ModuleResourceIntake;
                                if (intake.resourceName == "IntakeAir")
                                {
                                    var pm = prefab_available_part.gameObject.AddComponent<AtmosphericIntake>();
                                    prefab_available_part.Modules.Add(pm);
                                    pm.area = intake.area * intake.unitScalar * intake.maxIntakeSpeed / 20;

                                    PartResource intake_air_resource = prefab_available_part.Resources["IntakeAir"];

                                    if (intake_air_resource != null && !prefab_available_part.Resources.Contains(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere))
                                    {
                                        ConfigNode node = new ConfigNode("RESOURCE");
                                        node.AddValue("name", InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);
                                        node.AddValue("maxAmount", intake_air_resource.maxAmount);
                                        node.AddValue("amount", intake_air_resource.amount);
                                        prefab_available_part.AddResource(node);
                                    }
                                }

                            }

                            if (prefab_available_part.FindModulesImplementing<ModuleDeployableSolarPanel>().Count > 0)
                            {
                                ModuleDeployableSolarPanel panel = prefab_available_part.Modules["ModuleDeployableSolarPanel"] as ModuleDeployableSolarPanel;
                                if (panel.chargeRate > 0)
                                {
                                    Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FNSolarPanelWasteHeatModule");
                                    FNSolarPanelWasteHeatModule pm = null;
                                    if (type != null)
                                    {
                                        pm = prefab_available_part.gameObject.AddComponent(type) as FNSolarPanelWasteHeatModule;
                                        prefab_available_part.Modules.Add(pm);
                                    }
                                }


                                if (!prefab_available_part.Resources.Contains("WasteHeat") && panel.chargeRate > 0)
                                {
                                    ConfigNode node = new ConfigNode("RESOURCE");
                                    node.AddValue("name", "WasteHeat");
                                    node.AddValue("maxAmount", panel.chargeRate * 100);
                                    node.AddValue("amount", 0);
                                    PartResource pr = prefab_available_part.AddResource(node);

                                    if (available_part.resourceInfo != null && pr != null)
                                    {
                                        if (available_part.resourceInfo.Length == 0)
                                        {
                                            available_part.resourceInfo = pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                                        }
                                        else
                                        {
                                            available_part.resourceInfo = available_part.resourceInfo + "\n" + pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                                        }
                                    }
                                }

                            }

                            if (prefab_available_part.FindModulesImplementing<ElectricEngineControllerFX>().Count() > 0)
                            {
                                available_part.moduleInfo = prefab_available_part.FindModulesImplementing<ElectricEngineControllerFX>().First().GetInfo();
                                available_part.moduleInfos.RemoveAll(modi => modi.moduleName == "Engine");
                                AvailablePart.ModuleInfo mod_info = available_part.moduleInfos.Where(modi => modi.moduleName == "Electric Engine Controller").First();
                                mod_info.moduleName = "Electric Engine";
                            }

                            if (prefab_available_part.FindModulesImplementing<FNNozzleController>().Count() > 0)
                            {
                                available_part.moduleInfo = prefab_available_part.FindModulesImplementing<FNNozzleController>().First().GetInfo();
                                available_part.moduleInfos.RemoveAll(modi => modi.moduleName == "Engine");
                                AvailablePart.ModuleInfo mod_info = available_part.moduleInfos.Where(modi => modi.moduleName == "FNNozzle Controller").First();
                                mod_info.moduleName = "Thermal Nozzle";
                            }

                            if (prefab_available_part.CrewCapacity > 0 || prefab_available_part.FindModulesImplementing<ModuleCommand>().Count > 0)
                            {
                                Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FNModuleRadiation");
                                FNModuleRadiation pm = null;
                                if (type != null)
                                {
                                    pm = prefab_available_part.gameObject.AddComponent(type) as FNModuleRadiation;
                                    prefab_available_part.Modules.Add(pm);
                                    double rad_hardness = prefab_available_part.mass / (Math.Max(prefab_available_part.CrewCapacity, 0.1)) * 7.5;
                                    pm.rad_hardness = rad_hardness;
                                    AvailablePart.ModuleInfo minfo = new AvailablePart.ModuleInfo();
                                    minfo.moduleName = "Radiation Status";
                                    minfo.info = pm.GetInfo();
                                    available_part.moduleInfos.Add(minfo);
                                }
                                print("Adding ModuleRadiation to " + prefab_available_part.name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (prefab_available_part != null)
                        {
                            print("[KSP Interstellar] Exception caught adding to: " + prefab_available_part.name + " part: " + ex.ToString());
                        }
                        else
                        {
                            print("[KSP Interstellar] Exception caught adding to unknown module");
                        }
                    }


                }
            }

            //Destroy (this);
        }

        protected static bool warning_displayed = false;

        public static void showInstallationErrorMessage()
        {
            if (!warning_displayed)
            {
                PopupDialog.SpawnPopupDialog("KSP Interstellar Installation Error", "KSP Interstellar is unable to detect files required for proper functioning.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin.", "OK", false, HighLogic.Skin);
                warning_displayed = true;
            }
        }




    }
}
