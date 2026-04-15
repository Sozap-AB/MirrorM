namespace MirrorM.Tests.Models
{
    public class PlayerPowerupHealthBoost : PlayerPowerup
    {
        private const string FIELD_HEALTH_BOOST_POWER = "health_boost_power";

        public float BoostPower
        {
            get => GetValue<float>(FIELD_HEALTH_BOOST_POWER);
            set => SetValue(FIELD_HEALTH_BOOST_POWER, value);
        }

        public PlayerPowerupHealthBoost(IContext db, Guid playerId, float boostPower) : base(db, playerId)
        {
            BoostPower = boostPower;
        }

        public PlayerPowerupHealthBoost(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
