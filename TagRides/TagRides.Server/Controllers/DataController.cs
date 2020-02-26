using System;
using TagRides.Shared.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TagRides.Shared.Geo;
using TagRides.Server.UserData;
using TagRides.Server.Centers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TagRides.Server.Controllers
{
    [Route("api/[controller]")]
    public class DataController : Controller
    {
        // POST api/data/location
        [HttpPost("location")]
        public async Task<ActionResult> PostLocation([FromBody] GeoCoordinates location, [FromQuery]string userId)
        {
            User user = await Users.GetUser(userId);
            if (user == null) return NotFound();

            user.LastKnownLocation = location;

            return Ok();
        }

        // GET api/data/passengers
        [HttpGet("passengers")]
        public IEnumerable<GeoCoordinates> GetPassengers()
        {
            return PendingRideRequestCenter.OriginLocations;
        }

        // GET api/data/users-in-rect?minLongitude=...&maxLongitude=...&minLatitude=...&maxLatitude=...
        [HttpGet("users-in-rect")]
        public ActionResult<IEnumerable<GeoCoordinates>> GetUsersIn(double minLongitude, double maxLongitude, double minLatitude, double maxLatitude)
        {
            Rect rect = new Rect
            {
                xMin = minLongitude,
                xMax = maxLongitude,
                yMin = minLatitude,
                yMax = maxLatitude
            };

            if (!GeoRectUtils.IsValidGeoRect(rect))
                return BadRequest();

            return Ok(Users.GetActiveUsersIn(rect.Intersects).Select((user) => user.LastKnownLocation));
        }

        // GET api/data/pending-ride-request-ids
        [HttpGet("pending-ride-request-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetPendingRideRequestIds([FromQuery]string userId)
        {
            User user = await Users.GetUser(userId);

            if (user == null)
                return NotFound();

            return new ActionResult<IEnumerable<string>>(user.PendingRideRequestIds);
        }

        // GET api/data/pending-ride-offer-ids
        [HttpGet("pending-ride-offer-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetPendingRideOfferIds([FromQuery]string userId)
        {
            User user = await Users.GetUser(userId);

            if (user == null)
                return NotFound();

            return new ActionResult<IEnumerable<string>>(user.PendingRideOfferIds);
        }

        // GET api/data/pending-ride-ids
        [HttpGet("pending-ride-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetPendingRideIds([FromQuery]string userId)
        {
            User user = await Users.GetUser(userId);

            if (user == null)
                return NotFound();

            return new ActionResult<IEnumerable<string>>(user.PendingRideIds);
        }

        // GET api/data/active-ride-ids
        [HttpGet("active-ride-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveRideIds([FromQuery]string userId)
        {
            User user = await Users.GetUser(userId);

            if (user == null)
                return NotFound();

            return new ActionResult<IEnumerable<string>>(user.ActiveRideIds);
        }

        // POST api/data/userinfoupdate
        [HttpPost("userinfoupdate")]
        public async Task<ActionResult> PostUserInfoUpdated([FromQuery]string userId)
        {
            if (await Users.UpdateUserIfLoaded(userId))
                return Ok();
            return NotFound();
        }
    }
}
