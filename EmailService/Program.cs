using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.Mime;
using System.Configuration;
using EmailService.BO;
using System.IO;
//using PictureU.Utilities;
//using PictureU.Utilities.BusinessObjects;

namespace EmailService
{
    public class Program
    {
        static string fileLoc = Environment.CurrentDirectory + "\\" + "EmailScheduler.txt";
        //static System.IO.StreamWriter file = new System.IO.StreamWriter(fileLoc);

        static void Main(string[] args)
        {
            SetupEmailBlastToEventUsers();
        }

        private static void SetupEmailBlastToEventUsers()
        {            
            List<PUConsumerData> consumerDataInfo;
            PUEvent eventInfo;
            using (var dc = new CoreDBDataContext())
            {
                consumerDataInfo = dc.PUConsumerDatas.Where(c => c.ImageSent == false && c.ImagesUploaded == true && c.Email.Trim().Length > 0 && c.OnsiteServerFlag == true).ToList();

                if (consumerDataInfo != null && consumerDataInfo.Count > 0)
                {
                    if (!File.Exists(fileLoc))
                    {
                        File.WriteAllText(fileLoc, "Text File Created");                        
                    }
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileLoc, true))
                    {
                        file.WriteLine();
                        file.WriteLine();
                        file.WriteLine("--------------------------------------------");
                        file.WriteLine(string.Format("DateTime: {0} -- Run contains {1} applicable records", DateTime.Now, consumerDataInfo.Count()));
                        file.WriteLine("--------------------------------------------");                        

                        foreach (var item in consumerDataInfo)
                        {
                            eventInfo = dc.PUEvents.FirstOrDefault(e => e.EventID == item.EventID);
                            var sent = SendEmail(dc, eventInfo, item);
                            if (sent)
                            {
                                file.WriteLine(string.Format("Email/MMS sent to '{0}' -- EventID = '{1}' -- ConsumerDataID = '{2}' at '{3}'", item.Email, item.EventID, item.ConsumerDataID, DateTime.Now));
                            }
                            else
                            {
                                file.WriteLine(string.Format("Email/MMS msg failed for '{0}' -- EventID = '{1}' -- ConsumerDataID = '{2}' at '{3}'", item.Email, item.EventID, item.ConsumerDataID, DateTime.Now));
                            }
                        }
                    }
                }
            }
        }

        private static bool SendEmail(CoreDBDataContext dc, PUEvent eventInfo, PUConsumerData consumerInfo, bool MMS = false, bool useConsumerDataID = false)
        {
            if (!string.IsNullOrWhiteSpace(consumerInfo.Email) || MMS)
            {
                var email = new Email();
                var imageDetails = new ImageDetails();
                var tourInfo = dc.PUTours.FirstOrDefault(t => t.TourID == eventInfo.TourID);
                var programInfo = dc.PUPrograms.FirstOrDefault(p => p.ProgramID == tourInfo.ProgramID);
                var brandInfo = dc.PUBrands.FirstOrDefault(b => b.BrandID == programInfo.BrandID);
                var brandName = brandInfo.Name;

                if (!string.IsNullOrWhiteSpace(brandName))
                {
                    email.Alias = brandName;
                }
                else
                {
                    email.Alias = "PictureU.com";
                }

                email.SendLink = (bool)eventInfo.SendLink;
                email.AttachImage = (bool)eventInfo.AttachImages;
                email.MMS = MMS;
                email.FirstName = consumerInfo.FirstName;
                email.LastName = consumerInfo.LastName;
                imageDetails.CompressedName = consumerInfo.CompressedImageName;
                imageDetails.Height = 250;
                email.To = consumerInfo.Email;
                email.From = eventInfo.EmailFrom;

                if (email.MMS) //Text Messaging
                {
                    email.TemplateContent = eventInfo.TextBody;
                    email.Subject = eventInfo.TextSubject;
                }
                else //Emailing
                {
                    email.TemplateContent = eventInfo.EmailBody;
                    email.Subject = eventInfo.EmailSubject;
                }

                var sent = SendNonTemplatedEmail(email, imageDetails, consumerInfo.SecureCode, consumerInfo.ConsumerDataID.ToString(), consumerInfo.EventID.ToString());

                if (sent)
                {
                    if (email.MMS)
                    {
                        consumerInfo.MMSSent = true;
                    }
                    else
                    {
                        consumerInfo.ImageSent = true;
                    }

                    dc.SubmitChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
                
        public static bool SendNonTemplatedEmail(Email email, ImageDetails imageDetails, string secureCode, string consumerDataId, string eventId)
        {
            var appSettings = new AppSettingsReader();
            string consumerURL;
            string emailBody = "";
            var fbURL = appSettings.GetValue("FacebookReturnURL", typeof(string)).ToString();

            email.TemplateContent = email.TemplateContent.Replace("#firstname#", email.FirstName);
            email.TemplateContent = email.TemplateContent.Replace("#lastname#", email.LastName);
            email.TemplateContent = email.TemplateContent.Replace("#securecode#", secureCode);

            email.TemplateContent = email.TemplateContent.Replace("#image#", string.Format("<img height='{0}' src=\"{1}{2}/{3}\" />", imageDetails.Height, appSettings.GetValue("PictureUProductionWebPath", typeof(string)).ToString(), eventId, imageDetails.CompressedName));
            email.TemplateContent = email.TemplateContent.Replace("#consumerdataid#", consumerDataId);
            email.TemplateContent = email.TemplateContent.Replace("#micrositeurl#", string.Format("{0}?ConsumerDataID={1}", appSettings.GetValue("FacebookReturnURL", typeof(string)).ToString(), consumerDataId));

            if (email.SendLink)
            {
                if (email.MMS)
                {
                    consumerURL = string.Format("{0}?ConsumerDataID={1}", appSettings.GetValue("FacebookReturnURL", typeof(string)).ToString(), consumerDataId);
                }
                else
                {
                    consumerURL = string.Format("<a href='{0}?ConsumerDataID={1}'>Click Here for your Photo!</a>", appSettings.GetValue("FacebookReturnURL", typeof(string)).ToString(), consumerDataId);
                }

                if (email.TemplateContent.Contains("#imagelink#") && !email.MMS)
                {
                    emailBody = email.TemplateContent.Replace("#imagelink#", consumerURL).ToString();
                }
                else if (!email.MMS)
                {
                    emailBody = email.TemplateContent + "<br><br>" + consumerURL;
                }

                if (email.MMS && email.TemplateContent.Contains("#imagelink#"))
                {
                    emailBody = email.TemplateContent.Replace("#imagelink#", consumerURL);
                }
                else if (email.MMS)
                {
                    emailBody = consumerURL + " " + emailBody;
                }
                email.SetEmailBody(emailBody);
            }
            else
            {
                email.SetEmailBody(email.TemplateContent);
            }

            if (email.AttachImage || email.MMS)
            {
                imageDetails.FTPImageURL = string.Format("{0}{1}/{2}", appSettings.GetValue("PictureUImagesPath", typeof(string)).ToString(), eventId, imageDetails.CompressedName);
            }

            try
            {
                var emailHost = new AppSettingsReader().GetValue("Email_Host", typeof(string)).ToString();
                var emailUserName = new AppSettingsReader().GetValue("Email_UserName", typeof(string)).ToString();
                var emailPassword = new AppSettingsReader().GetValue("Email_Password", typeof(string)).ToString();

                SmtpClient mySmtpClient = new SmtpClient(emailHost);
                StringBuilder fileBuilder = new StringBuilder();

                mySmtpClient.UseDefaultCredentials = false;
                var basicAuthenticationInfo = new System.Net.NetworkCredential(emailUserName, emailPassword);
                mySmtpClient.Credentials = basicAuthenticationInfo;

                MailAddress from = new MailAddress(email.From);
                MailAddress to = new MailAddress(email.To, "");
                MailMessage myMail = new System.Net.Mail.MailMessage(from, to);

                if (email.AttachImage)
                {
                    byte[] imageData;
                    using (System.Net.WebClient client = new System.Net.WebClient())
                    {
                        try
                        {
                            if (imageDetails.FTPImageURL != null)
                            {
                                imageData = client.DownloadData(imageDetails.FTPImageURL);
                            }
                            else
                            {
                                imageData = client.DownloadData(ConfigurationSettings.AppSettings["PictureUImagesPath"] + eventId + "/" + imageDetails.CompressedName);
                            }
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }

                    var test = new System.IO.MemoryStream((imageData));
                    var attachment = new Attachment(test, new ContentType() { MediaType = MediaTypeNames.Image.Jpeg });
                    myMail.Attachments.Add(attachment);
                }

                myMail.Subject = email.Subject;
                myMail.SubjectEncoding = System.Text.Encoding.UTF8;
                myMail.Body = email.Body;
                myMail.BodyEncoding = System.Text.Encoding.UTF8;
                myMail.IsBodyHtml = true;

                try
                {
                    mySmtpClient.Send(myMail);
                }
                catch (Exception e3)
                {
                    throw e3;
                }
            }

            catch (SmtpException ex)
            {
                throw new ApplicationException
                  ("SmtpException has occured: " + ex.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
    }
}
