using Carter;
using eCommerce.OrdersService.Api;
using eCommerce.OrdersService.Api.HttpClients.Products;
using eCommerce.OrdersService.Api.HttpClients.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddDependencies(builder.Configuration);

builder.Services.AddCarter();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IUsersMicroserviceClient, UsersMicroserviceClient>(client =>
{
    client.BaseAddress = new
        Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
});

builder.Services.AddHttpClient<IProductsMicroserviceClient, ProductsMicroserviceClient>(client =>
{
    client.BaseAddress = new
        Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCarter();

//app.UseHttpsRedirection();

app.Run();
