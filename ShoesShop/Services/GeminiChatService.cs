using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ShoesShop.Services
{
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        }

        public async Task<string> GetBraceletSuggestionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "Hệ thống chưa được cấu hình khóa API cho Gemini.";
            }

            // Model name changes over time; gemini-2.5-flash is a current generateContent-capable model.
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var systemPrompt =
                "Bạn là trợ lý tư vấn vòng tay phong thủy cho shop Ếch thắt vòng. " +
                "Nhiệm vụ của bạn: gợi ý size vòng tay phù hợp (chu vi cổ tay, size S/M/L/XL và cm), " +
                "gợi ý loại vòng, màu sắc, chất liệu dựa trên cung hoàng đạo, tuổi, ngày tháng năm sinh, giới tính. " +
                "Trả lời ngắn gọn, rõ ràng, dùng tiếng Việt thân thiện, dạng gạch đầu dòng dễ đọc.";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt },
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            // Nếu có lỗi từ API, cố gắng trả nội dung lỗi để dễ debug
            if (!response.IsSuccessStatusCode)
            {
                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var msg = errorElement.GetProperty("message").GetString();
                    return msg ?? "Gemini trả về lỗi, vui lòng kiểm tra cấu hình API.";
                }

                return "Gemini trả về lỗi, vui lòng kiểm tra lại API key hoặc cấu hình.";
            }

            // Gemini response: candidates[0].content.parts[0].text
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    return text ?? "Không nhận được nội dung gợi ý.";
                }
            }

            return "Không nhận được nội dung gợi ý.";
        }
    }
}

