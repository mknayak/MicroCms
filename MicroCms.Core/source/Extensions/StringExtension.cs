using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCms.Core.Extensions
{
    public static class StringExtension
    {
        public static string ToId(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.NewGuid().ToString();

            bool parseSuccess = Guid.TryParse(input, out var id);

            return (parseSuccess ? id : Guid.NewGuid()).ToString();

        }
    }
}
