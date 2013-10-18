using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNResourceOvermanager {
        static Dictionary<String, FNResourceOvermanager> resources_managers = new Dictionary<String, FNResourceOvermanager>();

        public static FNResourceOvermanager getResourceOvermanagerForResource(String resource_name) {
            FNResourceOvermanager fnro;
            if (resources_managers.ContainsKey(resource_name)) {
                fnro = resources_managers[resource_name];
            }
            else {
                fnro = new FNResourceOvermanager(resource_name);
                resources_managers.Add(resource_name,fnro);
            }
            return fnro;
        }

        protected Dictionary<Vessel, FNResourceManager> managers;
        protected String resource_name;

        public FNResourceOvermanager(String name) {
            managers = new Dictionary<Vessel, FNResourceManager>();
            this.resource_name = name;
        }

        public bool hasManagerForVessel(Vessel vess) {
            return managers.ContainsKey(vess);
        }

        public FNResourceManager getManagerForVessel(Vessel vess) {
            return managers[vess];
        }

        public void deleteManagerForVessel(Vessel vess) {
            managers.Remove(vess);
        }

        public void deleteManager(FNResourceManager manager) {
            managers.Remove(manager.getVessel());
        }

        public FNResourceManager createManagerForVessel(PartModule pm) {
            FNResourceManager megamanager = new FNResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }
    }
}
