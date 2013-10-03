using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    abstract class FNResourceSuppliableModule :PartModule, FNResourceSuppliable{
        protected Dictionary<String,float> fnresource_supplied = new Dictionary<String, float>();
		protected Dictionary<String,FNResourceManager> fnresource_managers = new Dictionary<String,FNResourceManager> ();
		protected Dictionary<String,bool> fnresource_manager_responsibilities = new Dictionary<String,bool> ();
		protected String[] resources_to_supply;

        public void receiveFNResource(float power, String resourcename) {
            
            //resourcename = resourcename.ToLower();
            if (fnresource_supplied.ContainsKey(resourcename)) {
                fnresource_supplied[resourcename] = power;
            }else{
                fnresource_supplied.Add(resourcename, power);
            }
        }

        public float consumeFNResource(float power, String resourcename) {
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

        public float consumeFNResource(double power, String resourcename) {
            return consumeFNResource((float)power, resourcename);
        }

		public float supplyFNResource(float supply, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.powerSupply (supply);
		}

		public double supplyFNResource(double supply, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.powerSupply (supply);
		}

		public float supplyFNResourceFixedMax(float supply, float maxsupply, String resourcename) {
			return (float)supplyFNResourceFixedMax ((double)supply, (double)maxsupply, resourcename);
		}

		public double supplyFNResourceFixedMax(double supply, double maxsupply, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.powerSupplyFixedMax (supply,maxsupply);
		}

		public float supplyManagedFNResource(float supply, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.managedPowerSupply (supply);
		}

		public double supplyManagedFNResource(double supply, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.managedPowerSupply (supply);
		}

		public float supplyManagedFNResourceWithMinimum(float supply, float rat_min, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.managedPowerSupplyWithMinimum (supply,rat_min);
		}

		public double supplyManagedFNResourceWithMinimum(double supply, double rat_min, String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.managedPowerSupplyWithMinimum (supply,rat_min);
		}

		public float getCurrentResourceDemand(String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.getCurrentResourceDemand ();
		}

		public float getStableResourceSupply(String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.getStableResourceSupply ();
		}

		public float getCurrentUnfilledResourceDemand(String resourcename) {
			if (!FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
				return 0;
			}

			FNResourceManager manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
			return manager.getCurrentUnfilledResourceDemand ();
		}

		public override void OnStart(PartModule.StartState state) {
			if (state != StartState.Editor && resources_to_supply != null) { 
				foreach (String resourcename in resources_to_supply) {
					FNResourceManager manager;

					if (FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
						manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
						if (manager == null) {
							manager = FNResourceOvermanager.getResourceOvermanagerForResource (resourcename).getManagerForVessel (vessel);
							print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
						}
					}else {
						manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).createManagerForVessel(this);

						print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
					}

				}
			}
		}

		public override void OnFixedUpdate() {
			if (resources_to_supply != null) {

				foreach (String resourcename in resources_to_supply) {
					FNResourceManager manager;

					if (!FNResourceOvermanager.getResourceOvermanagerForResource (resourcename).hasManagerForVessel (vessel)) {
						manager = FNResourceOvermanager.getResourceOvermanagerForResource (resourcename).createManagerForVessel (this);

						print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");


					} else {
						manager = FNResourceOvermanager.getResourceOvermanagerForResource (resourcename).getManagerForVessel (vessel);
						if (manager == null) {
							manager = FNResourceOvermanager.getResourceOvermanagerForResource (resourcename).getManagerForVessel (vessel);
							print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + resourcename + ")");
						}
					}

					if (manager.getPartModule ().vessel != this.vessel) {
						manager.updatePartModule (this);
					}

					if (manager.getPartModule () == this) {
						manager.update ();
					}
				}
			}

		}


    }
}
