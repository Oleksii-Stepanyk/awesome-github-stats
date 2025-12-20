using AwesomeGithubStats.Core.Interfaces;
using AwesomeGithubStats.Core.Models;
using Microsoft.Extensions.Options;

namespace AwesomeGithubStats.Core.Services
{
    public class RankService : IRankService
    {
        private readonly RankDegree _rankDegree;

        public RankService(IOptions<RankDegree> rankDegree)
        {
            _rankDegree = rankDegree.Value;
        }

        public UserRank CalculateRank(UserStats userStats, RankPoints rankPoints)
        {
            var updatedStats = userStats.ApplyRankPoints(rankPoints);
            return new(rankPoints, updatedStats, _rankDegree);
        }
    }
}
