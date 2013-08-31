using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    
    class ElectricEngineController : PartModule {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = ":";
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = true)]
        private int fuel_mode = 0;
        protected float total_power_output = 0;
        protected float reference_power = 8000;
        protected float initial_thrust = 0;
        protected float initial_isp = 0;
        protected int eval_counter = 0;
        protected float myScience = 0;
        protected ConfigNode upgrade_resource;
        protected float ispMultiplier = 1;
        protected ConfigNode[] propellants;
        protected VInfoBox fuel_gauge;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
        public string fuelmode;

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
        public void TogglePropellant() {

            fuel_mode++;
            if (fuel_mode >= propellants.Length) {
                fuel_mode = 0;
            }


            evaluateMaxThrust();

        }
        
        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
            if (isupgraded || myScience < upgradeCost) { return; } // || !hasScience || myScience < upgradeCost) { return; }
            isupgraded = true;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                ModuleEngines.Propellant prop = new ModuleEngines.Propellant();
                //prop.id = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id;
                //ConfigNode prop_node = new ConfigNode();
                //PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").Save(prop_node);

                PartResource part_resource = part.Resources.list[0];
                part_resource.info = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma");
                part_resource.maxAmount = 10;
                part_resource.amount = 10;

                propellants = ElectricEngineController.getPropellants(isupgraded);
                fuel_mode = 0;

                //curEngine.propellants[1].id = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id;
                //curEngine.propellants[1].name = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").name;
                engineType = upgradedName;

                evaluateMaxThrust();
            }
            
        }

        public override void OnLoad(ConfigNode node) {
                        
        }
        
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            //this.part.force_activate();

            fuel_gauge = part.stackIcon.DisplayInfo();
            propellants = getPropellants(isupgraded);

            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                initial_thrust = curEngine.maxThrust;
                initial_isp = curEngine.atmosphereCurve.Evaluate(0);
            }

            engineType = originalName;
            if (isupgraded) {
                foreach (PartResource part_resource in part.Resources.list) {
                    if (part_resource.resourceName == "XenonGas") {
                        part_resource.maxAmount = 0;
                        part_resource.amount = 0;
                    }
                    
                }
                engineType = upgradedName;
                curEngine.propellants[1].id = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id;
                curEngine.propellants[1].name = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").name;
                                
            }

            evaluateMaxThrust();
            
        }

        public override void OnUpdate() {
            Events["RetrofitEngine"].active = !isupgraded && myScience >= upgradeCost;
            Fields["upgradeCostStr"].guiActive = !isupgraded;

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            float currentscience = 0;
            foreach (PartResource partresource in partresources) {
                currentscience += (float)partresource.amount;
            }
            myScience = currentscience;

            upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";

            ModuleEngines curEngineT = (ModuleEngines)this.part.Modules["ModuleEngines"];
            if (curEngineT.isOperational && !IsEnabled) {
                IsEnabled = true;
                part.force_activate();
            }

            float currentpropellant = 0;
            float maxpropellant = 0;

            partresources = new List<PartResource>();
            part.GetConnectedResources(curEngineT.propellants[0].id, partresources);

            foreach (PartResource partresource in partresources) {
                currentpropellant += (float)partresource.amount;
                maxpropellant += (float)partresource.maxAmount;
            }

            if (curEngineT.isOperational) {
                if (!fuel_gauge.infoBoxRef.expanded) {
                    fuel_gauge.infoBoxRef.Expand();
                }
                fuel_gauge.length = 2;
                if (maxpropellant > 0) {
                    fuel_gauge.SetValue(currentpropellant / maxpropellant);
                }
                else {
                    fuel_gauge.SetValue(0);
                }
            }
            else {
                if (!fuel_gauge.infoBoxRef.collapsed) {
                    fuel_gauge.infoBoxRef.Collapse();
                }
            }
        }

        public override void OnFixedUpdate() {

            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine.maxThrust <= 0) {
                evaluateMaxThrust();
                if (curEngine.maxThrust <= 0) {
                    curEngine.maxThrust = initial_thrust;
                }
            }
            

            if (isupgraded) {
                part.RequestResource("VacuumPlasma", -10);
            }
        }

        public void evaluateMaxThrust() {
            List<Part> vessel_parts = vessel.parts;
            total_power_output = 0;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            ConfigNode chosenpropellant = propellants[fuel_mode];
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();
            //bool propellant_is_upgrade = false;

            for (int i = 0; i < assprops.Length; ++i) {
                fuelmode = chosenpropellant.GetValue("guiName");
                ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
                //propellant_is_upgrade = bool.Parse(chosenpropellant.GetValue("isUpgraded"));
                
                ModuleEngines.Propellant curprop = new ModuleEngines.Propellant();
                curprop.Load(assprops[i]);
                if (curprop.drawStackGauge) {
                    curprop.drawStackGauge = false;
                    fuel_gauge.SetMessage(curprop.name);
                    fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                    fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                    fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetValue(0f);
                }
                list_of_propellants.Add(curprop);
            }

            int engines = 0;
            foreach (Part vessel_part in vessel_parts) {
                foreach (PartModule vessel_part_module in vessel_part.Modules) {
                    var curEngine2 = vessel_part_module as ElectricEngineController;
                    if (curEngine2 != null) {
                        var curEngine3 = curEngine2.part.Modules["ModuleEngines"] as ModuleEngines;
                        if (curEngine3.isOperational) {
                            engines++;
                        }
                    }
                }

            }

            if (engines <= 0) {
                engines = 1;
            }

            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel)) {
                FNResourceManager megamanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).getManagerForVessel(vessel);
                total_power_output = megamanager.getStableResourceSupply()/engines;
            }else {
                total_power_output = 0;
            }
            
            
            float thrust_ratio = total_power_output / reference_power;
            curEngine.maxThrust = initial_thrust * thrust_ratio/ispMultiplier;
            FloatCurve newISP = new FloatCurve();
            newISP.Add(0, initial_isp * ispMultiplier);
            curEngine.atmosphereCurve = newISP;
            

            if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {
                curEngine.propellants.Clear();
                curEngine.propellants = list_of_propellants;
                curEngine.SetupPropellant();
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(curEngine.propellants[0].id, partresources);

            //if(!isupgraded) {
            if (partresources.Count == 0 && fuel_mode != 0) {
                TogglePropellant();
            }
            //}else{
            //    if(!propellant_is_upgrade) {
                    //TogglePropellant();
           //     }
            //}
            
        }

        public static string getPropellantFilePath(bool isupgraded) {
            if (isupgraded) {
                return KSPUtil.ApplicationRootPath + "gamedata/warpplugin/AdvElectricEnginePropellants.cfg";
            }else {
                return KSPUtil.ApplicationRootPath + "gamedata/warpplugin/ElectricEnginePropellants.cfg";
            }
        }

        public static ConfigNode[] getPropellants(bool isupgraded) {
            ConfigNode config = ConfigNode.Load(getPropellantFilePath(isupgraded));
            ConfigNode[] propellantlist = config.GetNodes("PROPELLANTS");
            return propellantlist;
        }
    }
}
