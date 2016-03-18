using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem 
{
    public class ORSResourceOvermanager 
    {
        protected static Dictionary<String, ORSResourceOvermanager> resources_managers = new Dictionary<String, ORSResourceOvermanager>();

        public static ORSResourceOvermanager getResourceOvermanagerForResource(String resource_name) {
            ORSResourceOvermanager fnro;
            if (resources_managers.ContainsKey(resource_name)) {
                fnro = resources_managers[resource_name];
            }else {
                fnro = new ORSResourceOvermanager(resource_name);
                resources_managers.Add(resource_name,fnro);
            }
            return fnro;
        }

        protected Dictionary<Vessel, ORSResourceManager> managers;
        protected String resource_name;

        public ORSResourceOvermanager() {

        }

        public ORSResourceOvermanager(String name) {
            managers = new Dictionary<Vessel, ORSResourceManager>();
            this.resource_name = name;
        }

        public bool hasManagerForVessel(Vessel vess) {
            return managers.ContainsKey(vess);
        }

        public ORSResourceManager getManagerForVessel(Vessel vess) {
            return managers[vess];
        }

        public void deleteManagerForVessel(Vessel vess) {
            managers.Remove(vess);
        }

        public void deleteManager(ORSResourceManager manager) {
            managers.Remove(manager.getVessel());
        }

        public virtual ORSResourceManager createManagerForVessel(PartModule pm) 
        {
            ORSResourceManager megamanager = new ORSResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }
    }
}
