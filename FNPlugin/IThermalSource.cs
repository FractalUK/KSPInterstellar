using System;

namespace FNPlugin{
	public interface IThermalSource{

        float CoreTemperature { get; }

        float MaximumThermalPower { get; }

        float MinimumThermalPower { get; }

        float ChargedPower { get; }

        bool IsNuclear { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

		float getRadius();

		void enableIfPossible();

        bool shouldScaleDownJetISP();

        float GetCoreTempAtRadiatorTemp(float rad_temp);

        float GetThermalPowerAtTemp(float temp);

	}
}

