using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCms.Core.Models.Templates
{
    public class Template : BaseModel
    {
        public Template()
        {
            FieldGroups = Enumerable.Empty<TemplateFieldGroup>();
        }
        public IEnumerable<TemplateFieldGroup> FieldGroups { get; set; }
    }

}
