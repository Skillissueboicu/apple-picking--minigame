using System.Collections.Generic;
using System.Threading.Tasks;
using FarmerQuest.Progress;

namespace FarmerQuest.Net
{
    [System.Serializable]
    public class PlayerStatDto
    {
        public int totalXp;
        public int markenXp;
        public int naturenXp;
        public int energiXp;
        public int madenXp;
        public int holdetXp;
        public int markenPercent;
        public int naturenPercent;
        public int energiPercent;
        public int madenPercent;
        public int holdetPercent;
        public int roundsPlayed;
        public List<BadgeProgressDto> badges;
    }

    [System.Serializable]
    public class BadgeProgressDto
    {
        public string badgeKey;
        public string tier;
        public int completions;
        public int bestCompletions;
        public int points;
    }

    public sealed class PlayerStatsApi
    {
        private readonly ApiClient _api;
        public PlayerStatsApi(ApiClient api) => _api = api;

        public Task<PlayerStatDto> GetMineAsync() =>
            _api.GetAsync<PlayerStatDto>("player-stats/mine");

        public Task<PlayerStatDto> AdjustAsync(string category, int delta) =>
            _api.PostAsync<PlayerStatDto>("player-stats/adjust", new { category, delta });

        public Task<PlayerStatDto> SetSlotPointsAsync(string slotKey, int points) =>
            _api.PostAsync<PlayerStatDto>("player-stats/slot", new { slotKey, points });

        public static PlayerProgressData ToProgress(PlayerStatDto dto)
        {
            if (dto == null) return new PlayerProgressData();
            var badges = new List<BadgeData>();
            if (dto.badges != null)
            {
                foreach (BadgeProgressDto b in dto.badges)
                {
                    badges.Add(new BadgeData
                    {
                        badgeKey = b.badgeKey,
                        tier = string.IsNullOrEmpty(b.tier) ? BadgeData.TierFromPoints(b.points) : b.tier,
                        completions = b.completions,
                        bestCompletions = b.bestCompletions,
                        points = b.points,
                    });
                }
            }

            return new PlayerProgressData
            {
                totalXp = dto.totalXp,
                markenXp = dto.markenXp,
                naturenXp = dto.naturenXp,
                energiXp = dto.energiXp,
                madenXp = dto.madenXp,
                holdetXp = dto.holdetXp,
                roundsPlayed = dto.roundsPlayed,
                badges = badges.ToArray(),
            };
        }
    }
}
