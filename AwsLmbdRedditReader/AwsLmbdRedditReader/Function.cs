using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using RedditSharp;
using RedditSharp.Things;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AwsLmbdRedditReader
{
    public class Function
    {
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
           
            SkillResponse response = null;
            
           Reddit r = new Reddit();
            Subreddit sr = r.RSlashAll;
            var getPostsTask = sr.GetPosts(1).First() ;
            getPostsTask.Wait();
            Post p = getPostsTask.Result;
           //subrredittask.Wait();
         

            var requestType = input.GetRequestType();

            if (requestType == typeof(LaunchRequest))
            {
                response = MakeSkillResponse($"The title of the first post is {p.Title}", false);
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                switch (intentRequest.Intent.Name)
                {
                    case "ChooseSubreddit":
                        var subreddit = intentRequest.Intent.Slots["SubReddit"].Value;
                        response = MakeSkillResponse($"{subreddit} is now selected", false);
                        break;
                    case "StepForward":
                        String nextTitle = "Thunderstorm in Ohio";
                        response = MakeSkillResponse($"{nextTitle}", false);
                        //logic
                        break;
                    case "AMAZON.StopIntent":
                        //stopping skill
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.CancelIntent":
                        //stopping skill
                        response = MakeSkillResponse($"", true);
                        break;
                    case "AMAZON.HelpIntent":
                        response = MakeSkillResponse($"Help is here", true);
                        break;
                }                 
            }
            if (response == null)
            {
                String errorreprompt = "Führe eine Aktion aus oder frage nach Hilfe";
                response = MakeSkillResponse($"Anfrage wurde nicht verstanden. {errorreprompt}", false, errorreprompt);
            }
            return response;
        }

        private SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Füge Spieler hinzu, oder starte das Spiel")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            return skillResponse;
        }

    }
}
