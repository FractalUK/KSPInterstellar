using System;

namespace FNPlugin
{
    public enum ElectricGeneratorType { unknown = 0, thermal = 1, charged_particle = 2 };

    public interface IThermalReciever
    {
        void AttachThermalReciever(Guid key, float radius);

        void DetachThermalReciever(Guid key);

        float GetFractionThermalReciever(Guid key);

        float ThermalTransportationEfficiency { get; }
    }


    public interface IThermalSource : IThermalReciever
    {
        double ProducedWasteHeat { get; }

        float PowerBufferBonus { get; }

        float StableMaximumReactorPower { get; }

        float MaximumPower { get; }

        float MinimumPower { get; }

        double ChargedPowerRatio { get; }

        float MaximumThermalPower { get; }

        float MaximumChargedPower { get; }

        float CoreTemperature { get; }

        float HotBathTemperature { get; }

        bool IsSelfContained { get; }

        bool IsActive { get; }

        bool IsVolatileSource { get; }

        float GetRadius();

        bool IsNuclear { get; }

		void EnableIfPossible();

        bool shouldScaleDownJetISP();

        float GetCoreTempAtRadiatorTemp(float rad_temp);

        float GetThermalPowerAtTemp(float temp);

        bool IsThermalSource { get; }

        float ThermalPropulsionEfficiency { get; }

        float ThermalEnergyEfficiency { get; }

        float ChargedParticleEnergyEfficiency { get; }

        double EfficencyConnectedThermalEnergyGenrator { get; }

        double EfficencyConnectedChargedEnergyGenrator { get; }

        void NotifyActiveThermalEnergyGenrator(double efficency, ElectricGeneratorType generatorType);

        void NotifyActiveChargedEnergyGenrator(double efficency, ElectricGeneratorType generatorType);

        bool ShouldApplyBalance(ElectricGeneratorType generatorType);
	}
}

