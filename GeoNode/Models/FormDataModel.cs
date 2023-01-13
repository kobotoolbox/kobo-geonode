using System.Text;

namespace GeoNode.Models
{
    public class FormDataModel
    {
        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public Dictionary<string, string> Attributes { get;set; } = new Dictionary<string, string>();

        public string GeoNodeAttributes
        {
            get
            {
                StringBuilder attrs = new StringBuilder();
                foreach (var attr in Attributes)
                {
                    attrs.Append(string.Format("<geonode:{0}>{1}</geonode:{0}>", attr.Key, attr.Value));
                }
                return attrs.ToString();
            }
        }
    }
}
