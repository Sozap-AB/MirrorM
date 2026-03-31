namespace MirrorM.Tests.Tools
{
    internal class RankCalculator : ISuperContext
    {
        public string CalculateRank(int level)
        {
            switch (level)
            {
                case int l when l >= 1 && l <= 10: return "Beginner";
                case int l when l > 10 && l <= 25: return "Mature";
                case int l when l > 25 && l <= 50: return "Pro";
                case int l when l > 50 && l <= 100: return "Ace";
                default: return "Champion";
            }
        }
    }
}
