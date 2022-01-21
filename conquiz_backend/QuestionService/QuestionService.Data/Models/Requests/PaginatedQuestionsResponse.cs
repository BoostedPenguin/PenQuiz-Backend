using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionService.Data.Models.Requests
{
    public class PaginatedQuestionsResponse
    {
        public List<Questions> Questions { get; set; }
        public int PageIndex { get; set; }
        public bool HasPreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool HasNextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }
        public int TotalPages { get; set; }
    }
}
