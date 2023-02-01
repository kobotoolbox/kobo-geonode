using GeoNode.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeoNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeonodeController : ControllerBase
    {
        [HttpPost]
        // This is a method for handling HTTP POST requests

        public async Task<IActionResult> Post([FromHeader(Name = "geo_api_url")] string geoNodeApiUrl,
            [FromHeader(Name = "geo-layer")] string geoLayer,
            [FromBody] JsonElement data)
        {
            try
            {
                // Get the form data from the input JSON data by using helper
                var formData = FormDataHelper.GetFormData(data);

                // Create an HTTP client instance
                using var http = new HttpClient();
               
                // Create a new HTTP POST request using the geoNodeApiUrl
                var request = new HttpRequestMessage(HttpMethod.Post, geoNodeApiUrl);

                // Get the base URI from the geoNodeApiUrl
                var uri = new Uri(geoNodeApiUrl);
                var baseUri = uri.GetLeftPart(System.UriPartial.Authority);
                
                // Define the XML request dynamic body
                var body =
                  @"<wfs:Transaction service=""WFS"" version=""1.1.0"" xmlns:wfs=""http://www.opengis.net/wfs"" xmlns:gml=""http://www.opengis.net/gml"" xmlns:ogc=""http://www.opengis.net/ogc"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.opengis.net/wfs"" xmlns:geonode=""{4}"">
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

                // Get the server URL from the base URI
                var ServerURL = baseUri?.Substring(0, baseUri.LastIndexOf(":")) + "/";

                // Format the XML body with the form data and server URL
                var bbody = string.Format(body, formData.Longitude, formData.Latitude, formData.GeoNodeAttributes, geoLayer, ServerURL);

                // Set the content of the request to the formatted XML body
                request.Content = new StringContent(bbody, Encoding.UTF8, "application/xml");

                // Send the request to the geoNode API
                var res = await http.SendAsync(request);

                // return the response from the geoserver api
                var rescont = await res.Content.ReadAsStringAsync();
                
                // Return an error status code and the response if the request was not successful
                if (!res.IsSuccessStatusCode)
                {
                    return StatusCode((int)res.StatusCode, rescont);
                }

                // Return an OK status code if the request was successful
                return Ok();
            }

            // Return a Bad Request status code and the exception message if an error occurs
           catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }

        }
    }

}