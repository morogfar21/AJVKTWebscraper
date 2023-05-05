using Serilog;
using System.Text.Json.Serialization;
using Webscraper.AJVKT.API.Service;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateLogger();

builder.Logging.ClearProviders();

builder.Logging.AddSerilog(Log.Logger);
//builder.Host.UseSerilog(Log.Logger);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddScoped<IWebsiteScrapingService, WebsiteScrapingService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
