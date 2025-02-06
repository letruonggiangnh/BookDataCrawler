using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookDataCrawler
{
    internal class Book
    {
        public string BookName { get; set; }
        public string Price { get; set; }
        public string Author {  get; set; }
        public string Description { get; set; }
        public string Weight { get; set; }
        public string Size { get; set; }
        public string Language { get; set; }
        public int NumberOfPages {  get; set; }
        public bool CoverType { get; set; }
        public string Translator { get; set; }
        public string Age { get; set; }
        public string Supplier { get; set; }

        public Guid IdLevel1 { get; set; }
        public Guid IdLevel2 { get; set;}
        public Guid IdLevel3 { get; set;}
        public Guid IdLevel4 { get; set;}

    }
}
