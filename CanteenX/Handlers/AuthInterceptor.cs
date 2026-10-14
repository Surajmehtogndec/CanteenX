using CanteenX.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CanteenX.Handlers
{
    public class AuthInterceptor : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;


        public AuthInterceptor(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            // 1. Agar request mein koi HttpContext nahi hai, toh seedha bhej dein
            if (context == null) return await base.SendAsync(request, cancellationToken);

            // 2. Cookie se JWT nikal kar Authorization header mein lagayein
            var jwtToken = context.Request.Cookies["jwt_token"];
            if (!string.IsNullOrEmpty(jwtToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            }

            // 3. API ko request bhejein
            var response = await base.SendAsync(request, cancellationToken);

            // 4. Agar API bole ki token expire ho gaya hai (401 Unauthorized)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshToken = context.Request.Cookies["refresh_token"];

                if (!string.IsNullOrEmpty(refreshToken) && !string.IsNullOrEmpty(jwtToken))
                {
                    // 5. Silently naya token lene ki koshish karein
                    var newTokens = await TryRefreshTokenAsync(jwtToken, refreshToken);

                    if (newTokens != null)
                    {
                        // 6. Naye tokens ko Cookies mein update karein
                        UpdateCookies(context, newTokens.AccessToken, newTokens.RefreshToken);

                        // 7. Purani request ko naye token ke sath CLONE karke dobara bhejein
                        var clonedRequest = await CloneHttpRequestAsync(request);
                        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);

                        response = await base.SendAsync(clonedRequest, cancellationToken);
                    }
                    else
                    {
                        // Agar Refresh Token bhi expire/invalid ho gaya hai, toh user ko force logout kar dein
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Response.Cookies.Delete("jwt_token");
                        context.Response.Cookies.Delete("refresh_token");
                    }
                }
            }

            return response;
        }


        // ==========================================
        // HELPER METHODS
        // ==========================================

        private async Task<AuthResponseDto?> TryRefreshTokenAsync(string jwtToken, string refreshToken)
        {
            // API Base URL appsettings.json se nikalein
            var apiUrl = _configuration["AppSettings:ApiBaseUrl"] ?? "https://localhost:7150/";
            using var client = new HttpClient { BaseAddress = new Uri(apiUrl) };

            var payload = JsonSerializer.Serialize(new { AccessToken = jwtToken, RefreshToken = refreshToken });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // API ke refresh-token endpoint ko call karein
            var response = await client.PostAsync("api/Auth/refresh-token", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AuthResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null && result.Success) return result;
            }

            return null;
        }

        private void UpdateCookies(HttpContext context, string newJwt, string newRefreshToken)
        {
            context.Response.Cookies.Append("jwt_token", newJwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            context.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }

        // HttpClient ko same request object dobara bhejne se problem hoti hai, isliye request ko clone karna zaroori hai
        private async Task<HttpRequestMessage> CloneHttpRequestAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri)
            {
                Version = req.Version
            };

            if (req.Content != null)
            {
                var bytes = await req.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(bytes);
                foreach (var h in req.Content.Headers) clone.Content.Headers.Add(h.Key, h.Value);
            }

            foreach (var prop in req.Options) clone.Options.Set(new HttpRequestOptionsKey<object>(prop.Key), prop.Value);
            foreach (var header in req.Headers) clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }


    }
}
