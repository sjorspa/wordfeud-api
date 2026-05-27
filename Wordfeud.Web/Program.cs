var builder = WebApplication.CreateBuilder(args);

// Add HttpClient for API communication
builder.Services.AddHttpClient("Api", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ApiUrl"] ?? "http://localhost:8080");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
