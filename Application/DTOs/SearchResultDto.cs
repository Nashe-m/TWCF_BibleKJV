using System;
using System.Collections.Generic;
using System.Text;

namespace TWCF_BibleApp.Application.DTOs
{
    public class SearchResultDto
    {
        public int BookId { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Reference { get; set; }
        public string Text { get; set; }
    }
}
