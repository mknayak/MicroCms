using MicroCms.Core;
using MicroCms.Core.Contracts.Providers;
using MicroCms.Core.Models.Templates;
using MicroCms.Web.Api.Helpers;
using MicroCms.Web.Api.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MicroCms.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemplateApiController : ControllerBase
    {
        private readonly IContentProvider contentProvider;

        public TemplateApiController(IContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
        }

        // GET api/<TemplateApiController>/5
        [HttpGet("{id}")]
        public TemplateModel? Get(string id)
        {
            var item = contentProvider.FindTemplateById(id);

            return item?.ToTemplateModel();
        }

        // POST api/<TemplateApiController>
        [HttpPost]
        public void Post(string name, IDictionary<string, TemplateFieldType> fields, string? parentId = null)
        {
            parentId = parentId ?? Constants.Ids.TemplateRootId;
            contentProvider.AddTemplate(name, parentId, fields.Select(c => new TemplateField(c.Key) { Type = c.Value }).ToArray());
        }

        // PUT api/<TemplateApiController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<TemplateApiController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
