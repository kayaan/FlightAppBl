using FlightApp.Domain;
using System.Text;

namespace FlightApp.Services;

public class TrackBinarySerializer
{
    public byte[] Serialize(TrackArrays track)
    {
        Validate(track);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes("TRK1"));

        int count = track.TDeltaMs.Length;
        writer.Write(count);

        WriteInt32Array(writer, track.TDeltaMs);
        WriteInt32Array(writer, track.LatE7);
        WriteInt32Array(writer, track.LonE7);
        WriteInt32Array(writer, track.AltGpsCm);
        WriteInt32Array(writer, track.AltBaroCm);
        WriteInt32Array(writer, track.SpeedCms);
        WriteInt32Array(writer, track.VarioCms);

        writer.Flush();
        return stream.ToArray();
    }

    public TrackArrays Deserialize(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != "TRK1")
            throw new InvalidOperationException("Invalid track binary format. Expected TRK1.");

        int count = reader.ReadInt32();
        if (count < 0)
            throw new InvalidOperationException("Invalid track point count.");

        return new TrackArrays
        {
            TDeltaMs = ReadInt32Array(reader, count),
            LatE7 = ReadInt32Array(reader, count),
            LonE7 = ReadInt32Array(reader, count),
            AltGpsCm = ReadInt32Array(reader, count),
            AltBaroCm = ReadInt32Array(reader, count),
            SpeedCms = ReadInt32Array(reader, count),
            VarioCms = ReadInt32Array(reader, count)
        };
    }

    private static void WriteInt32Array(BinaryWriter writer, int[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            writer.Write(values[i]);
        }
    }

    private static int[] ReadInt32Array(BinaryReader reader, int count)
    {
        var values = new int[count];

        for (int i = 0; i < count; i++)
        {
            values[i] = reader.ReadInt32();
        }

        return values;
    }

    private static void Validate(TrackArrays track)
    {
        int count = track.TDeltaMs.Length;

        if (track.LatE7.Length != count ||
            track.LonE7.Length != count ||
            track.AltGpsCm.Length != count ||
            track.AltBaroCm.Length != count ||
            track.SpeedCms.Length != count ||
            track.VarioCms.Length != count)
        {
            throw new InvalidOperationException("All TrackArrays must have the same length.");
        }
    }
}