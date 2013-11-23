using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNResourceScanner : PartModule {
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public bool mapViewAvailable = false;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Abundance")]
        public string Ab;

        protected double abundance = 0;

        [KSPEvent(guiActive = true, guiName = "Display Hotspots", active = true)]
        public void DisplayResource() {
            FNPlanetaryResourceMapData.setDisplayedResource(resourceName);
        }

        [KSPEvent(guiActive = true, guiName = "Hide Hotspots", active = true)]
        public void HideResource() {
            FNPlanetaryResourceMapData.setDisplayedResource("");
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            Events["DisplayResource"].active = Events["DisplayResource"].guiActive = !FNPlanetaryResourceMapData.resourceIsDisplayed(resourceName) && mapViewAvailable;
            Events["DisplayResource"].guiName = "Display " + resourceName + " hotspots";
            Events["HideResource"].active = Events["HideResource"].guiActive = FNPlanetaryResourceMapData.resourceIsDisplayed(resourceName) && mapViewAvailable;
            Events["HideResource"].guiName = "Hide " + resourceName + " hotspots";
            Fields["Ab"].guiName = resourceName + " abundance";
            if (abundance > 0.001) {
                Ab = (abundance * 100.0).ToString("0.00") + "%";
            } else {
                Ab = (abundance * 1000000.0).ToString("0.0") + "ppm";
            }
            FNPlanetaryResourceMapData.updatePlanetaryResourceMap();
        }

        public override void OnFixedUpdate() {
            CelestialBody body = vessel.mainBody;
            FNPlanetaryResourcePixel res_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, resourceName, body.GetLatitude(vessel.transform.position), body.GetLongitude(vessel.transform.position));
            abundance = res_pixel.getAmount();
        }


    }
}
