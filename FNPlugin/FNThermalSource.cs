using System;

namespace FNPlugin{
	public interface FNThermalSource{

		float getCoreTemp();

		float getThermalPower();

		bool getIsNuclear();

		float getRadius();

	}
}

