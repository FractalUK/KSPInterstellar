using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		protected float myScience = 0;
		protected float science_rate_f;

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitCore() {
			if (isupgraded || myScience < upgradeCost) { return; } // || !hasScience || myScience < upgradeCost) { return; }

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
			part.RequestResource("Science", upgradeCost);
		}

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }

			var curReaction = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
			curReaction.PitchTorque = 5;
			curReaction.RollTorque = 5;
			curReaction.YawTorque = 5;

			if (isupgraded) {
				computercoreType = upgradedName;
				if (nameStr == "") {
					ConfigNode[] namelist = ComputerCore.getNames();
					Random rands = new Random ();
					ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
					nameStr = myName.GetValue("name");
				}
			} else {
				computercoreType = originalName;
			}


			this.part.force_activate();
		}

		public override void OnUpdate() {
			Events["RetrofitCore"].active = !isupgraded && myScience >= upgradeCost;
			Fields["upgradeCostStr"].guiActive = !isupgraded;
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

			myScience = currentscience;
			upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";
		}

		public override void OnFixedUpdate() {

			if (!isupgraded) {
				float power_returned = consumePower (megajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
			} else {
				float power_returned = consumePower (upgradedMegajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime ;
				float altitude_multiplier = (float) (vessel.altitude / (vessel.mainBody.Radius));
				altitude_multiplier = Math.Max(altitude_multiplier, 1);
				science_rate_f = baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex) / 86400 * power_returned/upgradedMegajouleRate / ((float)Math.Sqrt(altitude_multiplier));
				part.RequestResource("Science", -science_rate_f * TimeWarp.fixedDeltaTime);
			}
		}

		public static string getNameFilePath() {
			return KSPUtil.ApplicationRootPath + "gamedata/warpplugin/NameList.cfg";
		}

		public static ConfigNode[] getNames() {
			ConfigNode config = ConfigNode.Load(getNameFilePath());
			ConfigNode[] namelist = config.GetNodes("NAME");
			return namelist;
		}
	}
}

