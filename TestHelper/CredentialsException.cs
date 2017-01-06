using System;

namespace TestHelper
{
    public class CredentialsException : Exception
    {
        public CredentialsException()
        {
        }

        public CredentialsException(string message): base(message)
        {
        }
    }
}
