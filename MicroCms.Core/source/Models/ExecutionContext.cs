using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCms.Core.Models
{
    public class ExecutionContext : IDisposable
    {
        public ExecutionContext()
        {
            Items = new Dictionary<string, object>();
        }
        public Dictionary<string, object> Items { get; set; }
        public void Dispose()
        {
            Items.Clear();
        }
    }
}
