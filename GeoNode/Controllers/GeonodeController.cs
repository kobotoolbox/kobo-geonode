using GeoNode.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GeoNode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeonodeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        //  constructor that takes an IConfiguration object as input to get env variables from local or server
                public GeonodeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost]
        // This is an HTTP POST endpoint that receives data from the header and body of the request.
        public async Task<IActionResult> Post([FromHeader(Name = "geo-layer")] string geoLayer,
            [FromHeader(Name = "geo-token")] string geoToken,
            [FromBody] JsonElement data)
        {
            try
            {
                // Get form data from the received JSON data.
                 var formData = FormDataHelper.GetFormData(data);

                // Create a new instance of HttpClient to send a POST request.
                using var http = new HttpClient();

                // Get the base server URL and port from the configuration.
                var baseServerUrl = _configuration["GEO_SERVER_URL"];
                var serverPort = _configuration["GEO_SERVER_PORT"];
                UriBuilder builder = new UriBuilder(baseServerUrl);
                builder.Port = int.Parse(serverPort);
                var serverUrl = builder.ToString();

                // Add the access token to the URL.
                var serverUrlWithToken = serverUrl +"/geoserver/ows?access_token=" + geoToken;

                // Create a new request with the POST method and the URL with the token.
                var request = new HttpRequestMessage(HttpMethod.Post, serverUrlWithToken);

                // Get the base URI from the server URL.
                var uri = new Uri(serverUrl);
                var baseUri = uri.GetLeftPart(System.UriPartial.Authority);

                // Create the XML body for the request.
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

                // Get the modified base URI.
                var b = baseUri?.Substring(0, baseUri.LastIndexOf(":")) + "/";

                // Format the XML body with the form data and other parameters.
                var bbody = string.Format(body, formData.Longitude, formData.Latitude, formData.GeoNodeAttributes, geoLayer, b);

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
