namespace PufferLaunch.Progress
{
    public static class StarRating
    {
        /// <summary>
        /// ★ mission won (enough bears down) · +★ at least one puffer left · +★ destruction score ≥ level threshold.
        /// A loss is always 0.
        /// </summary>
        public static int Compute(bool won, int puffersLeft, int destructionScore, int threeStarScore)
        {
            if (!won) return 0;
            int stars = 1;
            if (puffersLeft >= 1) stars++;
            if (destructionScore >= threeStarScore) stars++;
            return stars;
        }
    }
}
