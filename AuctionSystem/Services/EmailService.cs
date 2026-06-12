namespace AuctionSystem.Services
{
	public interface IEmailService
	{
		Task SendOtpEmailAsync(string email, string otp);
		Task SendWinnerEmailAsync(string email, string name, string auctionTitle, decimal finalPrice);
		Task SendAuctionEndedEmailAsync(string email, string name, string auctionTitle, decimal finalPrice, string? winnerName);
		Task SendOutbidEmailAsync(string email, string name, string auctionTitle, decimal newPrice);
	}

	public class EmailService : IEmailService
	{
		private readonly IConfiguration _config;
		private readonly ILogger<EmailService> _logger;

		public EmailService(IConfiguration config, ILogger<EmailService> logger)
		{
			_config = config;
			_logger = logger;
		}

		public async Task SendOtpEmailAsync(string email, string otp)
		{
			var subject = "Mã xác minh đăng ký AuctionHub";
			var body = $@"
<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family:Inter,sans-serif;background:#faf9f5;margin:0;padding:0;'>
  <div style='max-width:480px;margin:40px auto;background:#fff;border-radius:12px;border:1px solid #e6dfd8;overflow:hidden;'>
    <div style='background:#181715;padding:24px 32px;display:flex;align-items:center;gap:10px;'>
      <svg width='16' height='16' viewBox='0 0 14 14' fill='none'>
        <path d='M7 1v12M1 7h12M2.5 2.5l9 9M11.5 2.5l-9 9' stroke='#cc785c' stroke-width='1.5' stroke-linecap='round'/>
      </svg>
      <span style='color:#faf9f5;font-size:16px;font-weight:500;'>AuctionHub</span>
    </div>
    <div style='padding:36px 32px;text-align:center;'>
      <div style='font-size:14px;color:#6c6a64;margin-bottom:8px;'>Mã xác minh của bạn là</div>
      <div style='font-size:48px;font-weight:700;letter-spacing:12px;color:#141413;font-family:monospace;margin:16px 0;'>{otp}</div>
      <div style='background:#faeeda;border-radius:8px;padding:12px 16px;margin-bottom:24px;'>
        <p style='font-size:13px;color:#854f0b;margin:0;'>
          ⏱ Mã có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này cho bất kỳ ai.
        </p>
      </div>
      <p style='font-size:13px;color:#8e8b82;margin:0;'>
        Nếu bạn không yêu cầu đăng ký, vui lòng bỏ qua email này.
      </p>
    </div>
    <div style='background:#f5f0e8;padding:16px 32px;text-align:center;'>
      <p style='color:#8e8b82;font-size:12px;margin:0;'>© 2026 AuctionHub</p>
    </div>
  </div>
</body></html>";
			await SendEmailAsync(email, subject, body);
		}

		public async Task SendWinnerEmailAsync(string email, string name, string auctionTitle, decimal finalPrice)
		{
			var subject = $"🏆 Chúc mừng! Bạn đã thắng: {auctionTitle}";
			var body = $@"
<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family:Inter,sans-serif;background:#faf9f5;margin:0;padding:0;'>
  <div style='max-width:560px;margin:40px auto;background:#fff;border-radius:12px;border:1px solid #e6dfd8;overflow:hidden;'>
    <div style='background:#181715;padding:24px 32px;'>
      <span style='color:#faf9f5;font-size:16px;font-weight:500;'>AuctionHub</span>
    </div>
    <div style='padding:36px 32px;'>
      <div style='text-align:center;margin-bottom:28px;'>
        <div style='font-size:48px;margin-bottom:12px;'>🏆</div>
        <h2 style='font-size:22px;font-weight:600;color:#141413;margin:0 0 8px;'>Chúc mừng, {name}!</h2>
        <p style='color:#6c6a64;font-size:15px;margin:0;'>Bạn đã thắng phiên đấu giá</p>
      </div>
      <div style='background:#f5f0e8;border-radius:10px;padding:20px 24px;'>
        <p style='font-size:12px;font-weight:500;color:#8e8b82;text-transform:uppercase;letter-spacing:1px;margin:0 0 4px;'>Sản phẩm</p>
        <p style='font-size:18px;font-weight:600;color:#141413;margin:0 0 16px;'>{auctionTitle}</p>
        <p style='font-size:12px;font-weight:500;color:#8e8b82;text-transform:uppercase;letter-spacing:1px;margin:0 0 4px;'>Giá thắng</p>
        <p style='font-size:28px;font-weight:700;color:#cc785c;margin:0;'>{finalPrice:N0} VNĐ</p>
      </div>
    </div>
    <div style='background:#f5f0e8;padding:16px 32px;text-align:center;'>
      <p style='color:#8e8b82;font-size:12px;margin:0;'>© 2026 AuctionHub</p>
    </div>
  </div>
</body></html>";
			await SendEmailAsync(email, subject, body);
		}

		public async Task SendAuctionEndedEmailAsync(string email, string name, string auctionTitle, decimal finalPrice, string? winnerName)
		{
			var subject = $"Phiên đấu giá kết thúc: {auctionTitle}";
			var winnerInfo = winnerName != null
				? $"<p>Người thắng: <strong>{winnerName}</strong></p>"
				: "<p>Phiên kết thúc không có người thắng.</p>";
			var body = $@"
<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family:Inter,sans-serif;background:#faf9f5;'>
  <div style='max-width:560px;margin:40px auto;background:#fff;border-radius:12px;border:1px solid #e6dfd8;overflow:hidden;'>
    <div style='background:#181715;padding:24px 32px;'><span style='color:#faf9f5;font-size:16px;'>AuctionHub</span></div>
    <div style='padding:36px 32px;'>
      <h2 style='font-size:20px;font-weight:600;color:#141413;margin:0 0 16px;'>Phiên đấu giá đã kết thúc</h2>
      <div style='background:#f5f0e8;border-radius:10px;padding:20px 24px;margin-bottom:20px;'>
        <p style='font-size:16px;font-weight:600;color:#141413;margin:0 0 8px;'>{auctionTitle}</p>
        <p style='font-size:24px;font-weight:700;color:#cc785c;margin:0;'>{finalPrice:N0} VNĐ</p>
      </div>
      {winnerInfo}
    </div>
    <div style='background:#f5f0e8;padding:16px 32px;text-align:center;'><p style='color:#8e8b82;font-size:12px;margin:0;'>© 2026 AuctionHub</p></div>
  </div>
</body></html>";
			await SendEmailAsync(email, subject, body);
		}

		public async Task SendOutbidEmailAsync(string email, string name, string auctionTitle, decimal newPrice)
		{
			var subject = $"⚡ Bạn vừa bị vượt giá: {auctionTitle}";
			var body = $@"
<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family:Inter,sans-serif;background:#faf9f5;'>
  <div style='max-width:560px;margin:40px auto;background:#fff;border-radius:12px;border:1px solid #e6dfd8;overflow:hidden;'>
    <div style='background:#181715;padding:24px 32px;'><span style='color:#faf9f5;font-size:16px;'>AuctionHub</span></div>
    <div style='padding:36px 32px;'>
      <h2 style='font-size:20px;font-weight:600;color:#141413;margin:0 0 8px;'>Bạn vừa bị vượt giá!</h2>
      <p style='color:#6c6a64;font-size:15px;margin:0 0 20px;'>Có người đã đặt giá cao hơn bạn trong phiên <strong>{auctionTitle}</strong></p>
      <div style='background:#f5f0e8;border-radius:10px;padding:20px 24px;margin-bottom:20px;'>
        <p style='font-size:12px;font-weight:500;color:#8e8b82;text-transform:uppercase;margin:0 0 4px;'>Giá hiện tại</p>
        <p style='font-size:28px;font-weight:700;color:#cc785c;margin:0;'>{newPrice:N0} VNĐ</p>
      </div>
    </div>
    <div style='background:#f5f0e8;padding:16px 32px;text-align:center;'><p style='color:#8e8b82;font-size:12px;margin:0;'>© 2026 AuctionHub</p></div>
  </div>
</body></html>";
			await SendEmailAsync(email, subject, body);
		}

		private async Task SendEmailAsync(string to, string subject, string htmlBody)
		{
			try
			{
				var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
				var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
				var smtpUser = _config["Email:Username"] ?? "";
				var smtpPass = _config["Email:Password"] ?? "";
				var fromEmail = _config["Email:From"] ?? smtpUser;
				var fromName = _config["Email:FromName"] ?? "AuctionHub";

				using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort)
				{
					EnableSsl = true,
					Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass)
				};

				var msg = new System.Net.Mail.MailMessage
				{
					From = new System.Net.Mail.MailAddress(fromEmail, fromName),
					Subject = subject,
					Body = htmlBody,
					IsBodyHtml = true
				};
				msg.To.Add(to);
				await client.SendMailAsync(msg);
				_logger.LogInformation($"Email sent to {to}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to send email to {to}");
			}
		}
	}
}
