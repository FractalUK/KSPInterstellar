using System;

namespace FNPlugin{
	public interface FNThermalSource{

		float getCoreTemp();

		float getThermalPower();

        float getChargedPower();

		bool getIsNuclear();

		float getRadius();

		bool isActive();

		void enableIfPossible();

        bool shouldScaleDownJetISP();

        bool isVolatileSource();

        float getMinimumThermalPower();

        float getCoreTempAtRadiatorTemp(float rad_temp);

        float getThermalPowerAtTemp(float temp);

	}
}

