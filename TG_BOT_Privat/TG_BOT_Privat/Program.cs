using TG_BOT_Privat;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Easy_Privat_Bot easy_Privat_Bot = new Easy_Privat_Bot();
easy_Privat_Bot.Start();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

