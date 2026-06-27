using DALib.Networking.Wire;

namespace DALib.Tests.Networking.Wire;

// Stub packets used by codec tests. They live in the test assembly so the default
// PacketCodec / PacketCodec (which only scans the DALib assembly) does not pick
// them up. Codec tests pass typeof(TestNoneClientPacket).Assembly explicitly.
//
// Opcodes are chosen to match the EncryptMethod the codec will derive via
// CryptoState.GetClientEncryptMethod / GetServerEncryptMethod:
//
//   0x10  client None  (ClientJoin opcode in DA)
//   0x40  server None  (something pre-handshake)
//   0x02  client Normal (Login family)
//   0x01  server Normal (LoginMessage family)
//   0x39  client MD5Key + dialog obfuscation (NpcMainMenu)
//   0xFC  client MD5Key (catch-all default)
//   0xFE  server MD5Key (catch-all default)

[ClientOpcode((ClientOpcode)0x10)]
public sealed record TestNoneClientPacket : ClientPacket
{
    public required byte Value { get; init; }
    public required string Name { get; init; }

    public override byte Opcode => 0x10;

    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(Value);
        writer.WriteString8(Name);
    }

    public static TestNoneClientPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestNoneClientPacket
        {
            Value = reader.ReadByte(),
            Name = reader.ReadString8(),
        };
    }
}

[ServerOpcode((ServerOpcode)0x40)]
public sealed record TestNoneServerPacket : ServerPacket
{
    public required ushort Tag { get; init; }

    public override byte Opcode => 0x40;

    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt16(Tag);

    public static TestNoneServerPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestNoneServerPacket { Tag = reader.ReadUInt16() };
    }
}

[ClientOpcode((ClientOpcode)0x02)]
public sealed record TestNormalClientPacket : ClientPacket
{
    public required string Name { get; init; }
    public required string Password { get; init; }

    public override byte Opcode => 0x02;

    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteString8(Name);
        writer.WriteString8(Password);
    }

    public static TestNormalClientPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestNormalClientPacket
        {
            Name = reader.ReadString8(),
            Password = reader.ReadString8(),
        };
    }
}

[ServerOpcode((ServerOpcode)0x01)]
public sealed record TestNormalServerPacket : ServerPacket
{
    public required byte Status { get; init; }

    public override byte Opcode => 0x01;

    public override void WriteBody(IPacketWriter writer) => writer.WriteByte(Status);

    public static TestNormalServerPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestNormalServerPacket { Status = reader.ReadByte() };
    }
}

[ClientOpcode((ClientOpcode)0xFC)]
public sealed record TestMd5KeyClientPacket : ClientPacket
{
    public required string Text { get; init; }

    public override byte Opcode => 0xFC;

    public override void WriteBody(IPacketWriter writer) => writer.WriteString8(Text);

    public static TestMd5KeyClientPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestMd5KeyClientPacket { Text = reader.ReadString8() };
    }
}

[ServerOpcode((ServerOpcode)0xFE)]
public sealed record TestMd5KeyServerPacket : ServerPacket
{
    public required uint Value { get; init; }

    public override byte Opcode => 0xFE;

    public override void WriteBody(IPacketWriter writer) => writer.WriteUInt32(Value);

    public static TestMd5KeyServerPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestMd5KeyServerPacket { Value = reader.ReadUInt32() };
    }
}

[ClientOpcode((ClientOpcode)0x39)]
public sealed record TestDialogClientPacket : ClientPacket
{
    public required byte ObjectType { get; init; }
    public required uint ObjectId { get; init; }
    public required ushort PursuitId { get; init; }

    public override byte Opcode => 0x39;

    public override void WriteBody(IPacketWriter writer)
    {
        writer.WriteByte(ObjectType);
        writer.WriteUInt32(ObjectId);
        writer.WriteUInt16(PursuitId);
    }

    public static TestDialogClientPacket Parse(ReadOnlySpan<byte> body)
    {
        var reader = new PacketReader(body);

        return new TestDialogClientPacket
        {
            ObjectType = reader.ReadByte(),
            ObjectId = reader.ReadUInt32(),
            PursuitId = reader.ReadUInt16(),
        };
    }
}
