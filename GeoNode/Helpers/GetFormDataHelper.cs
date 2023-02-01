using GeoNode.Models;
using System.Text.Json;



    namespace GeoNode.Helpers
    {
    public class FormDataHelper
    {
        // This method parses JSON data from a JsonElement object and returns a FormDataModel object

        public static FormDataModel GetFormData(JsonElement dataElement)
        {
            // Initialize a new FormDataModel object
            var formData = new FormDataModel();

            // Get the "_geolocation" property from the JsonElement object
            var _geolocation = dataElement.GetProperty("_geolocation").EnumerateArray();
            // Set the Latitude and Longitude properties of the FormDataModel object
            formData.Latitude = float.Parse(_geolocation.First().ToString());
            formData.Longitude = float.Parse(_geolocation.Last().ToString());

            // Get all properties from the JsonElement object as an enumerable collection of JsonProperty objects
            var props = dataElement.EnumerateObject();
            // Filter out the "_geolocation" property and convert the resulting list to a list of JsonProperty objects
            var attrs = props.Where(s => !s.Name.Contains("_geolocation")).ToList();
            // Iterate over the list of remaining properties

            foreach (var attr in attrs)
            {
                // Get the attribute name after it has been processed by the AttrNameHandler method

                var attrName = AttrNameHandler(attr.Name);
                // Add the attribute name and its value to the Attributes dictionary of the FormDataModel object

                formData.Attributes.Add(attrName, attr.Value.ToString());
            }
            // Return the FormDataModel object

            return formData;
        }

        // This method handles the processing of attribute names

        private static string AttrNameHandler(string name)
        {
            // Initialize a newName variable with the original name

            string newName = name;
            // If the name contains a "/" character, replace it with "__" (this especially for grouping)

            if (name.Contains("/"))
            {
                newName = name.Replace("/", "__");
            }
            // Return the new name in all lowercase
            return newName.ToLower();
        }
    }
}

