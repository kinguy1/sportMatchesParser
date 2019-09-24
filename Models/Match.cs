using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
   public class Match
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Competition { get; set; }
        public string TeamA { get; set; }
        public string TeamB { get; set; }
    }
}
