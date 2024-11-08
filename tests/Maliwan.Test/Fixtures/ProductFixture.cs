﻿using Bogus;
using Maliwan.Domain.MaliwanContext.Entities;
using Maliwan.Test.Extensions;

namespace Maliwan.Test.Fixtures;

public class ProductFixture : IDisposable
{
    public Faker Faker { get; private set; }

    public ProductFixture()
    {
        Faker = new Faker("pt_BR");
    }

    public Product Valid()
    {
        Faker = new Faker("pt_BR");
        var name = Faker.Commerce.ProductName();
        var sku = name.GetSku();
        return new Product(1, 1, 1, name, Faker.Random.Decimal(10M, 200M), sku, true);
    }

    public Product Invalid()
    {
        return new Product(0, 0, 0, String.Empty, 0M, String.Empty, false);
    }

    public void Dispose()
    {
    }
}