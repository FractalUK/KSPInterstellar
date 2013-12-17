using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin { 
	class ComputerCore : FNResourceSuppliableModule {
		const float baseScienceRate = 0.3f;
		[KSPField(isPersistant = true)]
		public bool IsEnabled = false;
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradeCost = 100;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string computercoreType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
		public string upgradeCostStr;
		[KSPField(isPersistant = true, guiActive = true, guiName = "Name")]
		public string nameStr = "";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Science Rate")]
		public string scienceRate;
		[KSPField(isPersistant = true)]
		public bool isupgraded = false;
		[KSPField(isPersistant = false)]
		public float megajouleRate;
		[KSPField(isPersistant = false)]
		public float upgradedMegajouleRate;
		[KSPField(isPersistant = true)]
		public float electrical_power_ratio;
		[KSPField(isPersistant = true)]
		public float last_active_time;

		[KSPField(isPersistant = true)]
		public bool coreInit = false;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;

		protected float science_rate_f;
		protected bool hasrequiredupgrade = false;
        protected double science_awaiting_addition = 0;


		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitCore() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; } 

			var curReaction = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
			curReaction.PitchTorque = 5;
			curReaction.RollTorque = 5;
			curReaction.YawTorque = 5;

			ConfigNode[] namelist = ComputerCore.getNames();
			Random rands = new Random ();
			ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
			nameStr = myName.GetValue("name");

			computercoreType = upgradedName;
			isupgraded = true;
			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
		}

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }

			bool manual_upgrade = false;
			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(upgradeTechReq != null) {
					if(PluginHelper.hasTech(upgradeTechReq)) {
						hasrequiredupgrade = true;
					}else if(upgradeTechReq == "none") {
						manual_upgrade = true;
					}
				}else{
					manual_upgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}

			if (coreInit == false) {
				coreInit = true;
				if(hasrequiredupgrade) {
					isupgraded = true;
				}
			}

			if(manual_upgrade) {
				hasrequiredupgrade = true;
			}

			if (isupgraded) {
				computercoreType = upgradedName;
				if (nameStr == "") {
					ConfigNode[] namelist = ComputerCore.getNames();
					Random rands = new Random ();
					ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
					nameStr = myName.GetValue("name");
				}

				double now = Planetarium.GetUniversalTime ();
				double time_diff = now - last_active_time;
				float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
				altitude_multiplier = Math.Max (altitude_multiplier, 1);

				double science_to_add = baseScienceRate * time_diff / 86400 * electrical_power_ratio * PluginHelper.getScienceMultiplier (vessel.mainBody.flightGlobalsIndex,vessel.LandedOrSplashed) / ((float)Math.Sqrt (altitude_multiplier));
                science_awaiting_addition = science_to_add;
                
				var curReaction = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
				curReaction.PitchTorque = 5;
				curReaction.RollTorque = 5;
				curReaction.YawTorque = 5;
			} else {
				computercoreType = originalName;
			}


			this.part.force_activate();
		}

		public override void OnUpdate() {
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitCore"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitCore"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;
			Fields["nameStr"].guiActive = isupgraded;
			Fields["scienceRate"].guiActive = isupgraded;

			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
			float currentscience = 0;
			foreach (PartResource partresource in partresources) {
				currentscience += (float)partresource.amount;
			}

			float scienceratetmp = science_rate_f * 86400 ;
			scienceRate = scienceratetmp.ToString("0.000") + "/Day";

			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			}
		}

		public override void OnFixedUpdate() {

			if (!isupgraded) {
				float power_returned = consumeFNResource (megajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
			} else {
                if (ResearchAndDevelopment.Instance != null) {
                    if (!double.IsNaN(science_awaiting_addition) && !double.IsInfinity(science_awaiting_addition) && science_awaiting_addition > 0) {
                        ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)science_awaiting_addition;
                        ScreenMessages.PostScreenMessage(science_awaiting_addition.ToString("0") + " science has been added to the R&D centre.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                        science_awaiting_addition = 0;
                    }
                }

				float power_returned = consumeFNResource (upgradedMegajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime ;
				electrical_power_ratio = power_returned / upgradedMegajouleRate;
				float altitude_multiplier = (float) (vessel.altitude / (vessel.mainBody.Radius));
				altitude_multiplier = Math.Max(altitude_multiplier, 1);
				science_rate_f = baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex,vessel.LandedOrSplashed) / 86400 * power_returned/upgradedMegajouleRate / ((float)Math.Sqrt(altitude_multiplier));
				//part.RequestResource("Science", -science_rate_f * TimeWarp.fixedDeltaTime);
				if (ResearchAndDevelopment.Instance != null) {
					ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + science_rate_f * TimeWarp.fixedDeltaTime;
				}
			}
			last_active_time = (float)Planetarium.GetUniversalTime ();
		}

        public static ConfigNode[] getNames() {
            ConfigNode[] namelist = GameDatabase.Instance.GetConfigNodes("AI_CORE_NAME");
            return namelist;
        }

        public override string getResourceManagerDisplayName() {
            return computercoreType;
        }
	}
}

