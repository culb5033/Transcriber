using CliFx;
using CliFx.Attributes;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Transcriber
{
    [Command]
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
            Console.Clear();
            recognizer.Recognizing += (o, e) =>
            {                
                Console.SetCursorPosition(0, 0);
                Console.Write(e.Result.Text);
            };

            recognizer.Recognized += OnSpeechRecognized;            

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            Task.WaitAny(new[] { stopRecognition.Task });

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            void OnSpeechRecognized(object sender, SpeechRecognitionEventArgs e)
            {
                var result = e.Result;

                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        console.Output.Write($"We recognized: {result.Text}");
                        break;
                    case ResultReason.NoMatch:
                        console.Output.WriteLine($"NOMATCH: Speech could not be recognized.");
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        console.Output.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            console.Output.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            console.Output.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            console.Output.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                        stopRecognition.TrySetResult(0);
                        break;
                    default:
                        console.Output.WriteLine("ERROR: An unknown problem ocurred.");
                        break;
                }
            }
        }
    }
}