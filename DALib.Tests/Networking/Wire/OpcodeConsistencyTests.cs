using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

/// <summary>
///     Guards the one footgun in declaring an opcode twice per packet (the dispatch attribute
///     and the instance <c>Opcode</c> property): they must agree. Also asserts that every
///     concrete packet type actually carries its dispatch attribute - a missing one would make
///     the packet silently undispatchable.
/// </summary>
/// <remarks>
///     Instances are created with <see cref="RuntimeHelpers.GetUninitializedObject" /> so the
///     test stays generic despite <c>required</c> members - <c>Opcode</c> returns a compile-time
///     constant and never reads instance state, so an uninitialized instance reports it correctly.
/// </remarks>
public class OpcodeConsistencyTests
{
    private static readonly Assembly DALibAssembly = typeof(IPacket).Assembly;

    [Fact]
    public void EveryClientPacket_InstanceOpcodeMatchesAttribute()
    {
        foreach (var type in DALibAssembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IClientPacket).IsAssignableFrom(type))
                continue;

            var attr = FindAttribute<ClientOpcodeAttribute>(type);
            attr.Should().NotBeNull($"{type.Name} is an IClientPacket and must carry [ClientOpcode]");

            var instance = (IClientPacket)RuntimeHelpers.GetUninitializedObject(type);
            instance.Opcode.Should().Be(attr!.Opcode, $"{type.Name}: [ClientOpcode] and Opcode must agree");
        }
    }

    [Fact]
    public void EveryServerPacket_InstanceOpcodeMatchesAttribute()
    {
        foreach (var type in DALibAssembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IServerPacket).IsAssignableFrom(type))
                continue;

            var attr = FindAttribute<ServerOpcodeAttribute>(type);
            attr.Should().NotBeNull($"{type.Name} is an IServerPacket and must carry [ServerOpcode]");

            var instance = (IServerPacket)RuntimeHelpers.GetUninitializedObject(type);
            instance.Opcode.Should().Be(attr!.Opcode, $"{type.Name}: [ServerOpcode] and Opcode must agree");
        }
    }

    /// <summary>
    ///     Finds the opcode attribute on <paramref name="type" /> or, for a variant of a
    ///     multi-variant opcode, on the nearest base that carries it (the attributes are declared
    ///     <c>Inherited = false</c>, so the lookup walks the chain explicitly).
    /// </summary>
    private static TAttr? FindAttribute<TAttr>(Type type) where TAttr : Attribute
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var attr = t.GetCustomAttribute<TAttr>(inherit: false);
            if (attr is not null)
                return attr;
        }

        return null;
    }
}
