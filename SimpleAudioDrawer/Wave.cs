using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleAudioDrawer
{
    class Wave
    {
        public double[] DataValues { get; private set; } = new double[0];
        /// <summary>
        /// The binary content of the data bytes.
        /// </summary>
        public byte[] DataBytes { get; private set; } = new byte[0];
        /// <summary>
        /// The format id of the wav audio.
        /// </summary>
        public short FormatId { get; private set; } = 0;
        /// <summary>
        /// The format of the wav audio.
        /// </summary>
        public string Format { get; private set; } = string.Empty;
        /// <summary>
        /// The number of channel.
        /// </summary>
        public short NumChannels { get; private set; } = 0;
        /// <summary>
        /// The average number of samples per second.
        /// </summary>
        public int SampleRate { get; private set; } = 0;
        /// <summary>
        /// The average number of bytes per second.
        /// </summary>
        public int ByteRate { get; private set; } = 0;
        /// <summary>
        /// The block align.
        /// </summary>
        public short BlockAlign { get; private set; } = 0;
        /// <summary>
        /// The number of bits per sample.
        /// </summary>
        public short BitsPerSample { get; private set; } = 0;
        /// <summary>
        /// The extra size in fmt_ chunk.
        /// </summary>
        public short CbSize { get; private set; } = 0;
        /// <summary>
        /// The number of samples for each block(for MsAdpcm only).
        /// </summary>
        public short SamplesPerBlock { get; private set; } = 0;
        /// <summary>
        /// The number of the coefficients(for MsAdpcm only).
        /// </summary>
        public short NumCoef { get; private set; } = 0;
        /// <summary>
        /// The list of the coefficients(for MsAdpcm only).
        /// </summary>
        public List<short> CoefList { get; private set; } = new List<short>();
        /// <summary>
        /// The audio length of the wav(in second)
        /// </summary>
        public double AudioLength { get; private set; } = 0;

        /// <summary>
        /// The bits to represent a single sample. For PCM is mostly 16, for alaw/ulaw is 8.
        /// </summary>
        public int AudioBits { get; private set; } = 8;

        /// <summary>
        /// The root mean square of the audio, which relates to the volume of the audio.
        /// </summary>
        public double RMS { get; private set; } = 0;

        /// <summary>
        /// The standard coefficient list(for MsAdpcm only).
        /// </summary>
        private IReadOnlyList<short> CoefStandardList = new List<short>()
        {
            256,0,512,-256,0,0,192,64,240,0,460,-208,392,-232
        };

        private Chunk Chunk_fmt_ = new Chunk();
        private Chunk Chunk_data = new Chunk();
        private List<Chunk> Chunks = new List<Chunk>();
        private bool DeepFlag = true;
        private bool ParseDataFlag = false;
        private byte[] WaveBytes = new byte[0];

        public Wave() { }

        /// <summary>
        /// Parse the whole wav file.
        /// </summary>
        /// <param name="wavePath">The file path of the wav</param>
        public void DeepParse(string wavePath)
        {
            var bytes = File.ReadAllBytes(wavePath);
            DeepParse(bytes);
        }

        /// <summary>
        /// Parse the whole wav file.
        /// </summary>
        /// <param name="bytes">The whole binary of the wav</param>
        public void DeepParse(byte[] bytes)
        {
            DeepFlag = true;
            WaveBytes = bytes;
            ParseRiff();
            PostCheck();
        }

        /// <summary>
        /// Parse part of the wav file, typically the first 100 bytes.
        /// </summary>
        /// <param name="wavePath">The file path of the wav</param>
        public void ShallowParse(string wavePath)
        {
            var bytes = Common.ReadBytes(wavePath, 100);
            ShallowParse(bytes);
        }

        /// <summary>
        /// Parse part of the wav file, typically the first 100 bytes.
        /// </summary>
        /// <param name="bytes">The binary of the wav</param>
        public void ShallowParse(byte[] bytes)
        {
            DeepFlag = false;
            WaveBytes = bytes;
            ParseRiff();
            PostCheck();
        }

        public void ParseData()
        {
            Sanity.Requires(DeepFlag, "Parse data requires deep parse.");
            ParseDataFlag = true;
            AudioBits = BlockAlign / NumChannels * 8;
            DataValues = CalcValues().ToArray();
            RMS = CalculateRms();
        }

        /// <summary>
        /// Parse the RIFF chunk.
        /// </summary>
        private void ParseRiff()
        {
            Sanity.RequiresWave(WaveBytes.Length >= 44, "Wave size is less than min requirement(44 bytes).");

            string riffName = Encoding.ASCII.GetString(WaveBytes, 0, 4);
            Sanity.RequiresWave(riffName == "RIFF", "Riff header broken.");

            int length = BitConverter.ToInt32(WaveBytes, 4);
            Sanity.RequiresWave(!DeepFlag || length + 8 == WaveBytes.Length, "Riff length broken.");

            string waveName = Encoding.ASCII.GetString(WaveBytes, 8, 4);
            Sanity.RequiresWave(waveName == "WAVE", "Wave header broken.");

            ParseChunk(12);
        }

        /// <summary>
        /// Parse the chunk.
        /// </summary>
        /// <param name="offset">The offset of the chunk</param>
        private void ParseChunk(int offset)
        {
            if (offset == WaveBytes.Length)
                return;
            if (offset + 8 > WaveBytes.Length)
            {
                Sanity.RequiresWave(!DeepFlag, "Wave is shorter than expected.");
                return;
            }
            string name = Encoding.ASCII.GetString(WaveBytes, offset, 4);
            int length = BitConverter.ToInt32(WaveBytes, offset + 4);
            if ((length & 1) == 1)
            {
                length++;
            }
            switch (name)
            {
                case "fmt ":
                    Sanity.RequiresWave(string.IsNullOrEmpty(Chunk_fmt_.Name), "fmt_ chunk duplication.");
                    Chunk_fmt_ = new Chunk { Name = name, Length = length, Offset = offset };
                    break;
                case "data":
                    Sanity.RequiresWave(string.IsNullOrEmpty(Chunk_data.Name), "data chunk duplication.");
                    Chunk_data = new Chunk { Name = name, Length = length, Offset = offset };
                    break;
                default:
                    Chunks.Add(new Chunk() { Name = name, Length = length, Offset = offset });
                    break;

            }
            ParseChunk(offset + 8 + length);
        }

        /// <summary>
        /// Check the chunks after all the parsing are done.
        /// </summary>
        private void PostCheck()
        {
            Sanity.RequiresWave(!string.IsNullOrEmpty(Chunk_fmt_.Name), "Missing fmt_ chunk.");
            Sanity.RequiresWave(!string.IsNullOrEmpty(Chunk_data.Name), "Missing data chunk.");
            if (DeepFlag)
            {
                DataBytes = new byte[Chunk_data.Length];

                DataBytes = WaveBytes.ArraySkip(Chunk_data.Offset + 8);
                //Array.Copy(WaveBytes, Chunk_data.Offset + 8, DataBytes, 0, Chunk_data.Length);
            }
            ParseFormat();
            AudioLength = 1.0 * Chunk_data.Length / ByteRate;
        }

        /// <summary>
        /// Parse the format chunk according to its wave format.
        /// </summary>
        private void ParseFormat()
        {
            ParseFormatCore();
            switch (FormatId)
            {
                case 1:
                    break;
                case 3:
                case 6:
                case 7:
                    ParseWaveFormatEx();
                    break;
                case 2:
                    ParseMsAdpcm();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Parse the core part of the format(the first 16 bytes).
        /// </summary>
        private void ParseFormatCore()
        {
            int offset = Chunk_fmt_.Offset;
            FormatId = BitConverter.ToInt16(WaveBytes, offset + 8);
            Format = GetFormat();
            NumChannels = BitConverter.ToInt16(WaveBytes, offset + 10);
            SampleRate = BitConverter.ToInt32(WaveBytes, offset + 12);
            ByteRate = BitConverter.ToInt32(WaveBytes, offset + 16);
            BlockAlign = BitConverter.ToInt16(WaveBytes, offset + 20);
            BitsPerSample = BitConverter.ToInt16(WaveBytes, offset + 22);
        }

        /// <summary>
        /// Parse the extra parts, typically for alaw and ulaw.
        /// </summary>
        private void ParseWaveFormatEx()
        {
            if (Chunk_fmt_.Length >= 18)
                CbSize = BitConverter.ToInt16(WaveBytes, 24);
            Sanity.RequiresWave(ByteRate == NumChannels * SampleRate * BitsPerSample / 8, "Byte rate equation error.");
            Sanity.RequiresWave(BlockAlign == NumChannels * BitsPerSample / 8, "BlockAlign equation error.");
            Sanity.RequiresWave(CbSize == Chunk_fmt_.Length - 16, "CbSize Error.");
        }

        /// <summary>
        /// Parse the MsAdpcm format wav.
        /// </summary>
        private void ParseMsAdpcm()
        {
            int offset = Chunk_fmt_.Offset;
            Sanity.RequiresWave(Chunk_fmt_.Length >= 30, "fmt_ chunk is shorter than expected.");
            switch (SampleRate)
            {
                case 8000:
                case 11025:
                    Sanity.RequiresWave(BlockAlign == 256, "MsAdpcm BlockAlign mismatch.");
                    break;
                case 22050:
                    Sanity.RequiresWave(BlockAlign == 512, "MsAdpcm BlockAlign mismatch.");
                    break;
                case 44100:
                    Sanity.RequiresWave(BlockAlign == 1024, "MsAdpcm BlockAlign mismatch.");
                    break;
                default:
                    break;
            }
            CbSize = BitConverter.ToInt16(WaveBytes, offset + 24);
            SamplesPerBlock = BitConverter.ToInt16(WaveBytes, offset + 26);
            NumCoef = BitConverter.ToInt16(WaveBytes, offset + 28);
            Sanity.RequiresWave(NumCoef * 4 + 4 == CbSize, "CbSize mismatch.");
            Sanity.RequiresWave(BitsPerSample == 4, "bits/sample should be 4 in MsAdpcm.");
            Sanity.RequiresWave(SampleRate * BlockAlign / SamplesPerBlock == ByteRate, "ByteRate equation error.");
            Sanity.RequiresWave((BlockAlign - 7 * NumChannels) * 8 / (BitsPerSample * NumChannels) + 2 == SamplesPerBlock, "SamplesPerBlock equation error.");
            CoefList = Enumerable.Range(0, NumCoef * 2).Select(x => BitConverter.ToInt16(WaveBytes, offset + 30 + x * 2)).ToList();
            Sanity.RequiresWave(CoefStandardList.SequenceEqual(CoefList.Take(2 * NumCoef)), "Coef list mismatch.");
        }

        /// <summary>
        /// Get the format of the audio.
        /// </summary>
        /// <returns></returns>
        private string GetFormat()
        {
            switch (FormatId)
            {
                case 0:
                    return "UNKNOWN";
                case 1:
                    return "PCM";
                case 2:
                    return "MS_ADPCM";
                //case 3:
                //    return "IEEE";
                case 6:
                    return "ALAW";
                case 7:
                    return "ULAW";
                //case -2:
                //    return "Extensible";
                default:
                    throw new MtInfrastructureException("Unsupported audio format.");
            }
        }

        /// <summary>
        /// Calculate the binary of the wave bytes to double.
        /// </summary>
        /// <returns>The collection of the sound values.</returns>
        private IEnumerable<double> CalcValues()
        {
            for(int i = 0; i < DataBytes.Length; i += BlockAlign)
            {
                switch (AudioBits)
                {
                    case 8:
                        yield return (double)DataBytes[i] / 256;
                        break;
                    case 16:
                        yield return (double)BitConverter.ToInt16(DataBytes, i * 2) / 32768;
                        break;
                    case 24:
                        yield return (double)Common.ToInt24(DataBytes, i * 3) / Constants.UINT24_MAX;
                        break;
                    default:
                        throw new MtInfrastructureException("Invalid audio bits, please check manually.");
                }
            }
        }

        /// <summary>
        /// Calculate the root mean square of the audio.
        /// </summary>
        /// <returns>The RMS value</returns>
        private double CalculateRms()
        {
            Sanity.Requires(ParseDataFlag, "The wave data has to be parsed in order to get RMS.");
            return Math.Sqrt(DataValues.Sum(x => x * x) / DataValues.Length);
        }
    }

    /// <summary>
    /// The chunk structure.
    /// </summary>
    class Chunk
    {
        /// <summary>
        /// The offset of current chunk.
        /// </summary>
        public int Offset = 0;
        /// <summary>
        /// The name of current chunk.
        /// </summary>
        public string Name = string.Empty;
        /// <summary>
        /// The length of current chunk.
        /// </summary>
        public int Length = 0;
    }
}
