using System.Numerics;

namespace DALib.Extensions;

/// <summary>
///     Provides extension methods for numbers.
/// </summary>
/// <typeparam name="T">
/// </typeparam>
public static class NumberExtensions<T> where T: INumber<T>
{
    /// <summary>
    ///     Whether the type is an integer type
    /// </summary>
    public static bool IsIntegerType { get; } = (typeof(T) == typeof(sbyte))
                                                || (typeof(T) == typeof(byte))
                                                || (typeof(T) == typeof(short))
                                                || (typeof(T) == typeof(ushort))
                                                || (typeof(T) == typeof(int))
                                                || (typeof(T) == typeof(uint))
                                                || (typeof(T) == typeof(long))
                                                || (typeof(T) == typeof(ulong))
                                                || (typeof(T) == typeof(nint))
                                                || (typeof(T) == typeof(nuint));
}