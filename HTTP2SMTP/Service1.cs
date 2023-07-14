using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;

namespace HTTP2SMTP
{
    public partial class Service1 : ServiceBase
    {
        private HttpListener listener = new HttpListener();
        private EventLog eventLog;

        public Service1()
        {
            InitializeComponent();
            // Set up event logging
            eventLog = new EventLog();
            if (!EventLog.SourceExists("Http2Smtp"))
            {
                EventLog.CreateEventSource("Http2Smtp", "Application");
            }
            eventLog.Source = "Http2Smtp";
            eventLog.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            listener.Prefixes.Add("http://localhost:8080/smtpservice"); // or your desired URL
            listener.Start();
            listener.BeginGetContext(OnContext, null);
            eventLog.WriteEntry("Service started.");
        }


        protected override void OnStop()
        {
            listener.Stop();
            eventLog.WriteEntry("Service stopped.");
        }

        private void OnContext(IAsyncResult ar)
        {
            var context = listener.EndGetContext(ar);
            listener.BeginGetContext(OnContext, null); // Start listening for next client

            // Read request data:
            var requestStream = context.Request.InputStream;
            var reader = new StreamReader(requestStream);
            var json = reader.ReadToEnd();

            try
            {
                EmailData data = JsonConvert.DeserializeObject<EmailData>(json);

                // A simple validation could be to check if the fields are not null or empty.
                if (string.IsNullOrEmpty(data.To) ||
                    string.IsNullOrEmpty(data.From) ||
                    string.IsNullOrEmpty(data.Body) ||
                    string.IsNullOrEmpty(data.Subject))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    eventLog.WriteEntry($"Invalid request received.", EventLogEntryType.Warning);
                    return;
                }

                SendEmail(data);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Close();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
                // You could use the eventLog here to log the exception details
                eventLog.WriteEntry($"Error while sending email: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void SendEmail(EmailData data)
        {
            SmtpClient client = new SmtpClient("smtp.server.com");
            MailMessage mailMessage = new MailMessage();

            mailMessage.From = new MailAddress(data.From);

            // Add multiple recipients to the 'to' field
            foreach (string to in data.To.Split(';'))
            {
                mailMessage.To.Add(to.Trim());
            }

            // Add multiple recipients to the 'cc' field
            if (!string.IsNullOrEmpty(data.Cc))
            {
                foreach (string cc in data.Cc.Split(';'))
                {
                    mailMessage.CC.Add(cc.Trim());
                }
            }

            // Add multiple recipients to the 'bcc' field
            if (!string.IsNullOrEmpty(data.Bcc))
            {
                foreach (string bcc in data.Bcc.Split(';'))
                {
                    mailMessage.Bcc.Add(bcc.Trim());
                }
            }

            mailMessage.Subject = data.Subject;

            // Set the body
            mailMessage.Body = data.Body;
            // Specify the body is in HTML
            mailMessage.IsBodyHtml = true;

            // Set email priority
            switch (data.Priority.ToLower())
            {
                case "high":
                    mailMessage.Priority = MailPriority.High;
                    break;
                case "low":
                    mailMessage.Priority = MailPriority.Low;
                    break;
                default:
                    mailMessage.Priority = MailPriority.Normal;
                    break;
            }

            client.Send(mailMessage);
        }

        public class EmailData
        {
            public string To { get; set; }
            public string From { get; set; }
            public string Cc { get; set; }
            public string Bcc { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
            public string Priority { get; set; }
        }
    }

}
