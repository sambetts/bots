using Microsoft.Graph;

namespace DigitalTrainingAssistant.Models
{

    public class SiteUser : BaseSPItem
    {
        public SiteUser() { }
        public SiteUser(ListItem courseItem) : base(courseItem)
        {
            this.Name = courseItem.Fields.AdditionalData["Title"]?.ToString();
            this.Email = courseItem.Fields.AdditionalData.ContainsKey("EMail") ? courseItem.Fields.AdditionalData["EMail"]?.ToString() : string.Empty;
        }

        public string Email { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{this.Name} ({this.Email})";
        }
    }

}
