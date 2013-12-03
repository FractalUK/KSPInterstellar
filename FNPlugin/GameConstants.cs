using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class GameConstants {
        public const double baseReprocessingRate = 0.4;
        public const double basePowerConsumption = 5;
        public const double electrolysisEnergyPerTon = 18159;
        public const double bakingEnergyPerTon = 4920;
        public const double aluminiumElectrolysisEnergyPerTon = 35485.714;
        public const double electrolysisMassRatio = 7.936429;
        public const double aluminiumElectrolysisMassRatio = 1.5;
        public const double baseELCPowerConsumption = 40;
        public const double baseMiningPowerConsumption = 10;
        public const double baseMiningRatePerTon = 0.009259259259;
        public const double baseScienceRate = 0.1f;
        public const double baseAMFPowerConsumption = 5000;
        public const double baseCentriPowerConsumption = 43.5;
        public const double deuterium_abudance = 0.00015625;
        public const double deuterium_timescale = 0.0016667;
        public const double stefan_const = 5.670373e-8;
        public const double warpspeed = 29979245.8;
        public const float MAX_ANTIMATTER_TANK_STORED_CHARGE = 1000;
        public const double thorium_power_output_ratio = 1.38;
        public const double thorium_resource_burnrate_ratio = 0.45;
        public const double thorium_actinides_ratio_factor = 1;
        public const double thorium_temperature_ratio_factor = 1.17857;
        public const double plutonium_238_decay_constant = 3.6132369229223432425344238140179e-10;
        public const double microwave_angle = 3.64773814E-10f;
        public const double microwave_dish_efficiency = 0.85f;
        public const double microwave_alpha = 0.00399201596806387225548902195609f;
    }
}
