using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.Enums
{
    public static class EnumSources
    {
        public static PlayerPost[] PlayerPosts { get; } =
            Enum.GetValues(typeof(PlayerPost)).Cast<PlayerPost>().ToArray();
    }
}
