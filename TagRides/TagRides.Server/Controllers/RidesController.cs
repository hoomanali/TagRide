using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TagRides.Shared.RideData;
using TagRides.Shared.Geo;
using TagRides.Server.UserData;
using TagRides.Server.Centers;

namespace TagRides.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RidesController : ControllerBase
    {
        // POST api/rides/ride-request
        [HttpPost("ride-request")]
        public async Task<ActionResult> PostRideRequest([FromBody] RideRequest request, [FromQuery] string userId)
        {
            User user = await Users.GetUser(userId);
            if (user == null) return NotFound();

            string requestId = user.AddRideRequest(request);

            if (string.IsNullOrEmpty(requestId)) return BadRequest();

            return Created(requestId, null);
        }

        // POST api/rides/ride-offer
        [HttpPost("ride-offer")]
        public async Task<ActionResult<string>> PostRideOffer([FromBody] RideOffer offer, [FromQuery] string userId)
        {
            User user = await Users.GetUser(userId);
            if (user == null) return NotFound();

            string requestId = user.AddRideOffer(offer);

            //TODO is this the correct response code?
            if (string.IsNullOrEmpty(requestId)) return BadRequest();

            return Created(requestId, null);
        }

        // POST api/rides/pending-ride/confirm
        [HttpPost("pending-ride/confirm")]
        public async Task<ActionResult> PostPendingRideConfirm([FromQuery] string userId, [FromQuery] string pendingRideId)
        {
            User user = await Users.GetUser(userId);
            if (user == null) return NotFound();

            if (await user.ConfirmPendingRide(pendingRideId))
                return Ok();

            return BadRequest();
        }

        // POST api/rides/request/cancel
        [HttpPost("request/cancel")]
        public async Task<ActionResult> PostRequestCancel([FromQuery] string userId, [FromQuery] string requestId)
        {
            User user = await Users.GetUser(userId);
            if (user == null) return NotFound();

            if (user.CancelRequest(requestId))
                return Ok();

            return BadRequest();
        }

        // POST api/rides/active-ride/in-ride
        [HttpPost("active-ride/in-ride")]
        public async Task<ActionResult> PostActiveRideInRide([FromQuery] string userId, [FromQuery] string activeRideId)
        {
            if (await ActiveRideCenter.UserInRide(activeRideId, userId))
                return Ok();
            return BadRequest();
        }

        // POST api/rides/active-ride/finish
        [HttpPost("active-ride/finish")]
        public async Task<ActionResult> PostActiveRideFinish([FromQuery] string userId, [FromQuery] string activeRideId)
        {
            if (await ActiveRideCenter.UserFinished(activeRideId, userId))
                return Ok();
            return BadRequest();
        }

        // POST api/rides/active-ride/cancel
        [HttpPost("active-ride/cancel")]
        public async Task<ActionResult> PostActiveRideCancel([FromQuery] string userId, [FromQuery] string activeRideId)
        {
            if (await ActiveRideCenter.UserCanceled(activeRideId, userId))
                return Ok();
            return BadRequest();
        }
        
        // GET api/rides/ping
        [HttpGet("ping")]
        public ActionResult<string> Ping()
        {
            return System.DateTime.Now.ToString();
        }
    }
}
