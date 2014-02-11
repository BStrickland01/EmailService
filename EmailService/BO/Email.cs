using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmailService.BO
{
    public class Email
    {
        public string To { get; set; }
        public string From { get; set; }
        /// <summary>
        /// The alias that shows up in the 'From' field
        /// </summary>
        public string Alias { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        /// <summary>
        /// Carbon Copy list
        /// </summary>
        public string CCList { get; set; }
        /// <summary>
        /// Blind Carbon Copy list
        /// </summary>
        public string BlindCCList { get; set; }
        public string Subject { get; set; }
        public string Body { get; private set; }
        public string Image { get; set; }
        /// <summary>
        /// Used to create the preliminary body of the email -- Will later be transformed into the actual body with more details
        /// </summary>
        public string TemplateContent { get; set; }
        public bool MMS { get; set; }
        public bool SendLink { get; set; }
        public bool AttachImage { get; set; }

        public void SetEmailBody(string templateContent)
        {
            Body = templateContent;
        }
    }
}
