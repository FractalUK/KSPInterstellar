/*using System;

namespace FNPlugin
{
	class ElectricalRCSSystem : FNResourceSuppliableRCSModule	{
		protected float maxThrust = 0.5f;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Thrust")]
		public string thrustStr;

		public ElectricalRCSSystem () : base()	{
			maxThrust = thrusterPower;
		}


		public override void OnUpdate() {
			//base.OnFixedUpdate();
			float rcs_total_thrust = 0;
			foreach (float thrust_val in thrustForces) {
				rcs_total_thrust += thrust_val*maxThrust;
			}

			float power_required_megajoules = rcs_total_thrust*1000* 9.81f * realISP/1E6f;

			float power_received = consumeFNResource (power_required_megajoules*TimeWarp.deltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.deltaTime;
			if (power_required_megajoules > 0) {
				float power_received_pcnt = power_received / power_required_megajoules;
				thrusterPower = maxThrust * power_received_pcnt;
			} else {
				thrusterPower = 0;
			}

			thrustStr = thrusterPower.ToString ("0.000") + "kN";
		}
	}
}
 * */
