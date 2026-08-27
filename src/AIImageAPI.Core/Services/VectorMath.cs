namespace AIImageAPI.Core.Services;

public static class VectorMath
{
    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Dimension mismatch: {a.Length} vs {b.Length}");

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        var denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom < 1e-9 ? 0f : (float)(dot / denom);
    }

    public static byte[] PackFloats(float[] v)
    {
        var bytes = new byte[v.Length * sizeof(float)];
        Buffer.BlockCopy(v, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public static float[] UnpackFloats(byte[] bytes)
    {
        if (bytes.Length % sizeof(float) != 0)
            throw new ArgumentException("Byte length is not a multiple of sizeof(float)");
        var v = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, v, 0, bytes.Length);
        return v;
    }
}
