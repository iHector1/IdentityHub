using IdentityHub.UserService.Application.Abstractions;
using IdentityHub.UserService.Application.Users;
using IdentityHub.UserService.Domain.Entities;
using IdentityHub.UserService.Infrastructure.Repositories;
using IdentityHub.UserService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("PostgreSQL")
    ));

builder.Services.AddScoped<IUserRepository, EfUserRepository>();


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.MapGet("/api/users", async (IUserRepository userRepository) =>
{
    var users = await userRepository.GetAllAsync();
    return Results.Ok(users);
});

app.MapGet("/api/users/{id:guid}", async (Guid id, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/api/users", async (CreateUserRequest request, IUserRepository userRepository) =>
{
    var existingUser = await userRepository.GetByEmailAsync(request.Email);
    if (existingUser is not null)
    {
        return Results.Conflict($"A user with email '{request.Email}' already exists.");
    }

    var newUser = new User(request.FirstName, request.LastName, request.Email);
    await userRepository.AddAsync(newUser);
    return Results.Created($"/api/users/{newUser.Id}", newUser);
});

app.MapPut("/api/users/{id:guid}", async (Guid id, UpdateUserRequest request, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound();
    }

    var existingUser = await userRepository.GetByEmailAsync(request.Email);
    if (existingUser is not null && existingUser.Id != id)
    {
        return Results.Conflict($"A user with email '{request.Email}' already exists.");
    }

    user.Update(
        request.FirstName,
        request.LastName,
        request.Email
    );
    await userRepository.UpdateAsync(user);
    return Results.Ok(user);
});

app.MapDelete("/api/users/{id:guid}", async (Guid id, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByIdAsync(id);
    if (user is null)
    {
        return Results.NotFound();
    }

    await userRepository.DeleteAsync(user);
    return Results.NoContent();
});

app.Run();

