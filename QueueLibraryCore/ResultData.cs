using System;
using System.Collections.Generic;
using System.Text;

namespace QueueLibraryCore
{
    public class ResultData
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public List<string> Data { get; set; }

        public ResultData()
        {
            Success = false;
            Data = new List<string>();
        }
    }
}
