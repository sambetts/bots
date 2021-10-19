using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrainingOnboarding.Models
{

    public class CourseContact : BaseSPItem
    {

        public CourseContact(ListItem courseItem) : base(courseItem)
        {
            this.Name = courseItem.Fields.AdditionalData["Title"]?.ToString();
            this.Email = courseItem.Fields.AdditionalData.ContainsKey("EMail") ? courseItem.Fields.AdditionalData["EMail"]?.ToString() : string.Empty;
        }

        public string Email { get; set; }
        public string Name { get; set; }
    }

}
