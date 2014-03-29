extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_1::OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Antimatter Initiated Reactor")]
    class FNAmatCatFissionFusionReactor : FNReactor {
        protected double ticker = 0;
        protected double stored_tick = 0;
        protected GameObject lightGameObject;
        protected Light light;
        protected PartResource deuterium;
        protected PartResource he3;
        protected PartResource un;

        protected double antimatter_rate = 0;
        protected double un_rate = 0;
        protected double d_he3_rate = 0;
        protected double upgraded_d_he3_rate = 0;
        protected double upgraded_amat_rate = 0;               

        public override void OnStart(PartModule.StartState state) {
            deuterium = part.Resources["Deuterium"];
            he3 = part.Resources["Helium-3"];
            un = part.Resources["UraniumNitride"];
            base.OnStart(state);
            /*
            lightGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightGameObject.collider.enabled = false;
            lightGameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            lightGameObject.AddComponent<Light>();
            lightGameObject.renderer.material.shader = Shader.Find("Unlit/Transparent");
            lightGameObject.renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/explode2", false);
            lightGameObject.renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.9f);
            lightGameObject.renderer.enabled = false;
            light = lightGameObject.light;
            lightGameObject.transform.position = part.transform.position;
            light.type = LightType.Point;
            light.color = new Color(Color.white.r, Color.white.g, 0.87f, 1f);
            light.range = 1f;
            light.intensity = 50.0f;
            light.renderMode = LightRenderMode.ForcePixel;
             
            convert_charged_to_thermal = false;
            Destroy (lightGameObject.collider, 0.25f);
            Destroy(lightGameObject, 0.1f);
            */
            antimatter_rate = resourceRate * GameConstants.antimatter_initiated_antimatter_cons_constant*86400/1000000;
            d_he3_rate = resourceRate * GameConstants.antimatter_initiated_d_he3_cons_constant*86400;
            un_rate = resourceRate * GameConstants.antimatter_initiated_uf4_cons_constant*86400;
            upgraded_d_he3_rate = upgradedResourceRate * GameConstants.antimatter_initiated_upgraded_uf4_cons_constant;
            upgraded_amat_rate = upgradedResourceRate * GameConstants.antimatter_initiated_antimatter_cons_constant * 86400 / 1000000;


        }

        /*
        public override void OnUpdate() {
            base.OnUpdate();
            
            if (IsEnabled && total_power_ratio > 0) {
                if (lightGameObject != null & light != null) {
                    lightGameObject.transform.position = part.transform.position;
                    light.range = 0;
                    double flash_rate = Math.Max(charged_power_ratio, minimumThrottle);
                    if (ticker >= stored_tick + 0.15 / flash_rate) {
                        stored_tick = Planetarium.GetUniversalTime();
                        lightGameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                        lightGameObject.renderer.enabled = true;
                    } else if (ticker >= stored_tick + 0.1) {
                        lightGameObject.renderer.enabled = false;
                    } else {
                        float stage = (float)((ticker - stored_tick) * 10f);
                        lightGameObject.transform.localScale = new Vector3(stage, stage, stage);
                        light.range = stage * 4;
                    }
                }
            } else {
                if (lightGameObject != null & light != null) {
                    lightGameObject.renderer.enabled = false;
                }
            }

            if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                Events["ActivateReactor"].active = false;
                statusStr = "Offline: In Atmosphere.";
            }

            ticker = Planetarium.GetUniversalTime();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                IsEnabled = false;
            }
        }*/

        public override float getMinimumThermalPower() {
            return getThermalPower() * minimumThrottle;
        }

        public override string GetInfo() {
            antimatter_rate = resourceRate * GameConstants.antimatter_initiated_antimatter_cons_constant * 86400 * 1000000;
            d_he3_rate = resourceRate * GameConstants.antimatter_initiated_d_he3_cons_constant * 86400;
            un_rate = resourceRate * GameConstants.antimatter_initiated_uf4_cons_constant * 86400;
            upgraded_d_he3_rate = upgradedResourceRate * GameConstants.antimatter_initiated_upgraded_d_he3_cons_constant * 86400;
            upgraded_amat_rate = upgradedResourceRate * GameConstants.antimatter_initiated_antimatter_cons_constant * 86400 * 1000000;

            string basic = String.Format(" \n" + originalName + "\nCore Temperature: " + ReactorTemp.ToString("0") + "K\n Total Power: " + ThermalPower.ToString("0") + "MW\n D/He-3 Max Consumption Rate: " + d_he3_rate.ToString("0.00") + "Kg/day\n UN Max Consumption Rate: " + un_rate.ToString("0.00000000") + "m^3 /day\n Antimatter Max Consumption Rate:" + antimatter_rate.ToString("0.00") + "ng/day");
            string upgrade = String.Format("\n -Upgrade Information - \n" + upgradedName + "\nCore Temperature: " + upgradedReactorTemp.ToString("0") + "K\n Total Power: " + upgradedThermalPower.ToString("0") + "MW\n D/He-3 Max Consumption Rate: " + upgraded_d_he3_rate.ToString("0.00") + "Kg/day\n UF4 Max Consumption Rate: " + un_rate.ToString("0.00000000") + "m^3 /day\n Antimatter Max Consumption Rate:" + upgraded_amat_rate.ToString("0.00") + "ng/day");
            return basic + upgrade;
            //return String.Format(originalName + "\nCore Temperature: {0}K\n Total Power: {1}MW\n Tokomak Power Consumption: {6}MW\n D/He-3 Max Consumption Rate: {2}Kg/day\n -Upgrade Information-\n Upgraded Core Temperate: {3}K\n Upgraded Power: {4}MW\n Upgraded D/T Consumption: {5}Kg/day", ReactorTemp, ThermalPower, deut_rate_per_day, upgradedReactorTemp, upgradedThermalPower, up_deut_rate_per_day, powerRequirements);
        }

        protected override double consumeReactorResource(double resource) {
            double deuterium_he3_consumption = isupgraded ? resource * GameConstants.antimatter_initiated_upgraded_d_he3_cons_constant : resource * GameConstants.antimatter_initiated_d_he3_cons_constant;
            double un_consumption = isupgraded ? resource * GameConstants.antimatter_initiated_upgraded_uf4_cons_constant : resource * GameConstants.antimatter_initiated_uf4_cons_constant;
            double antimatter_consumption = GameConstants.antimatter_initiated_antimatter_cons_constant * resource;

            double delta_deut = deuterium.amount - Math.Max(0, deuterium.amount - deuterium_he3_consumption);
            double delta_he3 = he3.amount - Math.Max(0, he3.amount - deuterium_he3_consumption);
            double delta_un = un.amount - Math.Max(0, un.amount - un_consumption);
            double delta_amat = ORSHelper.fixedRequestResource(part, "Antimatter", antimatter_consumption);
            deuterium.amount = Math.Max(0, deuterium.amount - deuterium_he3_consumption);
            he3.amount = Math.Max(0, he3.amount - deuterium_he3_consumption);
            un.amount = Math.Max(0, un.amount - un_consumption);
            return resource * Math.Min(delta_deut / deuterium_he3_consumption, Math.Min(delta_he3 / deuterium_he3_consumption, Math.Min(delta_un / un_consumption,delta_amat / antimatter_consumption)));
        }

        protected override double returnReactorResource(double resource) {
            double deuterium_he3_consumption = isupgraded ? resource * GameConstants.antimatter_initiated_upgraded_d_he3_cons_constant : resource * GameConstants.antimatter_initiated_d_he3_cons_constant;
            double un_consumption = isupgraded ? resource * GameConstants.antimatter_initiated_upgraded_uf4_cons_constant : resource * GameConstants.antimatter_initiated_uf4_cons_constant;
            double antimatter_consumption = GameConstants.antimatter_initiated_antimatter_cons_constant * resource;

            double delta_deut = Math.Min(deuterium.maxAmount, deuterium.amount + deuterium_he3_consumption) - deuterium.amount;
            double delta_he3 = Math.Min(he3.maxAmount, he3.amount + deuterium_he3_consumption) - he3.amount;
            double delta_un = Math.Min(un.maxAmount, un.amount + un_consumption) - un.amount;
            double delta_amat = -ORSHelper.fixedRequestResource(part, "Antimatter", -antimatter_consumption);
            deuterium.amount = Math.Min(deuterium.maxAmount, deuterium.amount + deuterium_he3_consumption);
            he3.amount = Math.Min(he3.maxAmount, he3.amount + deuterium_he3_consumption);
            un.amount = Math.Min(un.maxAmount, un.amount + un_consumption);

            return resource;
        }
    }
}
