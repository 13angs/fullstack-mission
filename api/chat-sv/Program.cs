using chat_sv.Controllers;
using chat_sv.Hubs;
using MongoDB.Driver;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
        {
            options.AddPolicy(configuration["CorsName"]!, build =>
            {
                build.WithOrigins(configuration["AllowedHosts"]!)
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        });
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    });
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(configuration["MongoSetting:ConnectionString"]));
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateAsyncScope())
{
    var mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var database = mongoClient.GetDatabase("chat_db");
    var usersCollection = database.GetCollection<User>("users");
    var users = usersCollection.Find(_=>true);

    if(!users.Any())
    {
        logger.LogInformation("No user exist!\nAdding new users...");
        await usersCollection.InsertManyAsync(new List<User>{
            new User{
                Id="c13cf23b-59c8-490d-8e8b-93c1988e203b",
                Name="user 1",
                Avatar="https://cdn.dribbble.com/users/3841177/screenshots/11950347/cartoon-avatar_2020__8_circle.png"
            },
            new User{
                Id="29e7066b-0c42-48de-a835-1a103fd1dade",
                Name="user 2",
                Avatar="https://www.gamer-hub.io/static/img/team/sam.png"
            },
        });
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(configuration["CorsName"]!);

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");
app.Run();
