using MicroCms.Core.Contracts.Providers;
using MicroCms.Web.Api.Helpers;
using MicroCms.Web.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace MicroCms.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemApiController : ControllerBase
    {
        private readonly IContentProvider contentProvider;

        public ItemApiController(IContentProvider contentProvider)
        {
            this.contentProvider = contentProvider;
        }
        

        // GET api/<TemplateApiController>/5
        [HttpGet("{id}")]
        public ItemModel? Get(string id)
        {
            var item = contentProvider.FindItemById(id);

            return item?.ToItemModel();
        }

        // POST api/<TemplateApiController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
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
