namespace PufferLaunch.Progress
{
    public static class StarRating
    {
        /// <summary>
        /// ★ mission won (enough bears down) · +★ at least one puffer left · +★ destruction score ≥ level threshold.
        /// A loss is always 0.
        /// </summary>
        public static int Compute(bool won, int puffersLeft, int destructionScore, int threeStarScore) =>
            Count(Rules(won, puffersLeft, destructionScore, threeStarScore));

        /// <summary>The bonus fish run cannot be lost and always uses every pup: ★ played · +★ half the fish · +★ 80% of them.</summary>
        public static int ComputeBonus(int fishGot, int fishTotal) => Count(BonusRules(fishGot, fishTotal));

        /// <summary>Which of the three rules were met, in the order the result card lists them (a star can be earned
        /// for the third rule without the second).</summary>
        public static (bool passed, bool second, bool third) Rules(bool won, int puffersLeft, int destructionScore, int threeStarScore) =>
            (won, won && puffersLeft >= 1, won && destructionScore >= threeStarScore);

        public static (bool passed, bool second, bool third) BonusRules(int fishGot, int fishTotal) =>
            (true, fishTotal > 0 && fishGot * 2 >= fishTotal, fishTotal > 0 && fishGot * 5 >= fishTotal * 4);

        static int Count((bool passed, bool second, bool third) rules) =>
            (rules.passed ? 1 : 0) + (rules.second ? 1 : 0) + (rules.third ? 1 : 0);
    }
}
