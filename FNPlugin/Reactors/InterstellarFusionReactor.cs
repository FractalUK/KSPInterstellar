using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true)]
        public bool allowJumpStart = true;

        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk1 = 10;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk2 = 20;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk3 = 40;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk4 = 80;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk5 = 120;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Maintance")]
        public string electricPowerMaintenance;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Plasma Ratio")]
        public float plasma_ratio = 1.0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Is Swapping Fuel Mode")]
        public bool isSwappingFuelMode = false;

        //[KSPField(isPersistant = false, guiActive = true, guiName = "Min Power Req ", guiUnits = " MW")]
        //public float minimumPowerRequirement;
        //[KSPField(isPersistant = false, guiActive = true, guiName = "Fusion Q factor")]
        //public float fusionQfactor;

        //public float
        protected PartResource lithiumPartResource = null;

        public float MaximumChargedIspMult { get { return 100 ; } }

        public float MinimumChargdIspMult { get { return 1; } }

        public override float StableMaximumReactorPower { get { return IsEnabled && plasma_ratio >= 1 ? RawPowerOutput : 0; } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override float MaximumThermalPower 
        { 
            get 
            {
                float lithiumModifier = lithiumPartResource != null ? (float)Math.Sqrt(lithiumPartResource.amount / lithiumPartResource.maxAmount) : 1;

                float plasmaModifier = (plasma_ratio >= 1.0 ? 1 : 0);

                return base.MaximumThermalPower * lithiumModifier * Math.Max(lithiumModifier * plasmaModifier, 0.000000001f); 
            } 
        }

        public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        //public override float CoreTemperature {  get { return base.CoreTemperature * (current_fuel_mode != null ? (float)Math.Sqrt(current_fuel_mode.NormalisedPowerRequirements) : 1); } }

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public float PowerRequirement { get { return RawPowerOutput / FusionEnergyGainFactor; } }


        public float FusionEnergyGainFactor
        {
            get
            {
                if (CurrentGenerationType == GenerationType.Mk5)
                    return fusionEnergyGainFactorMk5;
                else if (CurrentGenerationType == GenerationType.Mk4)
                    return fusionEnergyGainFactorMk4;
                else if (CurrentGenerationType == GenerationType.Mk3)
                    return fusionEnergyGainFactorMk3;
                else if (CurrentGenerationType == GenerationType.Mk2)
                    return fusionEnergyGainFactorMk2;
                else
                    return fusionEnergyGainFactorMk1;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {

            lithiumPartResource = part.Resources.list.FirstOrDefault(r => r.resourceName == InterstellarResourcesConfiguration.Instance.Lithium);

            // call Interstellar Reactor Onstart
            base.OnStart(state);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Fusion Mode", active = true)]
        public void NextFusionModeEvent()
        {
            SwitchToNextFuelMode(fuel_mode);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Previous Fusion Mode", active = true)]
        public void PreviousFusionModeEvent()
        {
            SwitchToPreviousFuelMode(fuel_mode);
        }

        [KSPAction("Next Fusion Mode")]
        public void NextFusionModeAction(KSPActionParam param)
        {
            NextFusionModeEvent();
        }

        [KSPAction("Previous Fusion Mode")]
        public void PreviousFusionModeAction(KSPActionParam param)
        {
            PreviousFusionModeEvent();
        }

        private void SwitchToNextFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count)
                fuel_mode = 0;

            current_fuel_mode = fuel_modes[fuel_mode];

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToNextFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        private bool FullFuelRequirments()
        {
            return HasAllFuels() && FuelRequiresLab(current_fuel_mode.RequiresLab);
        }

        private bool HasAllFuels()
        {
            bool hasAllFuels = true;
            foreach (var fuel in current_fuel_mode.ReactorFuels)
            {
                if (GetFuelRatio(fuel, FuelEfficiency, NormalisedMaximumPower) < 1)
                {
                    hasAllFuels = false;
                    break;
                }
            }
            return hasAllFuels;
        }

        private void SwitchToPreviousFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuel_modes.Count - 1;

            current_fuel_mode = fuel_modes[fuel_mode];

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToPreviousFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        protected override void WindowReactorSpecificOverride()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Next Fusion Mode", GUILayout.ExpandWidth(true)))
            {
                NextFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous Fusion Mode", GUILayout.ExpandWidth(true)))
            {
                PreviousFusionModeEvent();
            }
            GUILayout.EndHorizontal();

            PrintToGUILayout("Fusion Maintenance", electricPowerMaintenance, bold_label);
        }

        //public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        //{
        //    //fusionQfactor = FusionEnergyGainFactor;
        //    //minimumPowerRequirement = PowerRequirement;
        //    base.OnFixedUpdate();
        //}
    }
}
