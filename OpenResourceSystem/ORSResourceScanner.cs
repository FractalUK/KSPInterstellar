using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace OpenResourceSystem {
    public class ORSResourceScanner : PartModule {
        [KSPField(isPersistant = false)]
        public string resourceName = "";
        [KSPField(isPersistant = false)]
        public bool mapViewAvailable = false;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Abundance")]
        public string Ab;

        protected double abundance = 0;

        [KSPEvent(guiActive = true, guiName = "Display Hotspots", active = true)]
        public void DisplayResource() {
            ORSPlanetaryResourceMapData.setDisplayedResource(resourceName);
        }

        [KSPEvent(guiActive = true, guiName = "Hide Hotspots", active = true)]
        public void HideResource() {
            ORSPlanetaryResourceMapData.setDisplayedResource("");
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            Events["DisplayResource"].active = Events["DisplayResource"].guiActive = !ORSPlanetaryResourceMapData.resourceIsDisplayed(resourceName) && mapViewAvailable;
            Events["DisplayResource"].guiName = "Display " + resourceName + " hotspots";
            Events["HideResource"].active = Events["HideResource"].guiActive = ORSPlanetaryResourceMapData.resourceIsDisplayed(resourceName) && mapViewAvailable;
            Events["HideResource"].guiName = "Hide " + resourceName + " hotspots";
            Fields["Ab"].guiName = resourceName + " abundance";
            if (abundance > 0.001) {
                Ab = (abundance * 100.0).ToString("0.00") + "%";
            } else {
                Ab = (abundance * 1000000.0).ToString("0.0") + "ppm";
            }
            ORSPlanetaryResourceMapData.updatePlanetaryResourceMap();
        }

        public override void OnFixedUpdate() {
            CelestialBody body = vessel.mainBody;
            ORSPlanetaryResourcePixel res_pixel = ORSPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, resourceName, body.GetLatitude(vessel.transform.position), body.GetLongitude(vessel.transform.position));
            abundance = res_pixel.getAmount();
        }


    }
}
