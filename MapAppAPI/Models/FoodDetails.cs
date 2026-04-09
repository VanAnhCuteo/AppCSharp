using System.Collections.Generic;

namespace FoodMapAPI.Models
{
    public class FoodDetails : Food
    {
        public List<string> images { get; set; }
    }
}
