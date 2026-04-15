namespace MirrorM.Tests.Models
{
    public class PlayerPowerupEnergyBoost : PlayerPowerup
    {
        private const string FIELD_ENERGY_BOOST_POWER = "energy_boost_power";

        public float BoostPower
        {
            get => GetValue<float>(FIELD_ENERGY_BOOST_POWER);
            set => SetValue(FIELD_ENERGY_BOOST_POWER, value);
        }

        public PlayerPowerupEnergyBoost(IContext db, Guid playerId, float boostPower) : base(db, playerId)
        {
            BoostPower = boostPower;
        }

        public PlayerPowerupEnergyBoost(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
