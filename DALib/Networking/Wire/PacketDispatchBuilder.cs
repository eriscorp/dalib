using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DALib.Networking.Wire;

/// <summary>
///     Parses a packet body into a typed packet. Used internally by the codec dispatch
///     tables; one instance per registered opcode, built once at codec construction.
/// </summary>
internal delegate TPacket WireParseFn<TPacket>(ReadOnlySpan<byte> body)
    where TPacket : IPacket;

/// <summary>
///     Builds opcode -> parser dispatch tables consumed by <see cref="PacketCodec" />.
///     The logic is identical for both directions; only the attribute type, the marker
///     interface, and the error-message prefix vary.
/// </summary>
internal static class PacketDispatchBuilder
{
    /// <summary>
    ///     Scans <paramref name="assemblies" /> for types decorated with
    ///     <typeparamref name="TAttribute" /> that implement <typeparamref name="TPacket" />
    ///     and have a <c>public static T Parse(ReadOnlySpan&lt;byte&gt;)</c> method, then
    ///     returns a dispatch table keyed by the attribute's opcode.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <param name="opcodeOf">Extracts the opcode from an attribute instance.</param>
    /// <param name="parseMethodName">
    ///     The static parse method to bind. Defaults to <c>Parse</c> for the opcode tables; the
    ///     0x3A dialog sub-dispatch passes <c>ParseResponse</c> so variant parsers do not collide
    ///     with the base's inherited <c>Parse</c>.
    /// </param>
    internal static Dictionary<byte, WireParseFn<TPacket>> Build<TAttribute, TPacket>(
        IEnumerable<Assembly> assemblies,
        Func<TAttribute, byte> opcodeOf,
        string parseMethodName = "Parse")
        where TAttribute : Attribute
        where TPacket : IPacket
    {
        var direction = TPacket.Direction;
        var table = new Dictionary<byte, WireParseFn<TPacket>>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in LoadableTypes(assembly))
            {
                var attr = type.GetCustomAttribute<TAttribute>(inherit: false);

                if (attr is null)
                    continue;

                var opcode = opcodeOf(attr);

                if (!typeof(TPacket).IsAssignableFrom(type))
                    throw new InvalidOperationException(
                        $"{type.FullName} carries the {direction} opcode attribute for " +
                        $"0x{opcode:X2} but does not implement {typeof(TPacket).Name}.");

                if (table.TryGetValue(opcode, out var existing))
                    throw new InvalidOperationException(
                        $"Duplicate {direction} opcode 0x{opcode:X2}: " +
                        $"{existing.Method.DeclaringType?.FullName ?? "?"} and {type.FullName}.");

                table[opcode] = BindParser<TPacket>(type, opcode, direction, parseMethodName);
            }
        }

        return table;
    }

    private static WireParseFn<TPacket> BindParser<TPacket>(
        Type type,
        byte opcode,
        string direction,
        string parseMethodName)
        where TPacket : IPacket
    {
        var parseMethod = type.GetMethod(
            parseMethodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            [typeof(ReadOnlySpan<byte>)]);

        if (parseMethod is null || !type.IsAssignableFrom(parseMethod.ReturnType))
            throw new InvalidOperationException(
                $"{type.FullName} carries the {direction} opcode attribute for " +
                $"0x{opcode:X2} but does not declare " +
                $"'public static {type.Name} {parseMethodName}(ReadOnlySpan<byte>)'.");

        // Return-type covariance: the method returns T (a concrete TPacket), the delegate
        // returns TPacket. CreateDelegate accepts this since at least .NET Core 2.1.
        return (WireParseFn<TPacket>)parseMethod.CreateDelegate(typeof(WireParseFn<TPacket>));
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Partial type loads still yield a useful subset - a missing optional dependency
            // in one type shouldn't kill the entire codec.
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
