using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM;
using MirrorM.Relations;
using System.Text.Json.Nodes;

namespace FishingTourServerTests.Tests.DataAccessLayer.Models
{
    [Entity("player_details")]
    public class PlayerDetails : Entity
    {
        public const string FIELD_PLAYER_ID = "player_id";
        public const string FIELD_META_DATA = "meta_data";

        [Field(FIELD_PLAYER_ID)]
        public Guid PlayerId
        {
            get => GetValue<Guid>(FIELD_PLAYER_ID);
            set => SetValue(FIELD_PLAYER_ID, value);
        }

        [Field(FIELD_META_DATA)]
        public JsonNode MetaData
        {
            get => GetValue<JsonNode>(FIELD_META_DATA);
            set => SetValue(FIELD_META_DATA, value);
        }

        public IRelationFieldToId<Player> Player => GetRelationFieldToId<PlayerDetails, Player>(x => x.PlayerId);

        public PlayerDetails(IContext db, Player player) : base(db)
        {
            Player.Attach(player);
            MetaData = new JsonObject();
        }

        public PlayerDetails(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
