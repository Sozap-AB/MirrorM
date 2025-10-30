using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM;
using MirrorM.Relations;

namespace FishingTourServerTests.Tests.DataAccessLayer.Models
{
    [Entity("player_details")]
    public class PlayerDetails : Entity
    {
        public const string FIELD_PLAYER_ID = "player_id";

        [Field(FIELD_PLAYER_ID)]
        public Guid PlayerId
        {
            get => GetValue<Guid>(FIELD_PLAYER_ID);
            set => SetValue(FIELD_PLAYER_ID, value);
        }

        public IRelationFieldToId<Player> Player => GetRelationFieldToId<PlayerDetails, Player>(x => x.PlayerId);

        public PlayerDetails(IContext db, Player player) : base(db)
        {
            Player.Attach(player);
        }

        public PlayerDetails(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
