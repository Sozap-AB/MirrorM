using MirrorM.Attributes;
using MirrorM.Common;
using MirrorM.Relations;
using MirrorM.Tests.Tools;

namespace MirrorM.Tests.Models
{
    [Entity(TABLE)]
    public class Player : EntityBase
    {
        public enum Status
        {
            Active,
            Inactive,
        }

        private const string TABLE = "players";

        public const string CONNECTION_KEY = "player_id";

        private const string FIELD_NAME = "name";
        private const string FIELD_LEVEL = "level";
        private const string FIELD_STATUS = "status";

        [Field(FIELD_NAME)]
        public string Name
        {
            get => GetValue<string>(FIELD_NAME);
            set => SetValue(FIELD_NAME, value);
        }

        [Field(FIELD_LEVEL)]
        public int Level
        {
            get => GetValue<int>(FIELD_LEVEL);
            set => SetValue(FIELD_LEVEL, value);
        }

        [Field(FIELD_STATUS)]
        public Status PlayerStatus
        {
            get => GetValue<Status>(FIELD_STATUS);
            set => SetValue(FIELD_STATUS, value);
        }

        public IRelationIdToField<PlayerDetails> PlayerDetails => GetRelationIdToField<PlayerDetails>(x => x.PlayerId);
        public IRelationIdToFieldMany<PlayerInventoryItem> PlayerItems => GetRelationIdToFieldMany<PlayerInventoryItem>(x => x.PlayerId);
        public IRelationIdManyToIdMany<PlayerGroup> Groups => GetRelationManyToManyForeign<PlayerGroup>(
            PlayerGroup.CONNECTION_TABLE_PLAYER,
            CONNECTION_KEY,
            PlayerGroup.CONNECTION_KEY
        );
        public IRelationIdToFieldMany<PlayerPowerup> PlayerPowerups => GetRelationIdToFieldMany<PlayerPowerup>(x => x.PlayerId);

        public string Rank => ((RankCalculator)SuperContext!).CalculateRank(Level);

        public Player(IContext db, Guid id, string name, int level) : base(db, id)
        {
            Name = name;
            Level = level;
        }

        public Player(IContext db, string name, int level) : base(db)
        {
            Name = name;
            Level = level;
            PlayerStatus = Status.Active;
        }

        public Player(IContext db, IFields fields) : base(db, fields)
        {
        }

        public static async Task<IEnumerable<Player>> FindByNamePartialAsync(IContext db, string partialName)
        {
            return await db.ExecuteSqlQueryAsync<Player>(
                $"SELECT m.* FROM {TABLE} m WHERE m.{FIELD_NAME} LIKE @pattern",
                new SqlParameter("pattern", $"%{partialName}%")
            ).ToListAsync();
        }
    }
}
