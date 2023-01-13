using GeoNode.Models;
using System.Text.Json;

namespace GeoNode.Helpers
{
    public class FormDataHelper
    {
        public static FormDataModel GetFormData(JsonElement dataElement)
        {
            var formData = new FormDataModel();
            //var dataElement = data.GetProperty("data");
            var _geolocation = dataElement.GetProperty("_geolocation").EnumerateArray();
            formData.Latitude = float.Parse(_geolocation.First().ToString());
            formData.Longitude = float.Parse(_geolocation.Last().ToString());

            var props = dataElement.EnumerateObject();
            var attrs = props.Where(s => !s.Name.Contains("_geolocation")).ToList();
            foreach (var prop in attrs)
            {
                formData.Attributes.Add(prop.Name, prop.Value.ToString());
            }
            return formData;
        }
    }
}
