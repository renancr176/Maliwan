﻿using Bogus;

namespace Maliwan.Test.Fixtures;

[CollectionDefinition(nameof(EntityColletion))]
public class EntityColletion : ICollectionFixture<EntityFixture>
{ }

public class EntityFixture : IDisposable
{
    public Faker Faker { get; private set; }
    public BrandFixture BrandFixture { get; set; }

    public EntityFixture()
    {
        Faker = new Faker("pt_BR");
        BrandFixture = new BrandFixture();
    }

    public void Dispose()
    {
        BrandFixture.Dispose();
    }
}