﻿using Bogus;
using Bogus.Extensions.Brazil;
using Maliwan.Domain.MaliwanContext.Entities;
using Maliwan.Domain.MaliwanContext.Enums;

namespace Maliwan.Test.Fixtures;

public class CustomerFixture : IDisposable
{
    public Faker Faker => new Faker("pt_BR");

    public Customer Valid()
    {
        var type = Faker.PickRandom<CustomerTypeEnum>();
        var name = type == CustomerTypeEnum.Individual ? Faker.Person.FullName : Faker.Company.CompanyName();
        var document = type == CustomerTypeEnum.Individual ? Faker.Person.Cpf() : Faker.Company.Cnpj();
        return new Customer(name, document, type);
    }

    public Customer Invalid()
    {
        return new Customer(String.Empty, String.Empty, CustomerTypeEnum.Individual);
    }

    public void Dispose()
    {
    }
}