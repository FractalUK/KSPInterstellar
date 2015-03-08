using System;

namespace FNPlugin
{
    public interface ThermalReciever
    {
        void AttachThermalReciever(Guid key, float radius);

        void DetachThermalReciever(Guid key);

        float GetFractionThermalReciever(Guid key);

        float ThermalTransportationEfficiency { get; }
    }


    public interface INoozle 
    {
        int Fuel_mode { get; }

        bool Static_updating { get; set; }
        bool Static_updating2 { get; set; }

        ConfigNode[] getPropellants();

        double getNozzleFlowRate(); 
    }

    public interface IThermalSource : ThermalReciever
    {
        float MaximumPower { get; }

        float MinimumPower { get; }

        float MaximumThermalPower { get; }

        float CoreTemperature { get; }

        bool IsSelfContained { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

        float getRadius();

        bool IsNuclear { get; }

		void enableIfPossible();

        bool shouldScaleDownJetISP();

        float GetCoreTempAtRadiatorTemp(float rad_temp);

        float GetThermalPowerAtTemp(float temp);

	}
}

