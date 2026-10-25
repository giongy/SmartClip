using System;
using System.Text;

// A QR code (ISO/IEC 18004) of a text, for a phone to read off the screen and so take an entry from the PC (wanted by
// the user on 25/10/2026). Written here, as nothing can be added to the build: byte mode, the text in UTF-8, versions
// 1 to 40. The smallest version that holds the text with medium error correction (with low, when only that fits), then
// the strongest correction that version still holds: a larger code is harder to read off a screen, a stronger one is
// free. The steps are those of the standard, in the order of Project Nayuki's QR Code generator (MIT).
class Qr
{
    public const int Quiet = 4;   // the light modules the standard wants around the code
    const int MinVersion = 1, MaxVersion = 40;
    const int ModeBits = 4, ByteMode = 4;
    const int PadA = 0xEC, PadB = 0x11;
    // Error correction from weakest to strongest; how the format bits name each.
    public enum Level { Low, Medium, Quartile, High }
    static readonly int[] LevelBits = { 1, 0, 3, 2 };

    // For each level and version (none at 0): the error correction codewords of each block, and the blocks.
    static readonly int[][] EccPerBlock = {
        new int[] { -1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
        new int[] { -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        new int[] { -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 } };
    static readonly int[][] Blocks = {
        new int[] { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new int[] { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new int[] { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new int[] { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 } };

    public readonly int Version, Size;
    public readonly Level Correction;
    readonly bool[,] dark, fixedPart;   // [y, x]: the modules, and those of the patterns that the data goes around

    // Whether the module at column x, row y is dark.
    public bool this[int x, int y] { get { return dark[y, x]; } }

    // The most bytes of text a code holds: version 40 with low correction.
    public static int MaxBytes { get { return Capacity(MaxVersion, Level.Low); } }

    // The code of a text, or null when the text is too long for one.
    public static Qr Of(string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        foreach (Level least in new Level[] { Level.Medium, Level.Low })
            for (int version = MinVersion; version <= MaxVersion; version++)
            {
                if (bytes.Length > Capacity(version, least)) continue;
                Level level = least;
                for (Level stronger = least + 1; stronger <= Level.High; stronger++)
                    if (bytes.Length <= Capacity(version, stronger)) level = stronger;
                return new Qr(version, level, bytes);
            }
        return null;
    }

    // How many bytes a version holds at a level: its data codewords, less the mode and the count of the bytes.
    public static int Capacity(int version, Level level)
    {
        return (DataCodewords(version, level) * 8 - ModeBits - CountBits(version)) / 8;
    }

    static int CountBits(int version)
    {
        return version <= 9 ? 8 : 16;
    }

    // The modules a version has for codewords, data and error correction: all of them less those of the patterns.
    static int RawModules(int version)
    {
        int result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            int aligned = version / 7 + 2;
            result -= (25 * aligned - 10) * aligned - 55;
            if (version >= 7) result -= 36;
        }
        return result;
    }

    static int DataCodewords(int version, Level level)
    {
        return RawModules(version) / 8 - EccPerBlock[(int)level][version] * Blocks[(int)level][version];
    }

    Qr(int version, Level level, byte[] text)
    {
        Version = version;
        Correction = level;
        Size = version * 4 + 17;
        dark = new bool[Size, Size];
        fixedPart = new bool[Size, Size];
        DrawPatterns();
        Place(Interleave(Data(text)));
        // The mask that leaves the fewest look-alikes of the patterns, long runs and blocks, and the closest balance.
        int best = 0, least = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            Mask(mask);
            DrawFormat(mask);
            int penalty = Penalty();
            if (penalty < least)
            {
                least = penalty;
                best = mask;
            }
            Mask(mask);   // undone: masking twice changes nothing
        }
        Mask(best);
        DrawFormat(best);
    }

    // The data codewords: the mode, the count, the bytes, the end, then the padding of the standard.
    byte[] Data(byte[] text)
    {
        int capacity = DataCodewords(Version, Correction) * 8;
        Bits bits = new Bits(capacity);
        bits.Add(ByteMode, ModeBits);
        bits.Add(text.Length, CountBits(Version));
        foreach (byte b in text) bits.Add(b, 8);
        bits.Add(0, Math.Min(4, capacity - bits.Count));
        bits.Add(0, (8 - bits.Count % 8) % 8);
        for (int pad = PadA; bits.Count < capacity; pad ^= PadA ^ PadB) bits.Add(pad, 8);
        return bits.Bytes;
    }

    // Splits the data into its blocks, each with its error correction, and takes them a codeword from each in turn.
    // The first blocks are a codeword shorter.
    byte[] Interleave(byte[] data)
    {
        int blocks = Blocks[(int)Correction][Version], ecc = EccPerBlock[(int)Correction][Version];
        int raw = RawModules(Version) / 8, shorter = blocks - raw % blocks, shortLength = raw / blocks;
        byte[] divisor = Divisor(ecc);
        byte[][] made = new byte[blocks][];
        for (int i = 0, k = 0; i < blocks; i++)
        {
            int length = shortLength - ecc + (i < shorter ? 0 : 1);
            byte[] block = new byte[shortLength + 1];
            Array.Copy(data, k, block, 0, length);
            byte[] remainder = Remainder(data, k, length, divisor);
            k += length;
            Array.Copy(remainder, 0, block, shortLength + 1 - ecc, ecc);   // a short block keeps a hole before it
            made[i] = block;
        }
        byte[] result = new byte[raw];
        for (int i = 0, k = 0; i <= shortLength; i++)
            for (int j = 0; j < blocks; j++)
                if (i != shortLength - ecc || j >= shorter) result[k++] = made[j][i];
        return result;
    }

    // Reed-Solomon over GF(256) with the polynomial of QR codes, 0x11D: the generator of a degree, highest term
    // first and left out, and the remainder of some data divided by it.
    static byte[] Divisor(int degree)
    {
        byte[] result = new byte[degree];
        result[degree - 1] = 1;
        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < degree; j++)
            {
                result[j] = Times(result[j], root);
                if (j + 1 < degree) result[j] ^= result[j + 1];
            }
            root = Times(root, 2);
        }
        return result;
    }

    static byte[] Remainder(byte[] data, int start, int length, byte[] divisor)
    {
        byte[] result = new byte[divisor.Length];
        for (int i = start; i < start + length; i++)
        {
            int factor = data[i] ^ result[0];
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[result.Length - 1] = 0;
            for (int j = 0; j < result.Length; j++) result[j] ^= Times(divisor[j], factor);
        }
        return result;
    }

    static byte Times(int x, int y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }

    // The patterns every code of the version has: timing lines, the three finders, the alignment ones, and the
    // places of the format (written for real once the mask is chosen) and of the version.
    void DrawPatterns()
    {
        for (int i = 0; i < Size; i++)
        {
            Fixed(6, i, i % 2 == 0);
            Fixed(i, 6, i % 2 == 0);
        }
        Finder(3, 3);
        Finder(Size - 4, 3);
        Finder(3, Size - 4);
        int[] places = AlignmentPlaces();
        int count = places.Length;
        for (int i = 0; i < count; i++)
            for (int j = 0; j < count; j++)
                if (!(i == 0 && j == 0 || i == 0 && j == count - 1 || i == count - 1 && j == 0))   // not over a finder
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                            Fixed(places[i] + dx, places[j] + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
        DrawFormat(0);
        DrawVersion();
    }

    // A finder, with the light ring around it where the code has room.
    void Finder(int x, int y)
    {
        for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                int ring = Math.Max(Math.Abs(dx), Math.Abs(dy)), xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < Size && yy >= 0 && yy < Size) Fixed(xx, yy, ring != 2 && ring != 4);
            }
    }

    // Where the alignment patterns are centred, the same across and down.
    int[] AlignmentPlaces()
    {
        if (Version == 1) return new int[0];
        int count = Version / 7 + 2;
        int step = (Version * 8 + count * 3 + 5) / (count * 4 - 4) * 2;
        int[] result = new int[count];
        result[0] = 6;
        for (int i = count - 1, place = Size - 7; i >= 1; i--, place -= step) result[i] = place;
        return result;
    }

    // The level and the mask, with their own error correction, twice: beside the finder at the top left, and split
    // between the other two.
    void DrawFormat(int mask)
    {
        int data = LevelBits[(int)Correction] << 3 | mask, remainder = data;
        for (int i = 0; i < 10; i++) remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
        int bits = (data << 10 | remainder) ^ 0x5412;
        for (int i = 0; i <= 5; i++) Fixed(8, i, Bit(bits, i));
        Fixed(8, 7, Bit(bits, 6));
        Fixed(8, 8, Bit(bits, 7));
        Fixed(7, 8, Bit(bits, 8));
        for (int i = 9; i < 15; i++) Fixed(14 - i, 8, Bit(bits, i));
        for (int i = 0; i < 8; i++) Fixed(Size - 1 - i, 8, Bit(bits, i));
        for (int i = 8; i < 15; i++) Fixed(8, Size - 15 + i, Bit(bits, i));
        Fixed(8, Size - 8, true);   // always dark
    }

    // From version 7 the version itself, with its error correction, by the two finders away from the top left.
    void DrawVersion()
    {
        if (Version < 7) return;
        int remainder = Version;
        for (int i = 0; i < 12; i++) remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
        int bits = Version << 12 | remainder;
        for (int i = 0; i < 18; i++)
        {
            int a = Size - 11 + i % 3, b = i / 3;
            Fixed(a, b, Bit(bits, i));
            Fixed(b, a, Bit(bits, i));
        }
    }

    void Fixed(int x, int y, bool isDark)
    {
        dark[y, x] = isDark;
        fixedPart[y, x] = true;
    }

    static bool Bit(int value, int i)
    {
        return ((value >> i) & 1) != 0;
    }

    // The codewords, in columns two modules wide from the right, up and down in turn, around the patterns. What is left
    // at the end stays light.
    void Place(byte[] codewords)
    {
        int i = 0, total = codewords.Length * 8;
        for (int right = Size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;   // the vertical timing line takes a column of its own
            bool upward = ((right + 1) & 2) == 0;
            for (int step = 0; step < Size; step++)
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j, y = upward ? Size - 1 - step : step;
                    if (fixedPart[y, x] || i >= total) continue;
                    dark[y, x] = Bit(codewords[i >> 3], 7 - (i & 7));
                    i++;
                }
        }
    }

    // Turns over the modules of the data where the mask says so.
    void Mask(int mask)
    {
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool turn;
                switch (mask)
                {
                    case 0: turn = (x + y) % 2 == 0; break;
                    case 1: turn = y % 2 == 0; break;
                    case 2: turn = x % 3 == 0; break;
                    case 3: turn = (x + y) % 3 == 0; break;
                    case 4: turn = (x / 3 + y / 2) % 2 == 0; break;
                    case 5: turn = x * y % 2 + x * y % 3 == 0; break;
                    case 6: turn = (x * y % 2 + x * y % 3) % 2 == 0; break;
                    default: turn = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                }
                if (turn && !fixedPart[y, x]) dark[y, x] = !dark[y, x];
            }
    }

    // The four penalties of the standard: runs of five or more alike, blocks of two by two alike, look-alikes of a
    // finder, and dark modules far from half.
    int Penalty()
    {
        int result = 0, darkCount = 0;
        for (int i = 0; i < Size; i++)
        {
            result += Runs(i, true) + Runs(i, false);
            result += LookAlikes(i, true) + LookAlikes(i, false);
        }
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                if (dark[y, x]) darkCount++;
                if (x + 1 < Size && y + 1 < Size && dark[y, x] == dark[y, x + 1] && dark[y, x] == dark[y + 1, x]
                    && dark[y, x] == dark[y + 1, x + 1]) result += 3;
            }
        int total = Size * Size;
        result += ((Math.Abs(darkCount * 20 - total * 10) + total - 1) / total - 1) * 10;
        return result;
    }

    bool At(int line, int i, bool row)
    {
        return row ? dark[line, i] : dark[i, line];
    }

    int Runs(int line, bool row)
    {
        int result = 0, run = 1;
        for (int i = 1; i <= Size; i++)
        {
            if (i < Size && At(line, i, row) == At(line, i - 1, row)) run++;
            else
            {
                if (run >= 5) result += run - 2;
                run = 1;
            }
        }
        return result;
    }

    // Dark, light, three dark, light, dark, with four light on one side: the light outside the code counts.
    int LookAlikes(int line, bool row)
    {
        int result = 0;
        bool[] core = { true, false, true, true, true, false, true };
        for (int i = 0; i + 7 <= Size; i++)
        {
            bool match = true;
            for (int k = 0; k < 7 && match; k++) match = At(line, i + k, row) == core[k];
            if (!match) continue;
            if (Light(line, i - 4, i, row) || Light(line, i + 7, i + 11, row)) result += 40;
        }
        return result;
    }

    bool Light(int line, int from, int to, bool row)
    {
        for (int i = from; i < to; i++)
            if (i >= 0 && i < Size && At(line, i, row)) return false;
        return true;
    }

    // Bits written one after the other, the highest first.
    class Bits
    {
        readonly byte[] bytes;
        public int Count { get; private set; }

        public Bits(int capacity)
        {
            bytes = new byte[(capacity + 7) / 8];
        }

        public byte[] Bytes { get { return bytes; } }

        public void Add(int value, int length)
        {
            for (int i = length - 1; i >= 0; i--, Count++)
                if (((value >> i) & 1) != 0) bytes[Count >> 3] |= (byte)(0x80 >> (Count & 7));
        }
    }
}
