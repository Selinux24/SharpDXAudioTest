using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.XAudio2;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpDXAudioTest
{
    public class AudioReader : IDisposable
    {
        private const int WaitPrecision = 1;
        private const int BufferCount = 3;
        private const int DefaultBufferSize = 32 * 1024; // default size 32Kb

        private AudioDecoder audioDecoder;
        private SourceVoice srcVoice;
        private readonly AudioBuffer[] audioBuffers;
        private readonly DataPointer[] memBuffers;
        private readonly AutoResetEvent bufferEndEvent;
        private readonly ManualResetEvent playEvent;
        private TimeSpan playPositionStart;

        public AudioReader(XAudio2 device, string fileName)
        {
            bufferEndEvent = new AutoResetEvent(false);
            playEvent = new ManualResetEvent(false);

            audioDecoder = new AudioDecoder(File.OpenRead(fileName));
            srcVoice = new SourceVoice(device, audioDecoder.WaveFormat);
            srcVoice.BufferEnd += SourceVoice_BufferEnd;
            srcVoice.Start();

            // Pre-allocate buffers
            audioBuffers = new AudioBuffer[BufferCount];
            memBuffers = new DataPointer[BufferCount];

            for (int i = 0; i < audioBuffers.Length; i++)
            {
                audioBuffers[i] = new AudioBuffer();
                memBuffers[i].Size = DefaultBufferSize;
                memBuffers[i].Pointer = Utilities.AllocateMemory(memBuffers[i].Size);
            }
      
            Task.Run(() => PlayAsync());
        }

        public SourceVoice GetVoice()
        {
            return srcVoice;
        }

        int currentSample = 0;

        public void Play()
        {
            Play(TimeSpan.FromSeconds(0));
        }
        public void Play(TimeSpan playPositionStart)
        {
            this.playPositionStart = playPositionStart;
            playEvent.Set();
        }
        private void PlayAsync()
        {
            try
            {
                int nextBuffer = 0;

                var sampleIterator = audioDecoder.GetSamples(playPositionStart).GetEnumerator();

                while (true)
                {
                    while (true)
                    {
                        if (playEvent.WaitOne(WaitPrecision))
                        {
                            Console.WriteLine("playEvent.WaitOne");
                            break;
                        }
                    }

                    while (srcVoice.State.BuffersQueued == audioBuffers.Length)
                    {
                        Console.WriteLine("bufferEndEvent.WaitOne");
                        bufferEndEvent.WaitOne(WaitPrecision);
                    }

                    if (!sampleIterator.MoveNext())
                    {
                        // End of song
                        Console.WriteLine("End of song");
                        break;
                    }

                    Console.WriteLine($"Sample: {currentSample++}");

                    var audioBuffer = PrepareBuffer(sampleIterator.Current, nextBuffer);

                    srcVoice.SubmitSourceBuffer(audioBuffer, null);

                    nextBuffer = ++nextBuffer % audioBuffers.Length;
                }
            }
            finally
            {
                DisposeBuffers();
            }
        }
        private AudioBuffer PrepareBuffer(DataPointer bufferPointer, int nextBuffer)
        {
            // Check that our ring buffer has enough space to store the audio buffer.
            if (bufferPointer.Size > memBuffers[nextBuffer].Size)
            {
                if (memBuffers[nextBuffer].Pointer != IntPtr.Zero)
                {
                    Utilities.FreeMemory(memBuffers[nextBuffer].Pointer);
                }

                memBuffers[nextBuffer].Pointer = Utilities.AllocateMemory(bufferPointer.Size);
                memBuffers[nextBuffer].Size = bufferPointer.Size;
            }

            // Copy the memory from MediaFoundation AudioDecoder to the buffer that is going to be played.
            Utilities.CopyMemory(memBuffers[nextBuffer].Pointer, bufferPointer.Pointer, bufferPointer.Size);

            // Set the pointer to the data.
            audioBuffers[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].Pointer;
            audioBuffers[nextBuffer].AudioBytes = bufferPointer.Size;

            return audioBuffers[nextBuffer];
        }

        void SourceVoice_BufferEnd(IntPtr obj)
        {
            bufferEndEvent.Set();
        }

        public void Dispose()
        {
            DisposeBuffers();
        }
        private void DisposeBuffers()
        {
            audioDecoder?.Dispose();
            audioDecoder = null;

            srcVoice?.Dispose();
            srcVoice = null;

            for (int i = 0; i < BufferCount; i++)
            {
                Utilities.FreeMemory(memBuffers[i].Pointer);
                memBuffers[i].Pointer = IntPtr.Zero;
            }
        }
    }
}
