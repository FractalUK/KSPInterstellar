using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    abstract class FNResourceSuppliableModule :PartModule, FNResourceSuppliable{
        protected Dictionary<String,float> fnresource_supplied = new Dictionary<String, float>();

        public void supplyPower(float power, String resourcename) {
            
            //resourcename = resourcename.ToLower();
            if (fnresource_supplied.ContainsKey(resourcename)) {
                fnresource_supplied[resourcename] = power;
            }else{
                fnresource_supplied.Add(resourcename, power);
            }
        }

        public float consumePower(float power, String resourcename) {
            //print("preConsuming Resource");
            
            if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
                return 0;
            }
            if (!fnresource_supplied.ContainsKey(resourcename)) {
                fnresource_supplied.Add(resourcename, 0);
            }
            //print("Consuming Resource");
            float power_taken = Math.Min(power, fnresource_supplied[resourcename]);
            fnresource_supplied[resourcename] -= power_taken;
            FNResourceManager mega_manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
            mega_manager.powerDraw(this, power);
            return power_taken;
        }

        public float consumePower(double power, String resourcename) {
            return consumePower((float)power, resourcename);
        }

    }
}
