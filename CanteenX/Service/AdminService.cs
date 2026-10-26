using CanteenX.Models.Admin;
using CanteenX.Models.Auth;
using CanteenX.Service.Interface;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Http.Headers;

namespace CanteenX.Services
{
    public class AdminService : IAdminService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AdminService> _logger;
        private const string ApiClientName = "CanteenX.Api";

        public AdminService(IHttpClientFactory httpClientFactory, ILogger<AdminService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }
        public byte[] GenerateUsersPdf(List<ExportPdfDto> users)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // 1. HEADER
                    page.Header().PaddingBottom(20).Row(row =>
                    {
                        string basePath = System.IO.Directory.GetCurrentDirectory();
                        string canteenLogoPath = System.IO.Path.Combine(basePath, "wwwroot", "img", "CanteenXLogo.png");
                        string gndecLogoPath = System.IO.Path.Combine(basePath, "wwwroot", "img", "GNDEC.jpg");

                        // Left Logo
                        row.ConstantItem(70).Height(60).Element(imgContainer =>
                        {
                            if (System.IO.File.Exists(canteenLogoPath))
                            {
                                imgContainer.Image(canteenLogoPath).FitArea();
                            }
                            else
                            {
                                imgContainer.AlignCenter().AlignMiddle().Text("CanteenX").FontSize(10);
                            }
                        });

                        // Center Text
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().AlignCenter().Text("Guru Nanak Dev Engineering College Ludhiana")
                                .FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);

                            col.Item().AlignCenter().Text("CanteenX ")
                                .FontSize(14).FontColor(Colors.Red.Medium);

                            col.Item().AlignCenter().Text($"Generated on: {DateTime.Now:dd MMM yyyy hh:mm tt}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                        // Right Logo
                        row.ConstantItem(70).Height(80).Element(imgContainer =>
                        {
                            if (System.IO.File.Exists(gndecLogoPath))
                            {
                                imgContainer.Image(gndecLogoPath).FitArea();
                            }
                            else
                            {
                                imgContainer.AlignCenter().AlignMiddle().Text("GNDEC").FontSize(10);
                            }
                        });
                    }); // <-- YAHAN HEADER ROW CLOSE HONA CHAHIYE THA (Jo missing tha)

                    // 2. CONTENT (Table)
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); columns.RelativeColumn();
                            columns.RelativeColumn(); columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Name").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Email").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Role").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Status").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Joined").SemiBold();
                        });

                        foreach (var user in users)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(user.FullName ?? "_");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(user.Email ?? "_");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(user.Role ?? "_");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(user.Status ?? "_");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(user.JoinedDate ?? "_");
                        }
                    });

                    // 3. FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages();
                    });
                });
            }); // <-- DOCUMENT CREATE YAHAN CLOSE HOGA

            return document.GeneratePdf();
        }


        // ==========================================
        // API CALL LOGIC
        // ==========================================
        public async Task<UserProfileDto?> GetProfileDataAsync(string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(ApiClientName);
                var response = await client.GetAsync($"api/admin/loadprofiledata/{userId}", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiAdminResponseDto<UserProfileDto>>(cancellationToken: cancellationToken);
                    return apiResponse?.Data;
                }
                _logger.LogWarning("API Error: {StatusCode} while fetching profile.", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetProfileDataAsync.");
                return null;
            }
        }

        public async Task<List<CanteenStaffDto>> GetCanteenStaffAsync()
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var response = await client.GetAsync("api/admin/canteen-staff");

            if (!response.IsSuccessStatusCode) return new List<CanteenStaffDto>();

            var result = await response.Content.ReadFromJsonAsync<CanteenStaffResponseDto>();
            return result?.Data ?? new List<CanteenStaffDto>();
        }

        public async Task<AuthResponseDto?> RegisterCanteenStaffAsync(RegisterCanteensDto model)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            var response = await client.PostAsJsonAsync("api/admin/CanteenStaffRegister", new
            {
                model.FullName, model.Email, model.PhoneNumber, model.Password, model.ConfirmPassword
            });
            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            await client.PostAsJsonAsync("api/Auth/revoke-token", new { Token = refreshToken });
        }

        public async Task<ApiResponseDto?> AddCanteenAsync(AddCanteens dto)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var formdata = new MultipartFormDataContent();

            formdata.Add(new StringContent(dto.UserId), "UserId");
            formdata.Add(new StringContent(dto.Location ?? ""), "Location");
            formdata.Add(new StringContent(dto.Description ?? ""), "Description");
            formdata.Add(new StringContent(dto.IsActive.ToString()), "IsActive");

            if (dto.ImageUrl != null)
            {
                var streamContent = new StreamContent(dto.ImageUrl.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(dto.ImageUrl.ContentType);
                formdata.Add(streamContent, "ImageUrl", dto.ImageUrl.FileName);
            }

            var response = await client.PostAsync("api/admin/canteen", formdata);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ApiResponseDto>();
        }

        public async Task<HttpResponseMessage> UpdateProfileAsync(string userId, UserProfileDto model, string token)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/admin/updateprofiledata/{userId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { model.FullName, model.Email, model.PhoneNumber, model.IsActive });
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetUserDirectoryAsync(string queryString, string token, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/user-directory?{queryString}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.ParseAdd("application/json");
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        public async Task<HttpResponseMessage> UpdateUserAsync(string id, EditUserViewModel model, string token, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/admin/updateuserdirectory/{Uri.EscapeDataString(id)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { model.FullName, model.Email, model.Role, model.IsActive });
            return await client.SendAsync(request, cancellationToken);
        }

        public async Task<HttpResponseMessage> DeleteUserAsync(string id, string token, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(ApiClientName);
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/admin/deleteuserdirectory/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request, cancellationToken);
        }
    }
}