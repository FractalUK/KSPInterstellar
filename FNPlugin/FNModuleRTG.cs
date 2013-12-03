using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNModuleRTG : FNReactor {
        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Electrical Power")]
        public string currentElectricalPower;

        //Internal
        protected PartResource pu238;
        protected double electric_power_d;
        

        public override bool getIsNuclear() {
            return true;
        }

        public override bool isNeutronRich() {
            return false;
        }

        public override bool shouldScaleDownJetISP() {
            return true;
        }
            
        public override string GetInfo() {
            return "Core Temperature: " + ReactorTemp.ToString("0") +" K\n Thermal Power: " + (ThermalPower * 1000).ToString("0.0") +" KW";
        }

        public override void OnStart(PartModule.StartState state) {
            pu238 = part.Resources["Plutonium-238"];
            resourceRate = (float)(GameConstants.plutonium_238_decay_constant * pu238.maxAmount);
            base.OnStart(state);
        }

        public override void OnUpdate() {
            base.OnUpdate();
            currentElectricalPower = (electric_power_d*1000).ToString("0.00") + " W_e";
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            electric_power_d = -part.RequestResource("ElectricCharge", -powerPcnt * ThermalPower * 100*TimeWarp.fixedDeltaTime)/TimeWarp.fixedDeltaTime;
        }

        protected override double consumeReactorResource(double resource) {
            resource = GameConstants.plutonium_238_decay_constant * pu238.amount*TimeWarp.fixedDeltaTime;
            pu238.amount -= resource;
            return resource;
        }

        protected override double returnReactorResource(double resource) {
            return 0;
        }
        
        protected override string getResourceDeprivedMessage() {
            return "Pu-238 Deprived";
        }
    }
}
