﻿using System.Net.Http.Json;
using Bogus;
using Maliwan.Application.Commands.IdentityContext.UserCommands;
using Maliwan.Application.Models.IdentityContext.Responses;
using Maliwan.Domain.Core.Enums;
using Maliwan.Domain.IdentityContext.Entities;
using Maliwan.Domain.MaliwanContext.Entities;
using Maliwan.Infra.Data.Contexts.MaliwanDb;
using Maliwan.Service.Api.Models.Responses;
using Maliwan.Service.Api;
using Maliwan.Test.Extensions;
using Maliwan.Test.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Maliwan.Test.IntegrationTests.Config;

[CollectionDefinition(nameof(IntegrationTestsFixtureCollection))]
public class IntegrationTestsFixtureCollection : ICollectionFixture<IntegrationTestsFixture<StartupTests>> { }

public class IntegrationTestsFixture<TStartup> : IDisposable where TStartup : class
{
    public readonly StartupFactory<TStartup> Factory;
    public HttpClient Client;
    public EntityFixture EntityFixture;
    public IServiceProvider Services;
    public MaliwanDbContext MaliwanDbContext;
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

        AdminUserName = "usertest@maliwan.com.br";
        AdminPassword = "g}}P9=#%2L~R,fH?=_<]76Dc#96@Em65";

        Services = Factory.Server.Services;
        MaliwanDbContext = (MaliwanDbContext)Services.GetService(typeof(MaliwanDbContext));

        if (MaliwanDbContext == null)
        {
            throw new ArgumentNullException(nameof(MaliwanDbContext), "Database connection can't be null");
        }

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
                        "admin@maliwan.com.br",
                        "Admin"),
                    AdminPassword);

                user = await UserManager.FindByNameAsync(AdminUserName);
                user.EmailConfirmed = true;
                await UserManager.UpdateAsync(user);

                await UserManager.AddToRoleAsync(user, RoleEnum.Admin.ToString());
            }

            await MaliwanDbContext.SaveChangesAsync();
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

    #region Inserted Entity

    public async Task<Brand> GetInsertedNewBrandAsync()
    {
        var entity = EntityFixture.BrandFixture.Valid();
        while (await MaliwanDbContext.Brands.AnyAsync(e =>
                   (e.Name.Trim().ToLower() == entity.Name.Trim().ToLower() 
                   || e.Sku.Trim().ToLower() == entity.Sku.Trim().ToLower())
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.BrandFixture.Valid();
        }
        await MaliwanDbContext.Brands.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Category> GetInsertedNewCategoryAsync()
    {
        var entity = EntityFixture.CategoryFixture.Valid();
        while (await MaliwanDbContext.Categories.AnyAsync(e =>
                   e.Name.Trim().ToLower() == entity.Name.Trim().ToLower()
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.CategoryFixture.Valid();
        }
        await MaliwanDbContext.Categories.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Customer> GetInsertedNewCustomerAsync()
    {
        var entity = EntityFixture.CustomerFixture.Valid();
        while (await MaliwanDbContext.Customers.AnyAsync(e =>
                   e.Document.Trim().ToLower() == entity.Document.Trim().ToLower()
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.CustomerFixture.Valid();
        }
        await MaliwanDbContext.Customers.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Gender> GetInsertedNewGenderAsync()
    {
        var entity = EntityFixture.GenderFixture.Valid();
        while (await MaliwanDbContext.Genders.AnyAsync(e =>
                   (e.Name.Trim().ToLower() == entity.Name.Trim().ToLower()
                    || e.Sku.Trim().ToLower() == entity.Sku.Trim().ToLower())
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.GenderFixture.Valid();
        }
        await MaliwanDbContext.Genders.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<OrderItem> GetNewOrderItemAsync(Stock stock = null)
    {
        stock ??= await GetInsertedNewStockAsync();
        var product = await MaliwanDbContext.Products.FirstOrDefaultAsync(e => e.Id == stock.IdProduct);
        return new OrderItem(stock.Id, 1, product.UnitPrice, 0M);
    }

    public async Task<Order> GetNewOrderAsync(Customer customer = null, IEnumerable<Stock> stocks = null)
    {
        customer ??= await GetInsertedNewCustomerAsync();
        var orderItems = new List<OrderItem>();
        if (stocks != null && stocks.Any())
        {
            foreach (var stock in stocks)
            {
                orderItems.Add(await GetNewOrderItemAsync(stock));
            }
        }
        else
        {
            orderItems.Add(await GetNewOrderItemAsync());
        }
        var entity = new Order(customer.Id, orderItems);

        return entity;
    }

    public async Task<Order> GetInsertedNewOrderAsync(Customer customer = null, IEnumerable<Stock> stocks = null)
    {
        var entity = await GetNewOrderAsync(customer, stocks);
        await MaliwanDbContext.Orders.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<OrderPayment> GetNewOrderPaymentAsync(Order? order = null, PaymentMethod? paymentMethod = null)
    {
        order ??= await MaliwanDbContext.Orders
                      .Include(nameof(Order.OrderPayments))
                      .Include(nameof(Order.OrderItems))
                      .FirstOrDefaultAsync(e => 
                          !e.DeletedAt.HasValue
                          && ((e.OrderItems.Sum(e => e.UnitPrice * e.Quantity) - e.OrderItems.Sum(e => e.Discount)) - e.OrderPayments.Sum(e => e.AmountPaid)) > 0)  ??
                  await GetInsertedNewOrderAsync();
        paymentMethod ??=
            await MaliwanDbContext.PaymentMethods.FirstOrDefaultAsync(e => e.Active && !e.DeletedAt.HasValue) ??
            await GetInsertedNewPaymentMethodAsync();

        return new OrderPayment(order.Id, paymentMethod.Id, order.OutstandingBalance);
    }

    public async Task<OrderPayment> GetInsertedNewOrderPaymentAsync(Order? order = null, PaymentMethod? paymentMethod = null)
    {
        var entity = await GetNewOrderPaymentAsync(order, paymentMethod);
        await MaliwanDbContext.OrderPayments.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<PaymentMethod> GetInsertedNewPaymentMethodAsync()
    {
        var entity = EntityFixture.PaymentMethodFixture.Valid();
        while (await MaliwanDbContext.PaymentMethods.AnyAsync(e =>
                   e.Name.Trim().ToLower() == entity.Name.Trim().ToLower()
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.PaymentMethodFixture.Valid();
        }
        await MaliwanDbContext.PaymentMethods.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Product> GetNewProductAsync()
    {
        var brand = await GetInsertedNewBrandAsync();
        var subcategory =await GetInsertedNewSubcategoryAsync(await GetInsertedNewCategoryAsync());
        var gender = EntityFixture.Faker.Random.Bool()
            ? await GetInsertedNewGenderAsync()
            : null;

        var entity = EntityFixture.ProductFixture.Valid(brand.Id, subcategory.Id, gender?.Id);
        while (await MaliwanDbContext.Products.AnyAsync(e =>
                   e.Name.Trim().ToLower() == entity.Name.Trim().ToLower()
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.ProductFixture.Valid();
        }

        return entity;
    }

    public async Task<Product> GetInsertedNewProductAsync()
    {
        var entity = await GetNewProductAsync();
        await MaliwanDbContext.Products.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }
    
    public async Task<ProductColor> GetInsertedNewProductColorAsync()
    {
        var entity = EntityFixture.ProductColorFixture.Valid();
        while (await MaliwanDbContext.ProductColors.AnyAsync(e =>
                   (e.Name.Trim().ToLower() == entity.Name.Trim().ToLower() ||
                    e.Sku.Trim().ToLower() == entity.Sku.Trim().ToLower())
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.ProductColorFixture.Valid();
        }
        await MaliwanDbContext.ProductColors.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<ProductSize> GetInsertedNewProductSizeAsync()
    {
        var entity = EntityFixture.ProductSizeFixture.Valid();
        while (await MaliwanDbContext.ProductSizes.AnyAsync(e =>
                   (e.Name.Trim().ToLower() == entity.Name.Trim().ToLower() ||
                    e.Sku.Trim().ToLower() == entity.Sku.Trim().ToLower())
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.ProductSizeFixture.Valid();
        }
        await MaliwanDbContext.ProductSizes.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Stock> GetNewStockAsync(Product? product = null)
    {
        product ??=
                  await MaliwanDbContext.Products.FirstOrDefaultAsync(e => 
                      e.Active 
                      && !e.DeletedAt.HasValue 
                      && !e.Stocks.Any()) ??
                  await GetInsertedNewProductAsync();

        var productSize = await MaliwanDbContext.ProductSizes.FirstOrDefaultAsync(e => !e.DeletedAt.HasValue) ?? await GetInsertedNewProductSizeAsync();
        var producColor = await MaliwanDbContext.ProductColors.FirstOrDefaultAsync(e => !e.DeletedAt.HasValue) ??
                          await GetInsertedNewProductColorAsync();

        return EntityFixture.StockFixture.Valid(product.Id, productSize.Id, producColor.Id);
    }

    public async Task<Stock> GetInsertedNewStockAsync(Product? product = null)
    {
        var stock = await GetNewStockAsync(product);
        await MaliwanDbContext.Stocks.AddAsync(stock);
        await MaliwanDbContext.SaveChangesAsync();
        return stock;
    }

    public async Task<Subcategory> GetInsertedNewSubcategoryAsync(Category category = null)
    {
        category = category ?? await MaliwanDbContext.Categories.FirstOrDefaultAsync(e => e.Active && !e.DeletedAt.HasValue)
            ?? await GetInsertedNewCategoryAsync();

        var entity = EntityFixture.SubcategoryFixture.Valid();
        while (await MaliwanDbContext.Subcategories.AnyAsync(e =>
                   e.IdCategory == category.Id
                   && e.Name.Trim().ToLower() == entity.Name.Trim().ToLower()
                   && !e.DeletedAt.HasValue))
        {
            entity = EntityFixture.SubcategoryFixture.Valid();
        }

        entity.IdCategory = category.Id;

        await MaliwanDbContext.Subcategories.AddAsync(entity);
        await MaliwanDbContext.SaveChangesAsync();
        return entity;
    }

    #endregion

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        EntityFixture.Dispose();
    }
}