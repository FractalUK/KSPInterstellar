using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	class FNGenerator : FNResourceSuppliableModule{
		// Persistent True
		[KSPField(isPersistant = true)]
		public bool IsEnabled = true;
		[KSPField(isPersistant = true)]
		public bool generatorInit = false;
		[KSPField(isPersistant = true)]
		public bool isupgraded = false;

		// Persistent False
		[KSPField(isPersistant = false)]
		public float pCarnotEff;
		[KSPField(isPersistant = false)]
		public float maxThermalPower;
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradedpCarnotEff;
		[KSPField(isPersistant = false)]
		public string animName;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;
		[KSPField(isPersistant = false)]
		public float upgradeCost;

		// GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string generatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Current Power")]
		public string OutputPower;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Max Power")]
		public string MaxPowerStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
		public string OverallEfficiency;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade Cost")]
		public string upgradeCostStr;

		// Internal
		protected float coldBathTemp = 500;
		protected float hotBathTemp = 1;
		protected float outputPower;
		protected float totalEff;
		protected float sectracker = 0;
		protected bool play_down = true;
		protected bool play_up = true;
		protected FNThermalSource myAttachedReactor;
		protected bool hasrequiredupgrade = false;
		protected int last_draw_update = 0;
		protected int shutdown_counter = 0;
		protected Animation anim;
		protected bool hasstarted = false;

		[KSPEvent(guiActive = true, guiName = "Activate Generator", active = true)]
		public void ActivateGenerator() {
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Deactivate Generator", active = false)]
		public void DeactivateGenerator() {
			IsEnabled = false;
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitGenerator() {
			if (ResearchAndDevelopment.Instance == null) { return;}
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
			upgradePartModule ();
			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
		}

		[KSPAction("Activate Generator")]
		public void ActivateGeneratorAction(KSPActionParam param) {
			ActivateGenerator();
		}

		[KSPAction("Deactivate Generator")]
		public void DeactivateGeneratorAction(KSPActionParam param) {
			DeactivateGenerator();
		}

		[KSPAction("Toggle Generator")]
		public void ToggleGeneratorAction(KSPActionParam param) {
			IsEnabled = !IsEnabled;
		}

		public void upgradePartModule () {
			isupgraded = true;
			pCarnotEff = upgradedpCarnotEff;
			generatorType = upgradedName;
		}

		public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;
			base.OnStart (state);

			if (state == StartState.Editor) { return; }
			this.part.force_activate();
			generatorType = originalName;

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) {
				anim [animName].layer = 1;
				if (!IsEnabled) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}

			bool manual_upgrade = false;
			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(upgradeTechReq != null) {
					if(PluginHelper.hasTech(upgradeTechReq)) {
						hasrequiredupgrade = true;
					}else if(upgradeTechReq == "none") {
						manual_upgrade = true;
						hasrequiredupgrade = true;
					}
				}else{
					manual_upgrade = true;
					hasrequiredupgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}

			if (generatorInit == false) {
				generatorInit = true;
				IsEnabled = true;
				if(hasrequiredupgrade && !manual_upgrade) {
					isupgraded = true;
				}
			}

			if (isupgraded) {
				upgradePartModule ();
			}

			foreach (AttachNode attach_node in part.attachNodes) {
				List<FNThermalSource> sources = attach_node.attachedPart.FindModulesImplementing<FNThermalSource> ();
				if (sources.Count > 0) {
					myAttachedReactor = sources.First ();
					if (myAttachedReactor != null) {
						break;
					}
				}
			}

			print("[WarpPlugin] Configuring Generator");
		}

		public override void OnUpdate() {
			Events["ActivateGenerator"].active = !IsEnabled;
			Events["DeactivateGenerator"].active = IsEnabled;
			Fields["OverallEfficiency"].guiActive = IsEnabled;
			Fields["MaxPowerStr"].guiActive = IsEnabled;
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitGenerator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
				upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
			} else {
				Events ["RetrofitGenerator"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded  && hasrequiredupgrade;

			if (IsEnabled) {
				if (play_up && anim != null) {
					play_down = true;
					play_up = false;
					anim [animName].speed = 1f;
					anim [animName].normalizedTime = 0f;
					anim.Blend (animName, 2f);
				}
			} else {
				if (play_down && anim != null) {
					play_down = false;
					play_up = true;
					anim [animName].speed = -1f;
					anim [animName].normalizedTime = 1f;
					anim.Blend (animName, 2f);
				}
			}

			if (IsEnabled) {
				float percentOutputPower = totalEff * 100.0f;
				float outputPowerReport = -outputPower;
				if (Environment.TickCount - last_draw_update > 40) {
					OutputPower = outputPowerReport.ToString ("0.000") + "MW";
					OverallEfficiency = percentOutputPower.ToString ("0.0") + "%";
					if (totalEff >= 0) {
						MaxPowerStr = (maxThermalPower*totalEff).ToString ("0.000") + "MW";
					} else {
						MaxPowerStr = (0).ToString() + "MW";
					}
					last_draw_update = Environment.TickCount;
				}
			} else {
				OutputPower = "Generator Offline";
			}
		}

		public float getMaxPowerOutput() {
			double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
			float maxTotalEff = (float)carnotEff * pCarnotEff;
			return maxThermalPower * maxTotalEff;
		}

		public void updateGeneratorPower() {
			hotBathTemp = myAttachedReactor.getCoreTemp();
			maxThermalPower = myAttachedReactor.getThermalPower();
			coldBathTemp = FNRadiator.getAverageRadiatorTemperatureForVessel (vessel);
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			if (IsEnabled && myAttachedReactor != null && FNRadiator.hasRadiatorsForVessel (vessel)) {
				updateGeneratorPower ();
				double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
				totalEff = (float)(carnotEff * pCarnotEff);

				if (totalEff < 0) {
					return;
				}

				List<PartResource> partresources = new List<PartResource> ();
				part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition ("Megajoules").id, partresources);
				double currentmegajoules = 0;
				foreach (PartResource partresource in partresources) {
					currentmegajoules += (partresource.maxAmount - partresource.amount);
				}
				currentmegajoules = currentmegajoules / TimeWarp.fixedDeltaTime;

				double waste_heat_produced = (getCurrentUnfilledResourceDemand (FNResourceManager.FNRESOURCE_MEGAJOULES) + currentmegajoules);
				double thermal_power_currently_needed = waste_heat_produced / totalEff;
				double thermaldt = Math.Min (maxThermalPower, thermal_power_currently_needed) * TimeWarp.fixedDeltaTime;
				double inputThermalPower = consumeFNResource (thermaldt, FNResourceManager.FNRESOURCE_THERMALPOWER);
				double wastedt = inputThermalPower * totalEff;
				consumeFNResource (wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);

				double electricdt = inputThermalPower * totalEff;
				double electricdtps = Math.Max (electricdt / TimeWarp.fixedDeltaTime, 0.0);
				double max_electricdtps = maxThermalPower * totalEff;

				outputPower = -(float)supplyFNResourceFixedMax (electricdtps * TimeWarp.fixedDeltaTime, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
			} else {
				if (IsEnabled && !vessel.packed) {
					if (!FNRadiator.hasRadiatorsForVessel (vessel)) {
						IsEnabled = false;
						Debug.Log ("[WarpPlugin] Generator Shutdown: No radiators available!");
						ScreenMessages.PostScreenMessage ("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}

					if (myAttachedReactor == null) {
						IsEnabled = false;
						Debug.Log ("[WarpPlugin] Generator Shutdown: No reactor available!");
						ScreenMessages.PostScreenMessage ("Generator Shutdown: No reactor available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}
				}
			}
		}

		public override string GetInfo() {
			return String.Format("Percent of Carnot Efficiency: {0}%\n-Upgrade Information-\n Upgraded Percent of Carnot Efficiency: {1}%", pCarnotEff*100, upgradedpCarnotEff*100);
		}

	}
}

