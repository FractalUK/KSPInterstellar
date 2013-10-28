using System;

namespace FNPlugin{
	public interface FNThermalSource{

		float getThermalTemp();

		float getThermalPower();

		bool getIsNuclear();

		bool getIsThermalHeatExchanger();

		float getRadius();

		bool isActive();

		void enableIfPossible();

	}
}

