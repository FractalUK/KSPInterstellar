using System;

namespace FNPlugin
{
    public interface IThermalReciever
    {
        void AttachThermalReciever(Guid key, float radius);

        void DetachThermalReciever(Guid key);

        float GetFractionThermalReciever(Guid key);

        float ThermalTransportationEfficiency { get; }
    }


    public interface IThermalSource : IThermalReciever
    {
        float StableMaximumThermalPower { get; }

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

