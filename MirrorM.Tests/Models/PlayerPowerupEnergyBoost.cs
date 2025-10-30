using MirrorM;

namespace FishingTourServerTests.Tests.DataAccessLayer.Models
{
    public class PlayerPowerupEnergyBoost : PlayerPowerup
    {
        private const string FIELD_ENERGY_BOOST_POWER = "energy_boost_power";

        public float BoostPower
        {
            get => GetValue<float>(FIELD_ENERGY_BOOST_POWER);
            set => SetValue(FIELD_ENERGY_BOOST_POWER, value);
        }

        public PlayerPowerupEnergyBoost(IContext db, float boostPower) : base(db)
        {
            BoostPower = boostPower;
        }

        public PlayerPowerupEnergyBoost(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
