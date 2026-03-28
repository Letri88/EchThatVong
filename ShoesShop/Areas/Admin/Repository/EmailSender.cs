using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;

namespace ShoesShop.Areas.Admin.Repository
{
	public class EmailSender : IEmailSender
	{
        private readonly IConfiguration _configuration;
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

		public async Task SendEmailAsync(string email, string subject, string message, string emailType = "default")
		{
            var emailSettings = _configuration.GetSection("EmailSettings");
            var mail = emailSettings["Email"];
            var pw = emailSettings["Password"];
            var host = emailSettings["Host"];
            var port = int.Parse(emailSettings["Port"]);

			var client = new SmtpClient(host, port)
			{
				EnableSsl = true,
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(mail, pw)
			};

			var mailMessage = new MailMessage();
			mailMessage.From = new MailAddress(mail, "ShoesShop");
			mailMessage.To.Add(email);
			mailMessage.Subject = subject;
			mailMessage.IsBodyHtml = true;

			// Chọn ảnh & nội dung tuỳ theo loại email
			string imagePath;
			string customMessage;

            // Kiểm tra thư mục ảnh, nếu không có thì dùng ảnh mặc định hoặc bỏ qua ảnh
            var webRoot = Directory.GetCurrentDirectory();
            
			switch (emailType.ToLower())
			{
				case "order":
					imagePath = Path.Combine(webRoot, "wwwroot", "images", "thankyou.jpg");
					customMessage = $"<h3>{message}</h3><p>Cảm ơn bạn đã mua hàng tại <strong>ShoesShop</strong>!</p>";
					break;

				case "reset":
					imagePath = Path.Combine(webRoot, "wwwroot", "images", "reset_password.jpg");
					customMessage = $"<h3>{message}</h3><p>Vui lòng nhấn vào link bên dưới để đặt lại mật khẩu của bạn.</p>";
					break;

				default:
					imagePath = Path.Combine(webRoot, "wwwroot", "images", "default.jpg");
					customMessage = $"<h3>{message}</h3>";
					break;
			}

            // Tạo nội dung HTML cơ bản
            string htmlBody = $@"
                <html>
                <body style='font-family:Arial;'>
                    {customMessage}
                    <br/>
                    <p>Trân trọng,<br><strong>Đội ngũ ShoesShop</strong></p>
                </body>
                </html>
            ";

            // Nếu file ảnh tồn tại thì mới attach
            if (File.Exists(imagePath))
            {
			    var inlineLogo = new LinkedResource(imagePath, MediaTypeNames.Image.Jpeg);
			    inlineLogo.ContentId = "mainImage";
                htmlBody = htmlBody.Replace("<br/>", $@"<br/><img src=""cid:mainImage"" alt=""Image"" width=""400""/><br/>");

			    var altView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
			    altView.LinkedResources.Add(inlineLogo);
			    mailMessage.AlternateViews.Add(altView);
            }
            else
            {
                mailMessage.Body = htmlBody;
            }

			await client.SendMailAsync(mailMessage);
		}

	}
}
