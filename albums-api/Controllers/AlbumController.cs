using albums_api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace albums_api.Controllers
{
    [Route("albums")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        // GET /albums
        [HttpGet]
        public IActionResult Get() => Ok(Album.GetAll());

        // GET /albums/{id}
        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var album = Album.GetById(id);
            return album is null ? NotFound() : Ok(album);
        }

        // GET /albums/year/{year}
        [HttpGet("year/{year:int}")]
        public IActionResult GetByYear(int year) => Ok(Album.GetByYear(year));

        // POST /albums
        [HttpPost]
        public IActionResult Create([FromBody] Album album)
        {
            if (album is null) return BadRequest();
            var created = Album.Add(album);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        // PUT /albums/{id}
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] Album album)
        {
            if (album is null) return BadRequest();
            var updated = Album.Update(id, album);
            return updated is null ? NotFound() : Ok(updated);
        }

        // DELETE /albums/{id}
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
            => Album.Delete(id) ? NoContent() : NotFound();
    }
}
