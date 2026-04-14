using MirrorM.Attributes;
using MirrorM.Relations;

namespace MirrorM.Tests.Models
{
    [Entity("player_groups")]
    public class PlayerGroup : EntityBase
    {
        public const string CONNECTION_TABLE_PLAYER = "player_group_player";
        public const string CONNECTION_KEY = "group_id";

        private const string FIELD_NAME = "name";

        [Field(FIELD_NAME)]
        public string Name
        {
            get => GetValue<string>(FIELD_NAME);
            set => SetValue(FIELD_NAME, value);
        }

        public IRelationIdManyToIdMany<Player> Players => GetRelationManyToManyForeign<Player>(
            CONNECTION_TABLE_PLAYER,
            CONNECTION_KEY,
            Player.CONNECTION_KEY
        );

        public PlayerGroup(IContext db, string name) : base(db)
        {
            Name = name;
        }

        public PlayerGroup(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
