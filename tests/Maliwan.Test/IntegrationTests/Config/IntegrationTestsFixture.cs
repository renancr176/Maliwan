﻿using System.Net.Http.Json;
using Bogus;
using Maliwan.Application.Commands.IdentityContext.UserCommands;
using Maliwan.Application.Models.IdentityContext.Responses;
using Maliwan.Domain.Core.Enums;
using Maliwan.Domain.IdentityContext.Entities;
using Maliwan.Service.Api.Models.Responses;
using Maliwan.Service.Api;
using Maliwan.Test.Extensions;
using Maliwan.Test.Fixtures;
using Microsoft.AspNetCore.Identity;

namespace Maliwan.Test.IntegrationTests.Config;

[CollectionDefinition(nameof(IntegrationTestsFixtureCollection))]
public class IntegrationTestsFixtureCollection : ICollectionFixture<IntegrationTestsFixture<StartupTests>> { }

public class IntegrationTestsFixture<TStartup> : IDisposable where TStartup : class
{
    public readonly StartupFactory<TStartup> Factory;
    public HttpClient Client;
    public EntityFixture EntityFixture;
    public IServiceProvider Services;
    //public DigaXDbContext DigaXDbContext;
    public UserManager<User> UserManager;

    public string AdminUserName { get; set; }
    public string AdminPassword { get; set; }
    public string? AdminAccessToken { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string UserPassword { get; set; }
    public string? UserAccessToken { get; set; }

    public IntegrationTestsFixture()
    {
        Factory = new StartupFactory<TStartup>();
        Client = Factory.CreateClient();
        EntityFixture = new EntityFixture();

        AdminUserName = "usertest@telecall.com.br";
        AdminPassword = "g}}P9=#%2L~R,fH?=_<]76Dc#96@Em65";

        Services = Factory.Server.Services;
        //DigaXDbContext = (DigaXDbContext)Services.GetService(typeof(DigaXDbContext));

        //if (DigaXDbContext == null)
        //{
        //    throw new ArgumentNullException(nameof(DigaXDbContext), "Database connection can't be null");
        //}

        Task.Run(async () =>
        {
            UserManager = (UserManager<User>)Services.GetService(typeof(UserManager<User>));

            var user = await UserManager.FindByNameAsync(AdminUserName);
            if (user == null)
            {
                await UserManager.CreateAsync(
                    new User(
                        AdminUserName,
                        "Admin",
                        "admin@digax.com.br",
                        "Admin"),
                    AdminPassword);

                user = await UserManager.FindByNameAsync(AdminUserName);
                user.EmailConfirmed = true;
                await UserManager.UpdateAsync(user);

                await UserManager.AddToRoleAsync(user, RoleEnum.Admin.ToString());
            }

            //await DigaXDbContext.SaveChangesAsync();
        }).Wait();
    }

    public async Task GenerateUserAndPasswordAsync()
    {
        var userExists = false;
        var maxRetry = 10;
        do
        {
            var faker = new Faker("pt_BR");
            UserName = faker.Internet.Email().ToLower();
            var user = await UserManager.FindByNameAsync(UserName);

            if (user == null)
            {
                UserPassword = faker.Internet.Password(8, false, "", "Ab@1_");
                user = new User(
                    UserName,
                    UserName,
                    UserName,
                    UserPassword);
                await UserManager.CreateAsync(user, UserPassword);

                user = await UserManager.FindByNameAsync(UserName);
                user.EmailConfirmed = true;
                await UserManager.UpdateAsync(user);

                UserId = user.Id;
            }
            else
            {
                userExists = true;
            }

            maxRetry--;
        } while (userExists && maxRetry > 0);

        if (maxRetry <= 0)
        {
            throw new Exception("Reached max attempts to create user.");
        }
    }

    public async Task AuthenticateAsAdminAsync()
    {
        AdminAccessToken = await AuthenticateAsync(AdminUserName, AdminPassword);
    }

    public async Task AuthenticateAsUserAsync()
    {
        UserAccessToken = await AuthenticateAsync(UserName, UserPassword);
    }

    public async Task<string> AuthenticateAsync(string userName, string password)
    {
        var request = new SignInCommand
        {
            UserName = userName,
            Password = password
        };

        // Recriando o client para evitar configurações de outro startup.
        Client = Factory.CreateClient();

        var response = await Client.AddJsonMediaType()
            .PostAsJsonAsync("/User/SignIn", request);
        response.EnsureSuccessStatusCode();
        var responseObj = await response.DeserializeObject<BaseResponse<SignInResponseModel>>();
        if (string.IsNullOrEmpty(responseObj?.Data?.AccessToken))
            throw new ArgumentNullException("AccessToken", "Unable to retrieve authentication token.");

        return responseObj?.Data.AccessToken;
    }

    public async Task ChangeUserAsync(Guid userId)
    {
        if (UserId != userId)
        {
            var user = await UserManager.FindByIdAsync(userId.ToString());
            UserId = userId;
            UserName = user.Name;
            UserPassword = user.RememberPhrase;
            await AuthenticateAsUserAsync();
        }
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}