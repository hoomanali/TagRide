using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TagRides.Server.Centers;

namespace TagRides.Server.Controllers
{
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        // POST api/game/rating ? userId=... & rating=...
        [HttpPost("rating")]
        public async Task<ActionResult> PostRating([FromQuery]string userId, [FromQuery]double rating)
        {
            //TODO make sure the user trying to give the rating has done something with the user worthy of a rating (got a ride from them).
            //TODO maybe don't post the game info everytime. Like a driver might get a lot of incoming ratings, no need to post everytime.
            if (await GameCenter.GiveRating(userId, rating, true))
                return Ok();

            return BadRequest();
        }

        // POST api/game/faction ? userId=... & factionName=...
        [HttpPost("faction")]
        public async Task<ActionResult> PostFaction([FromQuery]string userId, [FromQuery]string factionName)
        {
            if (await GameCenter.SetFaction(userId, factionName))
                return Ok();

            //TODO get more detail about why it failed (bad faction name?)
            return BadRequest();
        }

        // POST api/game/remove-faction ? userId=...
        [HttpPost("remove-faction")]
        public async Task<ActionResult> PostRemoveFaction([FromQuery] string userId, [FromQuery] bool canChangeAgain)
        {
            if (await GameCenter.RemoveFaction(userId, canChangeAgain))
                return Ok();

            return BadRequest();
        }
    }
}