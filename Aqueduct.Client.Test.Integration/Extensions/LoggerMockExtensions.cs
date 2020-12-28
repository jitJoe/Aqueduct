using System;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aqueduct.Client.Test.Integration.Extensions
{
    public static class LoggerMockExtensions
    {
        public static Mock<ILogger<T>> VerifyErrorWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage, string expectedExceptionMessage = null)
        {
            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;
    
            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.Is<Exception>(exception => expectedExceptionMessage == null || exception.Message == expectedExceptionMessage),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));

            return logger;
        }
    }
}