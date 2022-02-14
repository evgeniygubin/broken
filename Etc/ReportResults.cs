using System;
using System.Collections.Generic;
using System.Text;

namespace BrokenCode.Etc
{
    public class ReportResults<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
        public bool Succeeded { get; set; }
        public string Error { get; set; }
    }
}
