using System;

namespace FNPlugin{
	public interface FNThermalSource{

		float getThermalTemp();

		float getThermalPower();

		bool getIsNuclear();

		float getRadius();

	}
}

