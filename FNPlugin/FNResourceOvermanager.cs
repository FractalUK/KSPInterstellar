extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;
using UnityEngine;

namespace FNPlugin {
    public class FNResourceOvermanager : ORSResourceOvermanager {

        public static new FNResourceOvermanager getResourceOvermanagerForResource(String resource_name) {
            FNResourceOvermanager fnro;
            //Debug.Log("getResourceOvermanager");
            if (ORSResourceOvermanager.resources_managers.ContainsKey(resource_name)) {
                fnro = (FNResourceOvermanager) ORSResourceOvermanager.resources_managers[resource_name];
            } else {
                fnro = new FNResourceOvermanager(resource_name);
                ORSResourceOvermanager.resources_managers.Add(resource_name, fnro);
            }
            return fnro;
        }

        public FNResourceOvermanager(String name) {
            managers = new Dictionary<Vessel, ORSResourceManager>();
            this.resource_name = name;
        }

        public override ORSResourceManager createManagerForVessel(PartModule pm) {
            FNResourceManager megamanager = new FNResourceManager(pm, resource_name);
            managers.Add(pm.vessel, megamanager);
            return megamanager;
        }

    }
}
