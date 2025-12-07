using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VolleyStats.DTO
{
    public class SkillAnalysisDto
    {
        public string SkillName { get; set; } = string.Empty;
        public List<SkillTrendPointDto> Trend { get; set; } = new();
    }

}
