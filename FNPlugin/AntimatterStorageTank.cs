using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	class AntimatterStorageTank : FNResourceSuppliableModule	{
        //Persistent True
		[KSPField(isPersistant = true)]
		public float chargestatus = 1000.0f;

        //Persistent False
        [KSPField(isPersistant = false)]
        public float chargeNeeded = 100f;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
		public string chargeStatusStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string statusStr;

		bool charging = false;
		bool should_charge = true;
		float explosion_time = 0.35f;
		bool exploding = false;
		float explosion_size = 5000;
		float cur_explosion_size = 0;
		float current_antimatter = 0;
		int explode_counter = 0;
		GameObject lightGameObject;

		

		[KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
		public void StartCharge() {
			should_charge = true;
		}

		[KSPEvent(guiActive = true, guiName = "Stop Charging", active = true)]
		public void StopCharge() {
			should_charge = false;
		}

		public void doExplode() {
			if (current_antimatter <= 0.1f) {
				return;
			}

			lightGameObject = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			lightGameObject.collider.enabled = false;
			lightGameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			lightGameObject.AddComponent<Light>();
			lightGameObject.renderer.material.shader = Shader.Find("Unlit/Transparent");
			lightGameObject.renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/explode", false);
			lightGameObject.renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 0.9f);
			Light light = lightGameObject.light;
			lightGameObject.transform.position = part.transform.position;
			light.type = LightType.Point;					
			light.color = Color.white;						
			light.range = 100f;							
			light.intensity = 500000.0f;							
			light.renderMode = LightRenderMode.ForcePixel;
			//Destroy (lightGameObject.collider, 0.25f);
			Destroy (lightGameObject, 0.25f);

			bool exist_parts_to_explode = true;
			Part part_to_explode = null;
			exploding = true;

		}

		public override void OnStart(PartModule.StartState state) {

			if (state == StartState.Editor) { return; }
			this.part.force_activate();
		}

		public override void OnUpdate() {
			Events ["StartCharge"].active = current_antimatter <= 0.1 && !should_charge;
			Events ["StopCharge"].active = current_antimatter <= 0.1 && should_charge;
			chargeStatusStr = chargestatus.ToString ("0.0") + "/" + GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE.ToString ("0.0");

			if (chargestatus <= 60 && !charging && current_antimatter > 0.1) {
				ScreenMessages.PostScreenMessage("Warning!: Antimatter storage unpowered, tank explosion in: " + chargestatus.ToString("0") + "s", 1.0f, ScreenMessageStyle.UPPER_CENTER);
			}

			if (current_antimatter > 0.1) {
				if (charging) {
					statusStr = "Charging.";
				} else {
					statusStr = "Discharging!";
				}
			} else {
				if (should_charge) {
					statusStr = "Charging.";
				} else {
					statusStr = "No Power Required.";
				}
			}
		}

		public override void OnFixedUpdate() {

			List<PartResource> antimatter_resources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, antimatter_resources);
			float antimatter_current_amount = 0;
            float mult = 1;
			foreach (PartResource antimatter_resource in antimatter_resources) {
				antimatter_current_amount += (float)antimatter_resource.amount;
			}
			current_antimatter = antimatter_current_amount;
			explosion_size = Mathf.Sqrt (antimatter_current_amount)*5.0f;
			if (chargestatus > 0 && current_antimatter > 0.1) {
				chargestatus -= 1.0f * TimeWarp.fixedDeltaTime;
			}
            if (chargestatus >= GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE) {
                mult = 0.5f;
            }
            if (should_charge || (current_antimatter > 0.1)) {
                float charge_to_add = consumeFNResource(mult*2.0*chargeNeeded/1000.0 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000.0f/chargeNeeded;
                chargestatus += charge_to_add;

				if (charge_to_add < 2f * TimeWarp.fixedDeltaTime) {
                    float more_charge_to_add = part.RequestResource("ElectricCharge", mult * 2 * chargeNeeded * TimeWarp.fixedDeltaTime) / chargeNeeded;
					charge_to_add += more_charge_to_add;
					chargestatus += more_charge_to_add;
				}

				if (charge_to_add >= 1f * TimeWarp.fixedDeltaTime) {
					charging = true;
				} else {
					charging = false;
					if (TimeWarp.CurrentRateIndex > 3  && antimatter_current_amount > 0.1) {
						TimeWarp.SetRate (3, true);
						ScreenMessages.PostScreenMessage("Cannot Time Warp faster than 50x while Antimatter Tank is Unpowered", 1.0f, ScreenMessageStyle.UPPER_CENTER);
					}
				}
				//print (chargestatus);
				if (chargestatus <= 0) {
					chargestatus = 0;
					if (antimatter_current_amount > 0.1) {
						explode_counter++;
						if (explode_counter > 5) {
							doExplode ();
						}
					}
				} else {
					explode_counter = 0;
				}

                if (chargestatus > GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE) {
                    chargestatus = GameConstants.MAX_ANTIMATTER_TANK_STORED_CHARGE;
                }
			} else {
                
			}

			if (exploding && lightGameObject != null) {
				if (Mathf.Sqrt (cur_explosion_size) > explosion_size) {
					lightGameObject.collider.enabled = false;
					//Destroy (lightGameObject);
				}

				cur_explosion_size += TimeWarp.fixedDeltaTime * explosion_size * explosion_size / explosion_time;
				lightGameObject.transform.localScale = new Vector3(Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size), Mathf.Sqrt(cur_explosion_size));
				lightGameObject.light.range = Mathf.Sqrt (cur_explosion_size) * 15f;
				if (Mathf.Sqrt(cur_explosion_size) > explosion_size) {
					TimeWarp.SetRate (0, true);
					vessel.GoOffRails();

					Vessel[] list_of_vessels_to_explode = FlightGlobals.Vessels.ToArray ();
					foreach (Vessel vess_to_explode in list_of_vessels_to_explode) {
						if (Vector3d.Distance (vess_to_explode.transform.position, vessel.transform.position) <= explosion_size) {
							if (vess_to_explode.packed == false) {
								Part[] parts_to_explode = vess_to_explode.Parts.ToArray();
								foreach (Part part_to_explode in parts_to_explode) {
									if (part_to_explode != null) {
										part_to_explode.explode ();	
									}
								}
							}
						}
					}

					Part[] explode_parts = vessel.Parts.ToArray ();
					foreach (Part explode_part in explode_parts) {
						if (explode_part != vessel.rootPart && explode_part != this.part) {
							explode_part.explode ();
						}
					}
					vessel.rootPart.explode ();
					this.part.explode ();
				//	this.part.explode ();
				//	vessel.rootPart.explode ();
				}
			}

		}

        public override string GetInfo() {
            return "Maximum Power Requirements: " + (chargeNeeded*2).ToString("0") + " KW\nMinimum Power Requirements: " + chargeNeeded.ToString("0") + " KW";
        }

        public override string getResourceManagerDisplayName() {
            return "Antimatter Storage Tank";
        }


	}

}

