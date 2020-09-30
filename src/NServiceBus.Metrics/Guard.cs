﻿using System;

static class Guard
{
    public static void AgainstNull(string argumentName, object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstNullAndEmpty(string argumentName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstNegativeAndZero(string argumentName, TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(argumentName);
        }
    }

    public static void AgainstNegativeAndZero(string argumentName, TimeSpan? optionalValue)
    {
        if (optionalValue.HasValue && optionalValue.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(argumentName);
        }
    }
}