using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShop.Models
{
    public class EntityOperationResult
    {
        public EntityOperationResult(bool? _IsSuccess, List<string> _Errors)
        {
            IsSuccess = _IsSuccess;
            Errors = _Errors;
        }
        public bool? IsSuccess { get; set; }
        public List<string> Errors { get; set; }
    }
}
