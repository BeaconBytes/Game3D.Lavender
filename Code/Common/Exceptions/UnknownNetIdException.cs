using System;

namespace Lavender.Common.Exceptions;

public class UnknownNetIdException : Exception
{
    public UnknownNetIdException() { }

    public UnknownNetIdException(string msg) : base(msg) { }
}