using System;

namespace Lavender.Common.Exceptions;

public class BadNodeSetupException : Exception
{
    public BadNodeSetupException() { }

    public BadNodeSetupException(string msg) : base(msg) { }
}