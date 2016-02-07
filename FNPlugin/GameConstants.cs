using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    class GameConstants 
    {
        public const double basePowerConsumption = 5;
        public const double baseAMFPowerConsumption = 5000;
        public const double baseCentriPowerConsumption = 43.5;
        public const double baseELCPowerConsumption = 40;
        public const double baseAnthraquiononePowerConsumption = 5;
        public const double basePechineyUgineKuhlmannPowerConsumption = 5;
        public const double baseHaberProcessPowerConsumption = 20;
        public const double baseUraniumAmmonolysisPowerConsumption = 12;

        public const double anthraquinoneEnergyPerTon = 1834.321;
        public const double haberProcessEnergyPerTon = 34200;
        public const double waterElectrolysisEnergyPerTon = 18159;
        public const double aluminiumElectrolysisEnergyPerTon = 35485.714;
        public const double pechineyUgineKuhlmannEnergyPerTon = 1021;
        public const double EarthAtmospherePressureAtSeaLevel = 101.325;
        public const double KerbinAtmosphereDensityAtSeaLevel = 1.203016;

        public const double electrolysisMassRatio = 7.936429;
        public const double aluminiumElectrolysisMassRatio = 1.5;
        public const double thorium_power_output_ratio = 1.38;
        public const double thorium_resource_burnrate_ratio = 0.45;

        public const double deuterium_abudance = 0.00015625;
        public const double deuterium_timescale = 0.0016667;

        //public const string deuterium_tritium_fuel_mode = "Deuterium/Tritium";
        //public const string deuterium_helium3_fuel_mode = "Deuterium/Helium-3";
        //public const string helium3_fuel_mode = "Helium-3";

        public const double baseReprocessingRate = 400;
        public const double baseScienceRate = 0.1f;
        public const double baseUraniumAmmonolysisRate = 0.0002383381;

        public const double thorium_actinides_ratio_factor = 1;
        public const double thorium_temperature_ratio_factor = 1.17857;
        public const double plutonium_238_decay_constant = 3.6132369229223432425344238140179e-10;
        
        public const double microwave_angle = 3.64773814E-10f;
        public const double microwave_dish_efficiency = 0.85f;
        public const double microwave_alpha = 0.00399201596806387225548902195609f;

        public const float stefan_const = 5.670373e-8f;
        public const double warpspeed = 29979245.8;
        public const double rad_const_h = 1000;
        public const double alpha = 0.001998001998001998001998001998;
        public const double atmospheric_non_precooled_limit = 740;
        public const float initial_alcubierre_megajoules_required = 100;

        public const double telescopePerformanceTimescale = 2.1964508725630127431022388314009e-8;
        public const double telescopeBaseScience = 0.1666667;
        public const double telescopeGLensScience = 5;

        public const double antimatter_initiated_antimatter_cons_constant = 6.5075e-6;
        public const double antimatter_initiated_uf4_cons_constant = 1.0 / 128700.0;
        public const double antimatter_initiated_d_he3_cons_constant = 4.0 / 9.0;
        public const double antimatter_initiated_upgraded_d_he3_cons_constant = antimatter_initiated_d_he3_cons_constant * 1.037037;
        public const double antimatter_initiated_upgraded_uf4_cons_constant = antimatter_initiated_uf4_cons_constant / 3.0;

        public const double tritiumBreedRate = 428244.662271 / 0.222678566;
        public const double helium_boiloff_fraction = 1.667794e-8;
        public const double ammoniaHydrogenFractionByMass = 0.17647;

        public const int MAX_ANTIMATTER_TANK_STORED_CHARGE = 1000;
        public const int EARH_DAY_SECONDS = 86400;
        public const int KEBRIN_DAY_SECONDS = 21600;
        public const int HOUR_SECONDS = 3600;

        public const double ELECTRON_CHARGE = 1.602176565e-19;
        public const double ATOMIC_MASS_UNIT =  1.660538921e-27;
        public const float STANDARD_GRAVITY = 9.80665f;

        public const double dilution_factor = 15000.0;
        public const double LfoFuelThrustModifier = 2.2222;
        public const double IspCoreTemperatureMultiplier = 22.371670613;
        public const double BaseThrustPowerMultiplier = 2000;
        public const double HighCoreTempThrustMultiplier = 1600;

        public const float BaseMaxPowerDrawForExoticMatter = 1000f;
        public const float MaxThermalNozzleIsp = 2997.13f;
    }
}
