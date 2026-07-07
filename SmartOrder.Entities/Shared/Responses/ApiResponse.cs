using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartOrder.Entities.Shared.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(T data)
        {
            Success = true;
            Data = data;
        }

        public ApiResponse(Exception ex, string? customMessage = null)
        {
            Success = false;
            if (!string.IsNullOrWhiteSpace(customMessage))
                Errors.Add(customMessage);

            Errors.Add(ex.Message);

            var inner = ex.InnerException;
            while (inner != null)
            {
                Errors.Add(inner.Message);
                inner = inner.InnerException;
            }
        }
    }
}

