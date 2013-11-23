using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class GammaRaySpectrometer : PartModule {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Uranium Abundance")]
        public string UAb;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Thorium Abundance")]
        public string TAb;
                
        protected double uranium_abundance = 0;
        protected double thorium_abundance = 0;
        protected bool texture_shown = false;
        protected long update_count = 0;
        protected long last_update = 0;
        protected bool uranium_displayed = false;
        protected bool thorium_displayed = false;

        [KSPEvent(guiActive = true, guiName = "Display Uranium Hotspots", active = true)]
        public void DisplayUranium() {
            FNPlanetaryResourceMapData.setDisplayedResource("Uranium");
            uranium_displayed = true;
            thorium_displayed = false;
        }

        [KSPEvent(guiActive = true, guiName = "Hide Uranium Hotspots", active = true)]
        public void HideUranium() {
            FNPlanetaryResourceMapData.setDisplayedResource("");
            uranium_displayed = false;
        }

        [KSPEvent(guiActive = true, guiName = "Display Thorium Hotspots", active = true)]
        public void DisplayThorium() {
            FNPlanetaryResourceMapData.setDisplayedResource("Thorium");
            uranium_displayed = false;
            thorium_displayed = true;
        }

        [KSPEvent(guiActive = true, guiName = "Hide Thorium Hotspots", active = true)]
        public void HideThorium() {
            FNPlanetaryResourceMapData.setDisplayedResource("");
            thorium_displayed = false;
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            Events["DisplayUranium"].active = Events["DisplayUranium"].guiActive = !uranium_displayed;
            Events["HideUranium"].active = Events["HideUranium"].guiActive = uranium_displayed;
            Events["DisplayThorium"].active = Events["DisplayThorium"].guiActive = !thorium_displayed;
            Events["HideThorium"].active = Events["HideThorium"].guiActive = thorium_displayed;

            if (uranium_abundance > 0.001) {
                UAb = (uranium_abundance * 100.0).ToString("0.00") + "%";
            }else {
                UAb = (uranium_abundance * 1000000.0).ToString("0.0") + "ppm";
            }
            if (thorium_abundance > 0.001) {
                TAb = (thorium_abundance * 100.0).ToString("0.00") + "%";
            }else {
                TAb = (thorium_abundance * 1000000.0).ToString("0.0") + "ppm";
            }
            FNPlanetaryResourceMapData.updatePlanetaryResourceMap();
            //FNPlanetaryResourceMapData.showPlanetaryResourceMapTexture();
        }

        public override void OnFixedUpdate() {
            CelestialBody body = vessel.mainBody;
            FNPlanetaryResourcePixel uranium_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Uranium", body.GetLatitude(vessel.transform.position), body.GetLongitude(vessel.transform.position));
            FNPlanetaryResourcePixel thorium_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", body.GetLatitude(vessel.transform.position), body.GetLongitude(vessel.transform.position));
            uranium_abundance = uranium_pixel.getAmount();
            thorium_abundance = thorium_pixel.getAmount();
            
            //if (!texture_shown) {
            //if (update_count - last_update > 20) {
            
                
                last_update = update_count;
            //}
               //texture_shown = true;
            //}
            
            update_count++;
        }
    }
}
