using System;

namespace Aqueduct.Shared.DateTime
{
    public interface IDateTimeProvider
    {
        DateTimeOffset Now();
    }
}