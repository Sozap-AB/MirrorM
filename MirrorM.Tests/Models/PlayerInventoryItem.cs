using MirrorM.Attributes;
using MirrorM.Relations;

namespace MirrorM.Tests.Models
{
    [Entity("player_items")]
    public class PlayerInventoryItem : Entity
    {
        private const string FIELD_PLAYER_ID = "player_id";
        private const string FIELD_NAME = "name";

        [Field(FIELD_PLAYER_ID)]
        public Guid PlayerId
        {
            get => GetValue<Guid>(FIELD_PLAYER_ID);
            set => SetValue(FIELD_PLAYER_ID, value);
        }

        [Field(FIELD_NAME)]
        public string Name
        {
            get => GetValue<string>(FIELD_NAME);
            set => SetValue(FIELD_NAME, value);
        }

        public IRelationFieldToId<Player> User => GetRelationFieldToId<PlayerInventoryItem, Player>(x => x.PlayerId);

        public PlayerInventoryItem(IContext db, string name) : base(db)
        {
            Name = name;
        }

        public PlayerInventoryItem(IContext db, IFields fields) : base(db, fields)
        {
        }
    }
}
