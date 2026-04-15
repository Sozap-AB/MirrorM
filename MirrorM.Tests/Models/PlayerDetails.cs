using MirrorM.Attributes;
using MirrorM.Common;
using MirrorM.Relations;
using MirrorM.Tests.Tools;
using System.Text.Json.Nodes;

namespace MirrorM.Tests.Models
{
    [Entity(TABLE)]
    public class PlayerDetails : EntityBase
    {
        private const string TABLE = "player_details";

        private const string FIELD_PLAYER_ID = "player_id";
        private const string FIELD_META_DATA = "meta_data";

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

        public PlayerDetails(IContext db, Player player) : this(db)
        {
            this.Player.AttachTo(player);
        }

        public PlayerDetails(IContext db) : base(db)
        {
            MetaData = new JsonObject();
        }

        public PlayerDetails(IContext db, IFields fields) : base(db, fields)
        {
        }

        public async static Task<PlayerDetails> InstantInsertAsync(IContext db, Guid playerId, int metaDataNumber)
        {
            return (await db.ExecuteSqlQueryAsync<PlayerDetails>(
                $"INSERT INTO {TABLE} VALUES(@id, @player_id, @meta_data, 1, @now, @now) " +
                $"RETURNING *",
                new SqlParameter("id", Guid.NewGuid()),
                new SqlParameter("player_id", playerId),
                new SqlParameter("meta_data", JsonValue.Create(metaDataNumber)),
                new SqlParameter("now", DateTimeOffset.UtcNow)
            ).ToListAsync()).First();
        }
    }
}
