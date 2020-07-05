using CliFx;
using CliFx.Attributes;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Transcriber
{
    [Command("transcribe")]
    public class TranscribeCommand : ICommand
    {
        [CommandOption("key", 'k', Description = "Azure Cognitive Services API Key.",
            EnvironmentVariableName = "AZURE_COG_SVCS_API_KEY", IsRequired = false)]
        public string ApiKey { get; set; }

        [CommandOption("in", 'i', Description = "Input file (.wav).", IsRequired = true)]
        public FileInfo InputFile { get; set; }

        [CommandOption("out", 'o', Description = "Output file (.txt).", IsRequired = false)]
        public FileInfo OutputFile { get; set; } = new FileInfo(Path.Combine(Environment.CurrentDirectory, "out.txt"));

        [CommandOption("region", 'r', Description = "Azure region.", IsRequired = false)]
        public string Region { get; set; } = "westus2";

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var config = SpeechConfig.FromSubscription(ApiKey, Region);

            using var audioInput = AudioConfig.FromWavFileInput(InputFile.FullName);
            using var recognizer = new SpeechRecognizer(config, audioInput);

            var stopRecognition = new TaskCompletionSource<int>();
            await console.Output.WriteLineAsync($"Recognizing text and writing to file \"{OutputFile.FullName}\".");

            var sw = new Stopwatch();
            using var fileStream = File.OpenWrite(OutputFile.FullName);
            using var fileWriter = new StreamWriter(fileStream);

            recognizer.Recognized += async (o, e) => await OnSpeechRecognized(o, e);

            sw.Start();
            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            Task.WaitAny(new[] { stopRecognition.Task });

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            await console.Output.WriteLineAsync("Done recognizing.");
            await console.Output.WriteLineAsync($"Output saved to \"{OutputFile.FullName}\".");

            async Task OnSpeechRecognized(object sender, SpeechRecognitionEventArgs e)
            {
                var result = e.Result;
                var elapsed = sw.Elapsed;
                var hour = elapsed.Hours;
                var mins = elapsed.Minutes;
                var secs = elapsed.Seconds;

                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        var recOut = $"[{hour}:{mins}:{secs}] {result.Text}";
                        await fileWriter.WriteLineAsync(recOut);
                        await fileWriter.FlushAsync();
                        await console.Output.WriteLineAsync(recOut);
                        break;
                    case ResultReason.NoMatch:
                        var nmOut = $"[{hour}:{mins}:{secs}] Speech could not be recognized.";
                        await fileWriter.WriteLineAsync(nmOut);
                        await fileWriter.FlushAsync();
                        await console.Output.WriteLineAsync(nmOut);
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        var cOut = $"[{hour}:{mins}:{secs}] Canceled for the following reason \"{cancellation.Reason}\".";

                        await fileWriter.WriteLineAsync(cOut);

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            var cReason = $"[{hour}:{mins}:{secs}] Canceled with error code \"{cancellation.ErrorCode}\". " +
                                $"Error details: \"{cancellation.ErrorDetails}\". " +
                                $"Did you update the subscription info?";
                            await fileWriter.WriteLineAsync(cReason);
                            await console.Output.WriteLineAsync(cReason);
                        }
                        await fileWriter.FlushAsync();
                        stopRecognition.TrySetResult(0);
                        break;
                    default:
                        var dOut = $"[{hour}:{mins}:{secs}] ERROR: An unknown problem ocurred.";
                        await fileWriter.WriteLineAsync(dOut);
                        await fileWriter.FlushAsync();
                        await console.Output.WriteLineAsync(dOut);
                        break;
                }
            }
        }
    }
}