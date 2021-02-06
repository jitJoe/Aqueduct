using System;

namespace Aqueduct.Shared.CallbackRegistry
{
    public class CallbackExpiredException : Exception
    {
        public CallbackExpiredException(string message) : base(message)
        {
        }
    }
}