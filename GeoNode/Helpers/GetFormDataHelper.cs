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
                foreach (var attr in attrs)
                {
                    var attrName = AttrNameHandler(attr.Name);
                    formData.Attributes.Add(attrName, attr.Value.ToString());
                }
                return formData;
            }

            private static string AttrNameHandler(string name)
            {
                string newName = name;
                if (name.Contains("/"))
                {
                    newName = name.Replace("/", "__");

                    if (name.Contains("_"))
                    {
                        newName = newName.Substring(0, newName.LastIndexOf("_"));
                    }
                }

                return newName.ToLower();
            }
        }
    }

