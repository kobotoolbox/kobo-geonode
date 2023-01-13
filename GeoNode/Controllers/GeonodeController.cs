using GeoNode.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KoboMonday.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeonodeController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post([FromHeader(Name = "geo_api_url")] string geoNodeApiUrl,
            [FromHeader(Name = "geo_layer")] string geoLayer ,
            [FromBody] JsonElement data)
        {
            var formData = FormDataHelper.GetFormData(data);

            if (formData?.Attributes?.Count == 0)
            {
                return StatusCode(400, "empty attributes");
            }
            using var http = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Post, geoNodeApiUrl);

            var body =
              @"<wfs:Transaction service=""WFS"" version=""1.1.0"" xmlns:wfs=""http://www.opengis.net/wfs"" xmlns:gml=""http://www.opengis.net/gml"" xmlns:ogc=""http://www.opengis.net/ogc"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.opengis.net/wfs"" xmlns:geonode=""http://20.193.226.67/"">
                    <wfs:Insert>
                        <geonode:{3}>
                            {2}
                            <geonode:the_geom>
                                <gml:Point srsDimension = ""2"" srsName = ""EPSG:4326"">
                                    <gml:pos> {0} {1} </gml:pos>
                                </gml:Point>
                            </geonode:the_geom>
                        </geonode:{3}>
                    </wfs:Insert>
                </wfs:Transaction>";

            var bbody = string.Format(body, formData.Longitude, formData.Latitude, formData.GeoNodeAttributes , geoLayer);
            request.Content = new StringContent(bbody, Encoding.UTF8, "application/xml");

            var res = await http.SendAsync(request);

            // return the response from the geoserver api
            var rescont = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return StatusCode((int)res.StatusCode, rescont);
            }
            return Ok();
        }
    }
}
