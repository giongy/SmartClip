using System;
using System.Security.Cryptography;
using System.Text;

// What the database keeps encrypted, as the user chose it (the first start asks): nothing, everything readable by
// anyone who has the file (wanted by the user on 28/10/2026, the default); the saved entries; or everything, what is
// copied too.
enum Protection { Nothing = 0, Saved = 1, All = 2 }

// The key the saved entries are encrypted with, and the ways it is kept (wanted by the user on 28/10/2026: encrypted for
// the Windows user alone, as before, the saved texts could not be read once the PC was formatted, also from a copy of the
// database kept on OneDrive). The key is random, and the database keeps it locked twice: by DPAPI for each PC and Windows
// user that opened it (so there nothing is asked), and, if the user chose one, by a recovery phrase of three words, which
// unlocks it on any other PC. The phrase is never kept: only the key locked by it.
//
// What the key encrypts is AES-256 in CBC mode, then authenticated with HMAC-SHA256 over all of it (encrypt-then-MAC):
// a wrong key, or a changed byte, is found before anything is decrypted. .NET Framework has no AES-GCM. The versions
// before kept each value encrypted by DPAPI itself, and the user may keep them unencrypted (Protection): Open reads a
// value of any of the three, by how it starts, and the database brings them to the one chosen (Clips.Modernize).
sealed class Vault
{
    public const int PhraseWords = 3;
    public const char WordSeparator = '-';
    // PBKDF2 with HMAC-SHA512, as many rounds as OWASP advises (2023): 0.7 s on a PC of 2026, once a PC. A database
    // keeps the rounds its phrase was locked with, so that a later version can ask for more.
    public const int Rounds = 210000;
    const int KeySize = 32, MacSize = 32, BlockSize = 16, SaltSize = 16;
    // What a value encrypted under a key starts with: "SCK" and the version of the format. A DPAPI blob starts with 1, 0, 0, 0.
    static readonly byte[] Mark = { 0x53, 0x43, 0x4B, 0x01 };

    readonly byte[] key;   // the first half encrypts, the second authenticates; null while locked

    Vault(byte[] key)
    {
        this.key = key;
    }

    // A new key, never used before.
    public static Vault New()
    {
        return new Vault(Random(KeySize + MacSize));
    }

    // No key: a database whose key this PC cannot unlock, until the phrase is given.
    public static readonly Vault None = new Vault(null);

    public bool Locked
    {
        get { return key == null; }
    }

    // --- What the key encrypts ---

    // A value encrypted under the key. Locked, nothing can be: the message says why.
    public byte[] Seal(byte[] plain)
    {
        if (Locked) throw new InvalidOperationException(Strings.SavedLocked);
        return Seal(plain, key);
    }

    // What tells a copied entry copied again, when everything is encrypted: an HMAC under the key, which says nothing
    // of the content to whoever has the file, as a plain SHA-256 would of a short one. Its key is derived from that of
    // the MACs, so that the two never meet.
    public byte[] Fingerprint(byte[] content)
    {
        if (Locked) throw new InvalidOperationException(Strings.SavedLocked);
        byte[] part = new byte[MacSize];
        Buffer.BlockCopy(key, KeySize, part, 0, MacSize);
        byte[] derived;
        using (HMACSHA256 hmac = new HMACSHA256(part)) derived = hmac.ComputeHash(Encoding.ASCII.GetBytes("fingerprint"));
        using (HMACSHA256 hmac = new HMACSHA256(derived)) return hmac.ComputeHash(content);
    }

    // A value as it was before Seal, or as DPAPI encrypted it in the versions before, or the value itself if it is
    // kept unencrypted. One this PC cannot read throws a CryptographicException.
    public byte[] Open(byte[] secret)
    {
        if (Dpapi(secret)) return ProtectedData.Unprotect(secret, null, DataProtectionScope.CurrentUser);
        if (!Sealed(secret)) return secret;
        if (Locked) throw new CryptographicException(Strings.SavedLocked);
        byte[] plain = Open(secret, key);
        if (plain == null) throw new CryptographicException("A saved value does not match its key");
        return plain;
    }

    // Whether a value was encrypted under a key.
    public static bool Sealed(byte[] secret)
    {
        return Starts(secret, Mark);
    }

    // Whether DPAPI encrypted a value: its blobs all begin with the same version and provider, which no text written by
    // a person nor a PNG begins with. Taken from one made now, rather than written here.
    public static bool Dpapi(byte[] secret)
    {
        if (dpapiStart == null)
        {
            byte[] blob = ProtectedData.Protect(new byte[1], null, DataProtectionScope.CurrentUser);
            byte[] start = new byte[DpapiStart];
            Buffer.BlockCopy(blob, 0, start, 0, DpapiStart);
            dpapiStart = start;
        }
        return Starts(secret, dpapiStart);
    }

    const int DpapiStart = 20;   // a version of four bytes, and the GUID of the provider
    static byte[] dpapiStart;

    static bool Starts(byte[] value, byte[] start)
    {
        if (value == null || value.Length < start.Length) return false;
        for (int i = 0; i < start.Length; i++)
            if (value[i] != start[i]) return false;
        return true;
    }

    // --- How the key is kept ---

    // The key locked for this PC and this Windows user.
    public byte[] ForThisPc()
    {
        return ProtectedData.Protect(key, null, DataProtectionScope.CurrentUser);
    }

    // The key a PC locked, if it is this one; otherwise a CryptographicException.
    public static Vault FromThisPc(byte[] locked)
    {
        return new Vault(ProtectedData.Unprotect(locked, null, DataProtectionScope.CurrentUser));
    }

    // The key locked by a phrase (as Phrase gives it), with a new salt.
    public byte[] ForPhrase(string phrase, out byte[] salt)
    {
        salt = Random(SaltSize);
        return Seal(key, Derive(phrase, salt, Rounds));
    }

    // The key a phrase locked, or null if it is not that phrase.
    public static Vault FromPhrase(string phrase, byte[] salt, int rounds, byte[] locked)
    {
        byte[] found = Open(locked, Derive(phrase, salt, rounds));
        return found == null || found.Length != KeySize + MacSize ? null : new Vault(found);
    }

    // A phrase as it is compared: three words joined by WordSeparator, each with no space around it and one between
    // the words in it, whatever the letter case (wanted by the user: "Cat - RED-lamp" is "cat-red-lamp"); accented
    // letters as one character, however the keyboard typed them. Null if it is not three words.
    public static string Phrase(string written)
    {
        if (written == null) return null;
        string[] words = written.Split(WordSeparator);
        if (words.Length != PhraseWords) return null;
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = string.Join(" ", words[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            if (words[i].Length == 0) return null;
        }
        return string.Join(WordSeparator.ToString(), words).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    static byte[] Derive(string phrase, byte[] salt, int rounds)
    {
        using (Rfc2898DeriveBytes derived = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(phrase), salt, rounds, HashAlgorithmName.SHA512))
            return derived.GetBytes(KeySize + MacSize);
    }

    // --- The format: Mark, the IV, the AES of the value, the HMAC of all that ---

    static byte[] Seal(byte[] plain, byte[] keys)
    {
        byte[] iv = Random(BlockSize), body;
        using (Aes aes = Cipher(keys, iv))
        using (ICryptoTransform encryptor = aes.CreateEncryptor())
            body = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        byte[] sealedValue = new byte[Mark.Length + BlockSize + body.Length + MacSize];
        Buffer.BlockCopy(Mark, 0, sealedValue, 0, Mark.Length);
        Buffer.BlockCopy(iv, 0, sealedValue, Mark.Length, BlockSize);
        Buffer.BlockCopy(body, 0, sealedValue, Mark.Length + BlockSize, body.Length);
        byte[] mac = Mac(keys, sealedValue, sealedValue.Length - MacSize);
        Buffer.BlockCopy(mac, 0, sealedValue, sealedValue.Length - MacSize, MacSize);
        return sealedValue;
    }

    // The value, or null if the keys are not those it was sealed with, or it was changed.
    static byte[] Open(byte[] sealedValue, byte[] keys)
    {
        int body = sealedValue.Length - Mark.Length - BlockSize - MacSize;
        if (!Sealed(sealedValue) || body <= 0 || body % BlockSize != 0) return null;
        byte[] mac = Mac(keys, sealedValue, sealedValue.Length - MacSize);
        int differ = 0;   // compared to the end whatever differs, so that the time says nothing
        for (int i = 0; i < MacSize; i++) differ |= mac[i] ^ sealedValue[sealedValue.Length - MacSize + i];
        if (differ != 0) return null;
        byte[] iv = new byte[BlockSize];
        Buffer.BlockCopy(sealedValue, Mark.Length, iv, 0, BlockSize);
        using (Aes aes = Cipher(keys, iv))
        using (ICryptoTransform decryptor = aes.CreateDecryptor())
            return decryptor.TransformFinalBlock(sealedValue, Mark.Length + BlockSize, body);
    }

    static Aes Cipher(byte[] keys, byte[] iv)
    {
        Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        byte[] part = new byte[KeySize];
        Buffer.BlockCopy(keys, 0, part, 0, KeySize);
        aes.Key = part;
        aes.IV = iv;
        return aes;
    }

    static byte[] Mac(byte[] keys, byte[] data, int length)
    {
        byte[] part = new byte[MacSize];
        Buffer.BlockCopy(keys, KeySize, part, 0, MacSize);
        using (HMACSHA256 hmac = new HMACSHA256(part)) return hmac.ComputeHash(data, 0, length);
    }

    static byte[] Random(int size)
    {
        byte[] bytes = new byte[size];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
        return bytes;
    }
}
