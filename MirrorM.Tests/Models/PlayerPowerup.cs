using MirrorM.Attributes;
using MirrorM.Common;
using MirrorM.Relations;

namespace MirrorM.Tests.Models
{
    [Entity(TABLE, subTypes: [typeof(PlayerPowerupHealthBoost), typeof(PlayerPowerupEnergyBoost)])]
    public abstract class PlayerPowerup : EntityBase
    {
        public const string TABLE = "player_powerups";

        private const string FIELD_USER_ID = "player_id";

        [Field(FIELD_USER_ID)]
        public Guid PlayerId
        {
            get => GetValue<Guid>(FIELD_USER_ID);
            set => SetValue(FIELD_USER_ID, value);
        }

        public IRelationFieldToId<Player> User => GetRelationFieldToId<PlayerPowerup, Player>(x => x.PlayerId);

        protected PlayerPowerup(IContext db, Guid playerId) : base(db)
        {
            PlayerId = playerId;
        }

        protected PlayerPowerup(IContext db, IFields fields) : base(db, fields)
        {
        }

        public static IAsyncEnumerable<PlayerPowerup> FindByPlayerIdAsync(IContext db, Guid playerId)
        {
            return db.ExecuteSqlQueryAsync<PlayerPowerup>(
                $"SELECT m.* FROM {TABLE} m WHERE m.{FIELD_USER_ID} = @player_id",
                new SqlParameter("player_id", playerId)
            );
        }
    }
}
