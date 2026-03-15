using System.Collections.Generic;

namespace FoodMapAPI.Models
{
    public class FoodDetails : Food
    {
        public int visitor_count { get; set; }
        public List<string> images { get; set; }
    }
}
