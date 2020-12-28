using System;

namespace Aqueduct.Shared.DateTime
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset Now() => DateTimeOffset.Now;
    }
}