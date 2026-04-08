using MirrorM.Attributes;
using MirrorM.Relations;

namespace MirrorM.Tests.Models
{
    [Entity("player_powerups", subTypes: [typeof(PlayerPowerupHealthBoost), typeof(PlayerPowerupEnergyBoost)])]
    public abstract class PlayerPowerup : Entity
    {
        public const string FIELD_USER_ID = "player_id";
        public const string FIELD_POWERUP_DATA = "data";

        [Field(FIELD_USER_ID)]
        public Guid PlayerId
        {
            get => GetValue<Guid>(FIELD_USER_ID);
            set => SetValue(FIELD_USER_ID, value);
        }

        public IRelationFieldToId<Player> User => GetRelationFieldToId<PlayerPowerup, Player>(x => x.PlayerId);

        protected PlayerPowerup(IContext db) : base(db)
        {
        }

        protected PlayerPowerup(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
