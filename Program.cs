//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace HelloWorld
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            await RecognizeSpeechAsync(args[0]);
        }

        static async Task RecognizeSpeechAsync(string inputFile)
        {
            var apiKey = Environment.GetEnvironmentVariable("AZURE_COG_SVCS_API_KEY");
            var config = SpeechConfig.FromSubscription(apiKey, "westus2");

            using var audioInput = AudioConfig.FromWavFileInput(inputFile);
            using var recognizer = new SpeechRecognizer(config, audioInput);
            
                Console.WriteLine("Recognizing first result...");
                var result = await recognizer.RecognizeOnceAsync();

                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        Console.WriteLine($"We recognized: {result.Text}");
                        break;
                    case ResultReason.NoMatch:
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");
                
                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                        break;                
            }
        }
    }
}